using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct GridPosition
{
    public int row;
    public int column;

    public GridPosition(int row, int column)
    {
        this.row = row;
        this.column = column;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is GridPosition))
            return false;

        GridPosition other = (GridPosition)obj;
        return row == other.row && column == other.column;
    }

    public override int GetHashCode()
    {
        return row.GetHashCode() ^ (column.GetHashCode() << 2);
    }

    public static bool operator ==(GridPosition a, GridPosition b)
    {
        return a.row == b.row && a.column == b.column;
    }

    public static bool operator !=(GridPosition a, GridPosition b)
    {
        return !(a == b);
    }
}

public class SkiBlockManager : MonoBehaviour
{
    [Header("Block Configuration")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private float blockSize = 200f;
    [SerializeField] private float slopeAngle = 20f;
    [SerializeField] private Vector3 originOffset = Vector3.zero;

    [Header("Spawn Behavior")]
    [Tooltip("Number of blocks to spawn ahead of the player")]
    [SerializeField] private int blocksAhead = 3;
    [Tooltip("Number of blocks to keep behind the player before recycling")]
    [SerializeField] private int blocksLag = 2;
    [Tooltip("Number of blocks to spawn on each side of the player (left and right)")]
    [SerializeField] private int lateralBuffer = 1;
    [Tooltip("Number of rows to spawn behind the player at game start")]
    [SerializeField] private int initialRowsBack = 1;

    [Header("Performance")]
    [SerializeField] private int initialPoolSize = 30;
    [SerializeField] private bool usePooling = true;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SkiGameManager gameManager;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Difficulty Progression")]
    [Tooltip("Number of rows between each difficulty increase")]
    [SerializeField] private int rowsPerDifficultyIncrease = 10;

    [Header("Difficulty Reprise")]
    [Tooltip("Minimum difficulty level before reprise system activates")]
    [SerializeField] private int repriseDifficultyThreshold = 3;
    [Tooltip("Number of normal difficulty rows between each reprise segment")]
    [SerializeField] private int rowsBeforeReprise = 50;
    [Tooltip("Number of easy (difficulty 0) rows in each reprise segment")]
    [SerializeField] private int repriseRowCount = 10;

    [Header("Feature Toggles")]
    [Tooltip("Enable or disable obstacle spawning")]
    [SerializeField] private bool spawnObstacles = true;

    // Data structures
    private Queue<GameObject> blockPool = new Queue<GameObject>();
    private Dictionary<GridPosition, GameObject> activeBlocks = new Dictionary<GridPosition, GameObject>();

    // Player tracking
    private GridPosition playerGridPosition;

    // Grid boundaries
    private int minRow;
    private int maxRow;
    private int minColumn;
    private int maxColumn;

    // Difficulty tracking
    private int currentDifficultyLevel = 0;
    private int rowsSpawnedForDifficulty = 0;
    private int lastDifficultyCountedRow = -1;

    // Reprise tracking
    private int normalRowsSpawned = 0;
    private int lastCountedRow = -1;
    private int repriseStartRow = -1;
    private bool isInRepriseMode = false;

    // Starting row tracking (to keep player's starting row blank)
    private int startingRow;

    void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("SkiBlockManager: playerTransform is not assigned!");
            return;
        }

        if (blockPrefab == null)
        {
            Debug.LogError("SkiBlockManager: blockPrefab is not assigned!");
            return;
        }

        // Pre-instantiate block pool
        if (usePooling)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject block = Instantiate(blockPrefab, transform);
                block.SetActive(false);
                blockPool.Enqueue(block);
            }
        }

        // Calculate initial player grid position
        playerGridPosition = CalculatePlayerGridPosition();

        // Store the starting row so we can keep it blank
        startingRow = playerGridPosition.row;

        // Set initial grid boundaries
        minRow = playerGridPosition.row - initialRowsBack;
        maxRow = playerGridPosition.row + blocksAhead;
        minColumn = playerGridPosition.column - lateralBuffer;
        maxColumn = playerGridPosition.column + lateralBuffer;

        // Spawn initial grid
        for (int row = minRow; row <= maxRow; row++)
        {
            for (int col = minColumn; col <= maxColumn; col++)
            {
                // Only blocks ahead of player should count as forward blocks
                bool isForward = row > playerGridPosition.row;
                SpawnBlock(new GridPosition(row, col), isForward);
            }
        }
    }

    void Update()
    {
        // Only update when in Playing state
        if (gameManager != null && gameManager.CurrentState != GameState.Playing)
            return;

        if (playerTransform == null)
            return;

        // Calculate player's current grid position
        GridPosition newPlayerGridPos = CalculatePlayerGridPosition();

        // Check if player moved to new grid cell
        if (newPlayerGridPos != playerGridPosition)
        {
            playerGridPosition = newPlayerGridPos;

            // Update blocks based on player movement
            UpdateForwardBlocks();
            UpdateLateralBlocks();
        }
    }

    GridPosition CalculatePlayerGridPosition()
    {
        Vector3 localPos = playerTransform.position - originOffset;

        float slopeAngleRad = slopeAngle * Mathf.Deg2Rad;
        float horizontalSpacing = blockSize * Mathf.Cos(slopeAngleRad);

        // Round to nearest grid cell
        int row = Mathf.RoundToInt(localPos.z / horizontalSpacing);
        int column = Mathf.RoundToInt(localPos.x / blockSize);

        return new GridPosition(row, column);
    }

    Vector3 CalculateBlockWorldPosition(GridPosition gridPos)
    {
        float slopeAngleRad = slopeAngle * Mathf.Deg2Rad;

        // Spacing between blocks accounting for slope
        // Reduce spacing slightly to create overlap and prevent seam issues
        float overlapAmount = 1f; // Half a unit overlap to prevent gaps
        float horizontalSpacing = (blockSize * Mathf.Cos(slopeAngleRad)) - overlapAmount;
        float verticalDrop = (blockSize * Mathf.Sin(slopeAngleRad)) - (overlapAmount * Mathf.Tan(slopeAngleRad));

        // Position relative to origin
        float x = gridPos.column * blockSize;
        float z = gridPos.row * horizontalSpacing;
        float y = -gridPos.row * verticalDrop;

        return new Vector3(x, y, z) + originOffset;
    }

    void UpdateForwardBlocks()
    {
        int requiredMaxRow = playerGridPosition.row + blocksAhead;

        // Need to spawn new row ahead?
        while (requiredMaxRow > maxRow)
        {
            maxRow++;
            SpawnRow(maxRow);

            // Recycle old row behind player
            int removeRow = playerGridPosition.row - blocksLag;
            if (minRow < removeRow)
            {
                RecycleRow(minRow);
                minRow++;
            }
        }
    }

    void UpdateLateralBlocks()
    {
        int requiredMinColumn = playerGridPosition.column - lateralBuffer;
        int requiredMaxColumn = playerGridPosition.column + lateralBuffer;

        // Need new column on left?
        while (requiredMinColumn < minColumn)
        {
            minColumn--;
            SpawnColumn(minColumn);

            // Recycle rightmost column if too far
            int removeColumn = playerGridPosition.column + lateralBuffer + 1;
            if (maxColumn > removeColumn)
            {
                RecycleColumn(maxColumn);
                maxColumn--;
            }
        }

        // Need new column on right?
        while (requiredMaxColumn > maxColumn)
        {
            maxColumn++;
            SpawnColumn(maxColumn);

            // Recycle leftmost column if too far
            int removeColumn = playerGridPosition.column - lateralBuffer - 1;
            if (minColumn < removeColumn)
            {
                RecycleColumn(minColumn);
                minColumn++;
            }
        }
    }

    void SpawnRow(int row)
    {
        for (int col = minColumn; col <= maxColumn; col++)
        {
            SpawnBlock(new GridPosition(row, col), isForwardBlock: true);
        }
    }

    void RecycleRow(int row)
    {
        for (int col = minColumn; col <= maxColumn; col++)
        {
            RecycleBlock(new GridPosition(row, col));
        }
    }

    void SpawnColumn(int column)
    {
        for (int row = minRow; row <= maxRow; row++)
        {
            SpawnBlock(new GridPosition(row, column), isForwardBlock: false);
        }
    }

    void RecycleColumn(int column)
    {
        for (int row = minRow; row <= maxRow; row++)
        {
            RecycleBlock(new GridPosition(row, column));
        }
    }

    void SpawnBlock(GridPosition gridPos, bool isForwardBlock = true)
    {
        // Don't spawn if already exists
        if (activeBlocks.ContainsKey(gridPos))
            return;

        GameObject block = GetBlockFromPool();
        Vector3 worldPos = CalculateBlockWorldPosition(gridPos);

        block.transform.position = worldPos;
        block.transform.rotation = Quaternion.Euler(slopeAngle, 0, 0);
        block.SetActive(true);

        activeBlocks[gridPos] = block;

        // Only count forward blocks for difficulty progression and reprise system
        if (isForwardBlock)
        {
            // Track rows for difficulty progression (count each row once)
            if (gridPos.row != lastDifficultyCountedRow)
            {
                rowsSpawnedForDifficulty++;
                lastDifficultyCountedRow = gridPos.row;

                // Check if it's time to increase difficulty
                if (rowsSpawnedForDifficulty >= rowsPerDifficultyIncrease)
                {
                    currentDifficultyLevel++;
                    rowsSpawnedForDifficulty = 0; // Reset counter for next difficulty level
                    Debug.Log($"Difficulty increased to {currentDifficultyLevel} at row {gridPos.row}");
                }
            }

            // Track rows for reprise system (only during normal difficulty)
            if (!isInRepriseMode)
            {
                // Only count each row once (not each block in the row)
                if (gridPos.row != lastCountedRow)
                {
                    normalRowsSpawned++;
                    lastCountedRow = gridPos.row;
                }

                // Check if we should enter reprise mode
                if (currentDifficultyLevel >= repriseDifficultyThreshold &&
                    normalRowsSpawned >= rowsBeforeReprise)
                {
                    isInRepriseMode = true;
                    repriseStartRow = gridPos.row;
                    normalRowsSpawned = 0; // Reset counter for next cycle
                    Debug.Log($"Entering reprise mode after {rowsBeforeReprise} normal rows, starting at row {repriseStartRow}");
                }
            }
        }

        // Determine difficulty for this block
        int blockDifficulty = currentDifficultyLevel;
        if (isInRepriseMode)
        {
            // Calculate how many rows we've progressed since entering reprise
            int rowsIntoReprise = gridPos.row - repriseStartRow;

            // Check if we've completed the reprise segment
            if (rowsIntoReprise >= repriseRowCount)
            {
                isInRepriseMode = false;
                Debug.Log($"Exiting reprise mode after {rowsIntoReprise} rows (from row {repriseStartRow} to {gridPos.row})");
                blockDifficulty = currentDifficultyLevel; // This block uses normal difficulty
            }
            else
            {
                blockDifficulty = 0; // Easy difficulty during reprise
            }
        }

        // Initialize the SkiBlock component with appropriate difficulty
        SkiBlock skiBlock = block.GetComponent<SkiBlock>();
        if (skiBlock != null)
        {
            // Starting row should always be blank (no obstacles)
            bool shouldSpawnObstacles = (gridPos.row == startingRow) ? false : spawnObstacles;

            skiBlock.Initialize(gridPos, blockDifficulty, shouldSpawnObstacles);
        }
    }

    public int GetCurrentDifficulty()
    {
        return currentDifficultyLevel;
    }

    void RecycleBlock(GridPosition gridPos)
    {
        if (activeBlocks.TryGetValue(gridPos, out GameObject block))
        {
            // Reset the SkiBlock component
            SkiBlock skiBlock = block.GetComponent<SkiBlock>();
            if (skiBlock != null)
            {
                skiBlock.Reset();
            }

            block.SetActive(false);

            if (usePooling)
            {
                blockPool.Enqueue(block);
            }

            activeBlocks.Remove(gridPos);
        }
    }

    GameObject GetBlockFromPool()
    {
        if (usePooling && blockPool.Count > 0)
        {
            return blockPool.Dequeue();
        }

        // Create new block if pool is empty
        return Instantiate(blockPrefab, transform);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showDebugGizmos || playerTransform == null)
            return;

        // Draw player grid position
        Gizmos.color = Color.green;
        Vector3 playerGridWorldPos = CalculateBlockWorldPosition(playerGridPosition);
        Gizmos.DrawWireCube(playerGridWorldPos, Vector3.one * blockSize * 0.9f);

        // Draw grid boundaries
        Gizmos.color = Color.yellow;
        for (int row = minRow; row <= maxRow; row++)
        {
            for (int col = minColumn; col <= maxColumn; col++)
            {
                Vector3 pos = CalculateBlockWorldPosition(new GridPosition(row, col));
                Gizmos.DrawWireCube(pos, Vector3.one * blockSize * 0.95f);
            }
        }
    }

    public void ResetBlockManager()
    {
        // Recycle all active blocks
        List<GridPosition> positions = new List<GridPosition>(activeBlocks.Keys);
        foreach (GridPosition pos in positions)
        {
            RecycleBlock(pos);
        }

        // Reset difficulty
        currentDifficultyLevel = 0;
        rowsSpawnedForDifficulty = 0;
        lastDifficultyCountedRow = -1;
        normalRowsSpawned = 0;
        lastCountedRow = -1;
        isInRepriseMode = false;
        repriseStartRow = -1;

        // Respawn initial grid
        if (playerTransform != null)
        {
            // Calculate initial player grid position
            playerGridPosition = CalculatePlayerGridPosition();

            // Store the starting row
            startingRow = playerGridPosition.row;

            // Set initial grid boundaries
            minRow = playerGridPosition.row - initialRowsBack;
            maxRow = playerGridPosition.row + blocksAhead;
            minColumn = playerGridPosition.column - lateralBuffer;
            maxColumn = playerGridPosition.column + lateralBuffer;

            // Spawn initial grid
            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minColumn; col <= maxColumn; col++)
                {
                    bool isForward = row > playerGridPosition.row;
                    SpawnBlock(new GridPosition(row, col), isForward);
                }
            }
        }
    }
}
