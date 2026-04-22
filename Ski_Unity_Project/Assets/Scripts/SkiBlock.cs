using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ObstacleSpawnSettings
{
    [Tooltip("Name for inspector organization")]
    public string name = "Obstacle";

    [Tooltip("Enable/disable this obstacle type")]
    public bool enabled = true;

    [Tooltip("Array of prefabs to randomly choose from")]
    public GameObject[] prefabs;

    [Tooltip("Minimum number to spawn")]
    public int minCount = 1;

    [Tooltip("Maximum number to spawn")]
    public int maxCount = 5;

    [Tooltip("Minimum spacing between obstacles of this type")]
    public float minSpacing = 5f;

    [Tooltip("Minimum scale variation")]
    public float minScale = 0.8f;

    [Tooltip("Maximum scale variation")]
    public float maxScale = 1.3f;

    [Tooltip("Should this obstacle increase with difficulty?")]
    public bool increaseWithDifficulty = true;

    [Tooltip("How many more obstacles to add per difficulty level")]
    public int countIncreasePerLevel = 2;

    [Tooltip("How much to reduce spacing per difficulty level")]
    public float spacingDecreasePerLevel = 0.2f;

    [Tooltip("Absolute minimum spacing (won't go below this)")]
    public float absoluteMinSpacing = 2.5f;

    [Tooltip("Minimum difficulty level required for this obstacle to spawn (0 = no minimum)")]
    public int minDifficulty = 0;

    [Tooltip("Maximum difficulty level for this obstacle to spawn (0 = no maximum)")]
    public int maxDifficulty = 0;

    [Tooltip("Apply random Y-axis rotation (disable for ramps/directional objects)")]
    public bool randomYRotation = true;
}

public class SkiBlock : MonoBehaviour
{
    [Header("Obstacle Spawning")]
    [SerializeField] private ObstacleSpawnSettings[] obstacleTypes;
    [SerializeField] private float spawnBoundaryInset = 10f;

    private GridPosition gridPosition;
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private int currentDifficultyLevel = 0;
    private bool shouldSpawnObstacles = true;

    public void Initialize(GridPosition pos, int difficultyLevel = 0, bool spawnObstacles = true)
    {
        gridPosition = pos;
        currentDifficultyLevel = difficultyLevel;
        shouldSpawnObstacles = spawnObstacles;

        if (shouldSpawnObstacles)
        {
            SpawnObstacles();
        }
    }

    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public void Reset()
    {
        // Clear all spawned objects when block is recycled
        ClearObstacles();
    }

    void SpawnObstacles()
    {
        // Don't spawn if no obstacle types configured
        if (obstacleTypes == null || obstacleTypes.Length == 0)
            return;

        // Get block dimensions
        float blockSize = 200f;
        float halfBlock = blockSize / 2f;

        // Spawn each type of obstacle
        foreach (ObstacleSpawnSettings obstacleType in obstacleTypes)
        {
            // Skip if disabled
            if (!obstacleType.enabled)
                continue;

            // Skip if no prefabs assigned
            if (obstacleType.prefabs == null || obstacleType.prefabs.Length == 0)
                continue;

            // Skip if current difficulty is below minimum required
            if (currentDifficultyLevel < obstacleType.minDifficulty)
                continue;

            // Skip if current difficulty is above maximum (when maxDifficulty > 0)
            if (obstacleType.maxDifficulty > 0 && currentDifficultyLevel > obstacleType.maxDifficulty)
                continue;

            // Calculate count based on difficulty
            int minCount = obstacleType.minCount;
            int maxCount = obstacleType.maxCount;

            if (obstacleType.increaseWithDifficulty)
            {
                // Calculate difficulty bonus relative to when this obstacle becomes available
                int effectiveDifficulty = Mathf.Max(0, currentDifficultyLevel - obstacleType.minDifficulty);
                int difficultyBonus = effectiveDifficulty * obstacleType.countIncreasePerLevel;
                minCount += difficultyBonus;
                maxCount += difficultyBonus;
            }

            // Determine number to spawn
            int spawnCount = Random.Range(minCount, maxCount + 1);

            // Calculate adjusted spacing
            float adjustedSpacing = obstacleType.minSpacing;
            if (obstacleType.increaseWithDifficulty)
            {
                // Calculate spacing reduction relative to when this obstacle becomes available
                int effectiveDifficulty = Mathf.Max(0, currentDifficultyLevel - obstacleType.minDifficulty);
                adjustedSpacing = Mathf.Max(
                    obstacleType.minSpacing - (effectiveDifficulty * obstacleType.spacingDecreasePerLevel),
                    obstacleType.absoluteMinSpacing
                );
            }

            // Spawn obstacles
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 localPosition = Vector3.zero;
                bool validPosition = false;
                int attempts = 0;
                int maxAttempts = 30;

                // Try to find a valid position
                while (!validPosition && attempts < maxAttempts)
                {
                    attempts++;

                    // Random position within block bounds
                    float x = Random.Range(-halfBlock + spawnBoundaryInset, halfBlock - spawnBoundaryInset);
                    float z = Random.Range(-halfBlock + spawnBoundaryInset, halfBlock - spawnBoundaryInset);

                    localPosition = new Vector3(x, 0, z);

                    // Check minimum spacing from all other obstacles
                    validPosition = true;
                    foreach (GameObject existingObstacle in spawnedObstacles)
                    {
                        if (existingObstacle != null)
                        {
                            float distance = Vector3.Distance(localPosition, existingObstacle.transform.localPosition);
                            if (distance < adjustedSpacing)
                            {
                                validPosition = false;
                                break;
                            }
                        }
                    }
                }

                // Spawn obstacle if valid position found
                if (validPosition)
                {
                    // Randomly select a prefab from this obstacle type
                    GameObject prefab = obstacleType.prefabs[Random.Range(0, obstacleType.prefabs.Length)];

                    // Instantiate as child of this block
                    GameObject obstacle = Instantiate(prefab, transform);
                    obstacle.transform.localPosition = localPosition;

                    // Set world rotation to stand upright with random Y rotation for variety (if enabled)
                    float yRotation = obstacleType.randomYRotation ? Random.Range(0f, 360f) : 0f;
                    obstacle.transform.rotation = Quaternion.Euler(0, yRotation, 0);

                    // Random scale variation
                    float scaleVariation = Random.Range(obstacleType.minScale, obstacleType.maxScale);
                    obstacle.transform.localScale = Vector3.one * scaleVariation;
                    spawnedObstacles.Add(obstacle);
                }
            }
        }
    }

    void ClearObstacles()
    {
        // Destroy all spawned obstacles
        foreach (GameObject obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        spawnedObstacles.Clear();
    }
}
