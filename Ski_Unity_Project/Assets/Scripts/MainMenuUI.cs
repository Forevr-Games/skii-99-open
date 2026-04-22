using UnityEngine;
using UnityEngine.UIElements;
using ForevrTools.Audio;
using Devvit;
using System;

public class MainMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkiGameManager gameManager;
    [SerializeField] private SkiHUD hud;
    [SerializeField] private LeaderboardUI leaderboardUI;
    [SerializeField] private ProceduralAudioManager audioManager;
    [SerializeField] private SaveDataManager saveDataManager;

    // UI Elements
    private UIDocument uiDocument;
    private VisualElement menuContainer;
    private Button playButton;
    private Button zenModeButton;

    private VisualElement challengeContainer;
    private Label challengeLabel;

    private void Start()
    {
        // Get UI Document component
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("MainMenuUI: UIDocument component is missing!");
            return;
        }

        // Query UI elements
        var root = uiDocument.rootVisualElement;
        menuContainer = root.Q<VisualElement>("main-menu-container");
        playButton = root.Q<Button>("play-button");
        zenModeButton = root.Q<Button>("zen-mode-button");
        challengeContainer = root.Q<VisualElement>("challenge-container");
        challengeLabel = root.Q<Label>("challenge-label");

        if (challengeContainer != null)
        {
            challengeContainer.style.display = DisplayStyle.None;
        }
        else
        {
            Debug.LogError("MainMenuUI: Could not find challenge-container!");
        }

        if (menuContainer == null)
        {
            Debug.LogError("MainMenuUI: Could not find main-menu-container!");
        }

        if (playButton == null)
        {
            Debug.LogError("MainMenuUI: Could not find play-button!");
        }
        if (zenModeButton == null)
        {
            Debug.LogError("MainMenuUI: Could not find zen-mode-button!");
        }

        // Register button callbacks
        if (playButton != null)
        {
            playButton.clicked += OnPlayButtonClicked;
        }

        if (zenModeButton != null)
        {
            zenModeButton.clicked += OnZenModeButtonClicked;
        }

        // Subscribe to state changes
        if (gameManager != null)
        {
            gameManager.OnStateChanged += HandleStateChange;
        }

        // Show menu by default (game starts in MainMenu state)
        ShowMenu();

        // Hide HUD when menu is shown
        if (hud != null)
        {
            hud.HideHUD();
        }

        if (saveDataManager != null)
        {
            saveDataManager.OnDevvitInitialized += HandleDevvitInitDataReceived;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playButton != null)
        {
            playButton.clicked -= OnPlayButtonClicked;
        }

        if (zenModeButton != null)
        {
            zenModeButton.clicked -= OnZenModeButtonClicked;
        }

        if (gameManager != null)
        {
            gameManager.OnStateChanged -= HandleStateChange;
        }

        if (saveDataManager != null)
        {
            saveDataManager.OnDevvitInitialized -= HandleDevvitInitDataReceived;
        }
    }

    private void HandleDevvitInitDataReceived()
    {
        bool isChallengePost = saveDataManager.IsChallengePost;
        ChallengePostData challengeData = saveDataManager.FetchedPostData;
        if (challengeContainer != null && challengeLabel != null)
        {
            if (isChallengePost && challengeData != null)
            {
                challengeContainer.style.display = DisplayStyle.Flex;
                challengeLabel.text = $"Challenge by {challengeData.author}\nScore: {challengeData.score} - Distance: {challengeData.distance}";
            }
            else
            {
                challengeContainer.style.display = DisplayStyle.None;
            }
        }
    }

    private void HandleStateChange(GameState oldState, GameState newState)
    {
        if (newState == GameState.MainMenu)
        {
            ShowMenu();
            if (hud != null)
            {
                hud.HideHUD();
            }
        }
        else if (newState == GameState.Playing)
        {
            HideMenu();
            if (hud != null)
            {
                hud.ShowHUD();
            }
        }
    }

    private void OnPlayButtonClicked()
    {
        // Play UI click sound
        audioManager?.PlayPreset("UIClick");

        if (gameManager != null)
        {
            gameManager.StartGame(false); // Start normal mode
        }
    }

    private void OnZenModeButtonClicked()
    {
        // Play UI click sound
        audioManager?.PlayPreset("UIClick");

        if (gameManager != null)
        {
            gameManager.StartGame(true); // Start zen mode
        }
    }

    public void ShowMenu()
    {
        if (menuContainer != null)
        {
            menuContainer.style.display = DisplayStyle.Flex;
        }

        // Show and refresh leaderboard
        if (leaderboardUI != null)
        {
            leaderboardUI.Show();
            leaderboardUI.RefreshLeaderboard();
        }
    }

    public void HideMenu()
    {
        if (menuContainer != null)
        {
            menuContainer.style.display = DisplayStyle.None;
        }

        // Hide leaderboard
        if (leaderboardUI != null)
        {
            leaderboardUI.Hide();
        }
    }
}
