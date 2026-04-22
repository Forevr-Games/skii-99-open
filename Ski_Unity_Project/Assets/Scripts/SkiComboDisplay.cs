using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class SkiComboDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private SkiTrickSystem trickSystem;
    [SerializeField] private SkiGameManager gameManager;

    private Label comboText;
    private Label comboMultiplier;
    private VisualElement comboContainer;
    private VisualElement comboTimerContainer;
    private VisualElement comboTimerBar;

    private Coroutine fadeCoroutine;
    private float lastComboUpdateTime;
    private bool isComboActive;

    private void OnEnable()
    {
        // Get UI elements
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            comboText = root.Q<Label>("combo-text");
            comboMultiplier = root.Q<Label>("combo-multiplier");
            comboContainer = root.Q<VisualElement>("combo-container");
            comboTimerContainer = root.Q<VisualElement>("combo-timer-container");
            comboTimerBar = root.Q<VisualElement>("combo-timer-bar");
        }

        // Subscribe to trick system events
        if (trickSystem != null)
        {
            trickSystem.OnTrickPerformed += HandleTrickPerformed;
            trickSystem.OnComboUpdated += HandleComboUpdated;
            trickSystem.OnComboBanked += HandleComboBanked;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (trickSystem != null)
        {
            trickSystem.OnTrickPerformed -= HandleTrickPerformed;
            trickSystem.OnComboUpdated -= HandleComboUpdated;
            trickSystem.OnComboBanked -= HandleComboBanked;
        }
    }

    private void Update()
    {
        // Hide all combo elements in zen mode
        if (gameManager != null && gameManager.IsZenMode())
        {
            HideComboDisplay();
            return;
        }

        if (!isComboActive || comboTimerBar == null || trickSystem == null)
            return;

        // Calculate time remaining
        float elapsedTime = Time.time - lastComboUpdateTime;
        float comboTimeWindow = 3f; // Default value, matches TrickScoreSettings
        float timeRemaining = Mathf.Max(0, comboTimeWindow - elapsedTime);
        float normalizedTime = timeRemaining / comboTimeWindow;

        // Scale the bar from both sides by using scaleX
        comboTimerBar.style.scale = new StyleScale(new Scale(new Vector3(normalizedTime, 1, 1)));
    }

    private void HideComboDisplay()
    {
        if (comboText != null)
        {
            comboText.style.display = DisplayStyle.None;
        }

        if (comboMultiplier != null)
        {
            comboMultiplier.style.display = DisplayStyle.None;
        }

        if (comboContainer != null)
        {
            comboContainer.style.display = DisplayStyle.None;
        }

        if (comboTimerContainer != null)
        {
            comboTimerContainer.style.display = DisplayStyle.None;
        }
    }

    private void HandleTrickPerformed(TrickInstance trick)
    {
        // Individual trick flash (optional - combo update will show full string)
        // This could be used for quick feedback
    }

    private void HandleComboUpdated(ComboState combo)
    {
        // Don't show combo in zen mode
        if (gameManager != null && gameManager.IsZenMode())
            return;

        if (comboText == null || comboMultiplier == null)
            return;

        // Ensure elements are visible (in case they were hidden)
        comboText.style.display = DisplayStyle.Flex;
        comboMultiplier.style.display = DisplayStyle.Flex;
        if (comboContainer != null)
            comboContainer.style.display = DisplayStyle.Flex;
        if (comboTimerContainer != null)
            comboTimerContainer.style.display = DisplayStyle.Flex;

        // Line 1: Trick string with counts
        comboText.text = combo.GetComboString();
        comboText.AddToClassList("combo-text-active");

        // Line 2: Base score x chain count (no multiplier shown if only 1 trick)
        int baseScore = combo.GetBaseScore();
        int chainCount = combo.GetComboChainCount();

        if (chainCount > 1)
            comboMultiplier.text = $"{baseScore} x {chainCount}";
        else
            comboMultiplier.text = baseScore.ToString();

        comboMultiplier.AddToClassList("combo-multiplier-active");

        // Show and reset combo timer bar
        if (comboTimerContainer != null)
        {
            comboTimerContainer.AddToClassList("combo-timer-container-active");
        }

        if (comboTimerBar != null)
        {
            comboTimerBar.style.scale = new StyleScale(new Scale(Vector3.one));
        }

        // Track combo state
        isComboActive = true;
        lastComboUpdateTime = Time.time;

        // Cancel any pending fade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }

    private void HandleComboBanked(int totalScore)
    {
        if (comboText == null || comboMultiplier == null)
            return;

        // Combo is no longer active
        isComboActive = false;

        // Start bank animation
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(BankComboAnimation(totalScore));
    }

    private IEnumerator BankComboAnimation(int score)
    {
        // Clear trick list, show only final total score in large text
        if (comboText != null)
        {
            comboText.text = "";
        }

        if (comboMultiplier != null)
        {
            comboMultiplier.text = score.ToString("N0");
            comboMultiplier.AddToClassList("combo-multiplier-banked");
        }

        // Hide timer bar
        if (comboTimerContainer != null)
        {
            comboTimerContainer.RemoveFromClassList("combo-timer-container-active");
        }

        yield return new WaitForSeconds(1.5f);

        // Fade out
        if (comboText != null)
            comboText.RemoveFromClassList("combo-text-active");

        if (comboMultiplier != null)
        {
            comboMultiplier.RemoveFromClassList("combo-multiplier-active");
            comboMultiplier.RemoveFromClassList("combo-multiplier-banked");
        }

        yield return new WaitForSeconds(0.5f);

        // Reset
        if (comboText != null)
            comboText.text = "";
        if (comboMultiplier != null)
            comboMultiplier.text = "";

        fadeCoroutine = null;
    }
}
