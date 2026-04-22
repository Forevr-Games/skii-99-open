using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public enum GameState
{
    MainMenu,
    Playing
}

public class SkiGameManager : MonoBehaviour
{
    // Game State Management
    private GameState currentState = GameState.MainMenu;
    public event Action<GameState, GameState> OnStateChanged;
    public GameState CurrentState => currentState;

    [Header("References")]
    [SerializeField] private SkiPlayerController player;
    [SerializeField] private SkiBlockManager blockManager;
    [SerializeField] private SkiGameOverScreen gameOverScreen;
    [SerializeField] private SkiTrickSystem trickSystem;
    [SerializeField] private SkiCameraController cameraController;

    [Header("Game Over Settings")]
    [SerializeField] private float gameOverDelay = 3f;

    [Header("Difficulty Progression")]
    [SerializeField] private bool zenMode = false;
    [SerializeField] private float speedIncreasePerSecond = 0.1f;
    [SerializeField] private float maxSpeed = 40f;

    [Header("Score Settings")]
    [SerializeField] private float distanceScoreMultiplier = 10f; // Score per meter traveled

    private bool isRestarting = false;
    private float gameTime = 0f;
    private float initialSpeed;

    // Score tracking
    private float currentScore = 0f;
    private Vector3 lastPlayerPosition;
    private float totalDistanceTraveled = 0f;
    private float trickScore = 0f;

    private void Start()
    {
        if (player != null)
        {
            initialSpeed = player.GetForwardSpeed();
            lastPlayerPosition = player.transform.position;
        }

        // Initialize to MainMenu state
        TransitionToState(GameState.MainMenu);
    }

    private void Update()
    {
        // Only update game logic when in Playing state
        if (currentState != GameState.Playing)
            return;

        // Check if player has crashed and we haven't started restarting yet
        if (!isRestarting && HasPlayerCrashed())
        {
            StartCoroutine(RestartGame());
            return;
        }

        // Only update difficulty if not in zen mode and not crashed
        if (!zenMode && !HasPlayerCrashed())
        {
            UpdateDifficulty();
            UpdateScore();
        }
    }

    private void UpdateDifficulty()
    {
        gameTime += Time.deltaTime;

        // Gradually increase player speed
        if (player != null)
        {
            float currentSpeed = player.GetForwardSpeed();
            if (currentSpeed < maxSpeed)
            {
                float newSpeed = Mathf.Min(initialSpeed + (gameTime * speedIncreasePerSecond), maxSpeed);
                player.SetForwardSpeed(newSpeed);
            }
        }

        // Difficulty is now managed by SkiBlockManager based on rows
    }

    private void UpdateScore()
    {
        if (player == null)
            return;

        // Calculate distance traveled since last frame (only on XZ plane for horizontal distance)
        Vector3 currentPosition = player.transform.position;
        Vector3 lastPos = new Vector3(lastPlayerPosition.x, 0, lastPlayerPosition.z);
        Vector3 currentPos = new Vector3(currentPosition.x, 0, currentPosition.z);

        float distanceThisFrame = Vector3.Distance(lastPos, currentPos);
        totalDistanceTraveled += distanceThisFrame;

        // Calculate score based on distance and tricks
        currentScore = (totalDistanceTraveled * distanceScoreMultiplier) + trickScore;

        lastPlayerPosition = currentPosition;
    }

    private bool HasPlayerCrashed()
    {
        if (player == null)
            return false;

        return player.HasCrashed();
    }

    private IEnumerator RestartGame()
    {
        isRestarting = true;

        // Wait for delay to let player see the crash
        yield return new WaitForSeconds(gameOverDelay);

        // Show game over screen with final stats
        if (gameOverScreen != null)
        {
            gameOverScreen.ShowScreen(currentScore, totalDistanceTraveled, trickScore);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void SetZenMode(bool enabled)
    {
        zenMode = enabled;
    }

    public bool IsZenMode()
    {
        return zenMode;
    }

    public float GetGameTime()
    {
        return gameTime;
    }

    public float GetCurrentScore()
    {
        return currentScore;
    }

    public float GetTotalDistance()
    {
        return totalDistanceTraveled;
    }

    public void AddTrickScore(int score)
    {
        trickScore += score;
        currentScore = (totalDistanceTraveled * distanceScoreMultiplier) + trickScore;
    }

    public float GetTrickScore()
    {
        return trickScore;
    }

    // State Management Methods
    public void TransitionToState(GameState newState, bool force = false)
    {
        if (currentState == newState && !force)
            return;

        GameState oldState = currentState;
        currentState = newState;

        // Notify listeners
        OnStateChanged?.Invoke(oldState, newState);
    }

    public void StartGame(bool zenModeEnabled)
    {
        ResetGameState();
        SetZenMode(zenModeEnabled);
        TransitionToState(GameState.Playing);
    }

    public void ResetGameState()
    {
        // Reset game variables
        isRestarting = false;
        gameTime = 0f;
        currentScore = 0f;
        totalDistanceTraveled = 0f;
        trickScore = 0f;

        // Reset player
        if (player != null)
        {
            player.ResetPlayer();
            player.SetForwardSpeed(initialSpeed);
            lastPlayerPosition = player.transform.position;
        }

        // Reset block manager
        if (blockManager != null)
        {
            blockManager.ResetBlockManager();
        }

        // Reset trick system
        if (trickSystem != null)
        {
            trickSystem.ResetTricks();
        }

        // Reset camera (clear faded renderers)
        if (cameraController != null)
        {
            cameraController.ClearFadedRenderers();
        }
    }
}
