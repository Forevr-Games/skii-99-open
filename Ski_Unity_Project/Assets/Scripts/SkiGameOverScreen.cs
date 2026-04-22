using UnityEngine;
using UnityEngine.UIElements;
using ForevrTools.Audio;
using System.Collections;

public class SkiGameOverScreen : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("References")]
    [SerializeField] private SkiHUD hud;
    [SerializeField] private SaveDataManager saveDataManager;
    [SerializeField] private SkiGameManager gameManager;
    [SerializeField] private ProceduralAudioManager audioManager;

    private VisualElement gameOverContainer;
    private Label scoreValue;
    private Label distanceValue;
    private Label tricksValue;
    private Label highScoreValue;
    private Label highDistanceValue;
    private Label newRecordLabel;
    private Button retryButton;
    private Button mainMenuButton;
    private Button shareButton;
    private VisualElement shareNotificationSuccess;
    private VisualElement shareNotificationFail;
    private VisualElement shareConfirmPopup;
    private Label shareConfirmMessage;
    private Button shareConfirmYes;
    private Button shareConfirmNo;

    private float finalScore;
    private float distance;

    private bool isInitialized = false;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        InitializeUI();
    }

    private void InitializeUI()
    {
        if (uiDocument == null)
        {
            Debug.LogError("SkiGameOverScreen: UIDocument is missing!");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // Get references to UI elements
        gameOverContainer = root.Q<VisualElement>("game-over-container");
        scoreValue = root.Q<Label>("score-value");
        distanceValue = root.Q<Label>("distance-value");
        tricksValue = root.Q<Label>("tricks-value");
        highScoreValue = root.Q<Label>("high-score-value");
        highDistanceValue = root.Q<Label>("high-distance-value");
        newRecordLabel = root.Q<Label>("new-record-label");
        retryButton = root.Q<Button>("retry-button");
        mainMenuButton = root.Q<Button>("main-menu-button");
        shareButton = root.Q<Button>("share-button");
        shareNotificationSuccess = root.Q<VisualElement>("share-notification-success");
        shareNotificationFail = root.Q<VisualElement>("share-notification-fail");

        if (gameOverContainer == null || scoreValue == null || distanceValue == null ||
            tricksValue == null || retryButton == null)
        {
            Debug.LogError("SkiGameOverScreen: Failed to find required UI elements!");
            return;
        }

        // Register button callback
        retryButton.clicked += OnRetryButtonClicked;
        mainMenuButton.clicked += OnMainMenuButtonClicked;
        if (shareButton != null)
        {
            shareButton.clicked += OnShareButtonClicked;
        }

        shareConfirmPopup   = root.Q<VisualElement>("share-confirm-popup");
        shareConfirmMessage = root.Q<Label>("share-confirm-message");
        shareConfirmYes     = root.Q<Button>("share-confirm-yes");
        shareConfirmNo      = root.Q<Button>("share-confirm-no");

        if (shareConfirmYes != null) shareConfirmYes.clicked += OnShareConfirmYes;
        if (shareConfirmNo  != null) shareConfirmNo.clicked  += OnShareConfirmNo;

        // Hide the screen initially - this prevents it from showing during gameplay
        HideScreen();

        isInitialized = true;
    }

    public void ShowScreen(float finalScore, float distance, float trickScore)
    {
        this.finalScore = finalScore;
        this.distance = distance;
        if (!isInitialized)
        {
            return;
        }

        // Hide the HUD
        if (hud != null)
        {
            hud.HideHUD();
        }

        // Check if we're in zen mode
        bool isZenMode = gameManager != null && gameManager.IsZenMode();

        // Hide new record label initially
        if (newRecordLabel != null)
        {
            newRecordLabel.style.display = DisplayStyle.None;
        }

        if (shareButton != null)
        {
            shareButton.style.display = DisplayStyle.None;
            shareButton.SetEnabled(false);
        }

        // In zen mode, show "--" for all stats
        if (isZenMode)
        {
            // Show "--" for current run stats
            scoreValue.text = "--";
            distanceValue.text = "--";
            tricksValue.text = "--";

            // Show "---" for high score displays
            if (highScoreValue != null)
            {
                highScoreValue.text = "---";
            }

            if (highDistanceValue != null)
            {
                highDistanceValue.text = "---";
            }
        }
        else
        {
            // Update current run stats (normal mode)
            scoreValue.text = Mathf.RoundToInt(finalScore).ToString();
            distanceValue.text = Mathf.RoundToInt(distance).ToString() + "m";
            tricksValue.text = Mathf.RoundToInt(trickScore).ToString();
        }

        // Only display and save high scores in normal mode
        if (!isZenMode && saveDataManager != null)
        {
            float highScore = saveDataManager.GetHighScore();
            float highDistance = saveDataManager.GetFurthestDistance();

            if (highScoreValue != null)
            {
                highScoreValue.text = highScore > 0
                    ? Mathf.RoundToInt(highScore).ToString()
                    : "---";
            }

            if (highDistanceValue != null)
            {
                highDistanceValue.text = highDistance > 0
                    ? Mathf.RoundToInt(highDistance).ToString() + "m"
                    : "---";
            }

            // Save the score (SaveDataManager will handle new record detection)
            saveDataManager.SaveScore(finalScore, distance, (isNewHighScore, isNewDistance) =>
            {
                // Show "NEW RECORD!" if any record was broken
                if (isNewHighScore || isNewDistance)
                {
                    if (newRecordLabel != null)
                    {
                        newRecordLabel.style.display = DisplayStyle.Flex;
                        StartCoroutine(AnimateRainbowText(newRecordLabel));
                    }
                    if (shareButton != null)
                    {
                        shareButton.style.display = DisplayStyle.Flex;
                        shareButton.SetEnabled(true);
                    }
                }

                // Update displayed high scores if new records and animate them
                if (isNewHighScore && highScoreValue != null)
                {
                    highScoreValue.text = Mathf.RoundToInt(finalScore).ToString();
                    StartCoroutine(AnimateRainbowText(highScoreValue));
                }
                if (isNewDistance && highDistanceValue != null)
                {
                    highDistanceValue.text = Mathf.RoundToInt(distance).ToString() + "m";
                    StartCoroutine(AnimateRainbowText(highDistanceValue));
                }
            });
        }

        // Show the container
        gameOverContainer.AddToClassList("game-over-container-visible");
    }

    public void HideScreen()
    {
        if (gameOverContainer != null)
        {
            gameOverContainer.RemoveFromClassList("game-over-container-visible");
        }
    }

    private void OnRetryButtonClicked()
    {
        // Play UI click sound
        audioManager?.PlayPreset("UIClick");

        // Hide this screen
        HideScreen();

        // Transition back to MainMenu state
        // The game will be reset when the player clicks play
        if (gameManager != null)
        {
            gameManager.TransitionToState(GameState.Playing, true); // Force transition to Playing state to reset game immediately
            gameManager.StartGame(gameManager.IsZenMode()); // Restart with the same mode
        }
    }

    private void OnMainMenuButtonClicked()
    {
        // Play UI click sound
        audioManager?.PlayPreset("UIClick");

        // Hide this screen
        HideScreen();

        // Transition back to MainMenu state
        if (gameManager != null)
        {
            gameManager.TransitionToState(GameState.MainMenu);
        }
    }

    private void OnShareButtonClicked()
    {
        audioManager?.PlayPreset("UIClick");

        if (shareConfirmPopup == null) { ExecuteShare(); return; }

        bool isChallenge = saveDataManager != null && saveDataManager.IsChallengePost;
        if (shareConfirmMessage != null)
            shareConfirmMessage.text = isChallenge
                ? "Comment your score on post?"
                : "Create a post challenging other users to ski?";

        shareConfirmPopup.style.display = DisplayStyle.Flex;
    }

    private void OnShareConfirmYes()
    {
        audioManager?.PlayPreset("UIClick");
        shareConfirmPopup.style.display = DisplayStyle.None;
        ExecuteShare();
    }

    private void OnShareConfirmNo()
    {
        audioManager?.PlayPreset("UIClick");
        shareConfirmPopup.style.display = DisplayStyle.None;
    }

    private void ExecuteShare()
    {
        shareButton.SetEnabled(false);
        if (saveDataManager != null)
        {
            saveDataManager.ShareScore(finalScore, distance, (success, _) =>
            {
                StartCoroutine(ShowSharedNotification(success));
            });
        }
    }

    private IEnumerator ShowSharedNotification(bool success)
    {
        VisualElement notification = success ? shareNotificationSuccess : shareNotificationFail;
        if (notification != null)
        {
            notification.style.top = new StyleLength(Length.Percent(0));
            yield return new WaitForSeconds(4f);
            notification.style.top = new StyleLength(Length.Percent(-110));

            if (!success)
            {
                shareButton.SetEnabled(true);
            }
        }
    }

    private void OnDestroy()
    {
        if (retryButton != null) retryButton.clicked -= OnRetryButtonClicked;
        if (shareConfirmYes != null) shareConfirmYes.clicked -= OnShareConfirmYes;
        if (shareConfirmNo  != null) shareConfirmNo.clicked  -= OnShareConfirmNo;
    }

    private System.Collections.IEnumerator AnimateRainbowText(Label label)
    {
        // Rainbow colors to cycle through
        Color[] rainbowColors = new Color[]
        {
            new Color(1f, 0f, 0f),      // Red
            new Color(1f, 0.65f, 0f),   // Orange
            new Color(1f, 1f, 0f),      // Yellow
            new Color(0f, 1f, 0f),      // Green
            new Color(0f, 0.5f, 1f),    // Blue
            new Color(0.55f, 0f, 1f)    // Purple
        };

        int colorIndex = 0;
        float duration = 0.3f; // Time for each color transition

        // Loop the animation continuously
        while (label != null && label.style.display == DisplayStyle.Flex)
        {
            // Get current and next color
            Color startColor = rainbowColors[colorIndex];
            Color endColor = rainbowColors[(colorIndex + 1) % rainbowColors.Length];

            // Animate from current color to next color
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color currentColor = Color.Lerp(startColor, endColor, t);
                label.style.color = new StyleColor(currentColor);
                yield return null;
            }

            // Move to next color
            colorIndex = (colorIndex + 1) % rainbowColors.Length;
        }
    }
}
