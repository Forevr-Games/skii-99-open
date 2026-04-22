using Devvit;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Development-only debug tool for testing the ShareScore flow in the Unity Editor.
///
/// Attach this MonoBehaviour to a GameObject in a debug scene alongside a UIDocument
/// that contains an integer field ("ScoreField"), an integer field ("DistanceField"),
/// a button ("SubmitButton"), and a label ("LogLabel").
///
/// Pressing the button calls SaveDataManager.ShareScore() with the entered values,
/// which exercises the full challenge post / comment submission path using the
/// DevvitServiceMock in the Editor.
///
/// This component is not required for the game to function and should not be
/// included in production scenes.
/// </summary>
public class DevvitPostDebugger : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private SaveDataManager saveDataManager;

    private IDevvitService devvitService;

    void Start()
    {
        devvitService = DevvitServiceFactory.Instance;

        var root = document.rootVisualElement;
        IntegerField scoreField = root.Q<IntegerField>("ScoreField");
        IntegerField distanceField = root.Q<IntegerField>("DistanceField");
        Button submitButton = root.Q<Button>("SubmitButton");
        Label logLabel = root.Q<Label>("LogLabel");

        submitButton.clicked += () =>
        {
            int score = scoreField.value;
            int distance = distanceField.value;

            if (saveDataManager == null)
            {
                Debug.LogError("DevvitPostDebugger: SaveDataManager reference is missing!");
                logLabel.text = "Error: SaveDataManager reference is missing!";
                return;
            }

            saveDataManager.ShareScore(score, distance, (success, url) =>
            {
                if (success)
                {
                    logLabel.text = $"Successfully shared score: {score}, distance: {distance} - {url}";
                }
                else
                {
                    logLabel.text = $"Failed to share score: {score}, distance: {distance} - Devvit not available or error occurred";
                }
            });
        };
    }
}
