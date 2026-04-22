using UnityEngine;
using UnityEngine.UIElements;

public class SkiHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkiGameManager gameManager;
    [SerializeField] private SkiPlayerController playerController;
    [SerializeField] private UIDocument uiDocument;

    [Header("Display Settings")]
    [SerializeField] private string scoreFormat = "{0}";
    [SerializeField] private string speedFormat = "{0} m/s";

    private Label scoreLabel;
    private Label speedLabel;
    private VisualElement hudContainer;
    private Label comboText;
    private Label comboMultiplier;

    private void Start()
    {
        // Subscribe to state changes
        if (gameManager != null)
        {
            gameManager.OnStateChanged += HandleStateChange;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from state changes
        if (gameManager != null)
        {
            gameManager.OnStateChanged -= HandleStateChange;
        }
    }

    private void HandleStateChange(GameState oldState, GameState newState)
    {
        if (newState == GameState.Playing)
        {
            ShowHUD();
        }
        else if (newState == GameState.MainMenu)
        {
            HideHUD();
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            hudContainer = uiDocument.rootVisualElement.Q<VisualElement>("hud-container");
            scoreLabel = uiDocument.rootVisualElement.Q<Label>("score-label");
            speedLabel = uiDocument.rootVisualElement.Q<Label>("speed-label");
            comboText = uiDocument.rootVisualElement.Q<Label>("combo-text");
            comboMultiplier = uiDocument.rootVisualElement.Q<Label>("combo-multiplier");
        }
    }

    private void Update()
    {
        if (gameManager == null)
            return;

        // Hide entire HUD if in zen mode
        if (gameManager.IsZenMode())
        {
            if (hudContainer != null && hudContainer.style.display == DisplayStyle.Flex)
            {
                HideHUD();
            }
            return;
        }

        // Show HUD if not in zen mode and currently playing (but not crashed)
        if (gameManager.CurrentState == GameState.Playing && hudContainer != null && hudContainer.style.display != DisplayStyle.Flex)
        {
            // Only show if player hasn't crashed
            if (playerController != null && !playerController.HasCrashed())
            {
                ShowHUD();
            }
        }

        // Update score display
        if (scoreLabel != null)
        {
            float score = gameManager.GetCurrentScore();
            scoreLabel.text = Mathf.FloorToInt(score).ToString();
        }

        // Update speed display
        if (playerController != null && speedLabel != null)
        {
            float speed = playerController.GetForwardSpeed();
            speedLabel.text = Mathf.FloorToInt(speed).ToString() + " m/s";
        }
    }

    public void ShowHUD()
    {
        if (hudContainer != null)
        {
            hudContainer.style.display = DisplayStyle.Flex;
        }

        if (comboText != null)
        {
            comboText.style.display = DisplayStyle.Flex;
        }

        if (comboMultiplier != null)
        {
            comboMultiplier.style.display = DisplayStyle.Flex;
        }
    }

    public void HideHUD()
    {
        if (hudContainer != null)
        {
            hudContainer.style.display = DisplayStyle.None;
        }

        if (comboText != null)
        {
            comboText.style.display = DisplayStyle.None;
        }

        if (comboMultiplier != null)
        {
            comboMultiplier.style.display = DisplayStyle.None;
        }
    }
}
