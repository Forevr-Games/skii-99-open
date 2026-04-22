using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private SkiGameManager gameManager;

    private void Start()
    {
        if (gameManager != null)
        {
            gameManager.OnStateChanged += HandleStateChange;

            // Set initial camera state
            if (gameManager.CurrentState == GameState.MainMenu)
            {
                ShowMenuCamera();
            }
            else
            {
                ShowGameplayCamera();
            }
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnStateChanged -= HandleStateChange;
        }
    }

    private void HandleStateChange(GameState oldState, GameState newState)
    {
        if (newState == GameState.MainMenu)
        {
            ShowMenuCamera();
        }
        else if (newState == GameState.Playing)
        {
            ShowGameplayCamera();
        }
    }

    private void ShowMenuCamera()
    {
        if (menuCamera != null)
        {
            menuCamera.enabled = true;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.enabled = false;
        }
    }

    private void ShowGameplayCamera()
    {
        if (menuCamera != null)
        {
            menuCamera.enabled = false;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.enabled = true;
        }
    }
}
