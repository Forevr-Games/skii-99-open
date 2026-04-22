using UnityEngine;
using System.Collections.Generic;

public class SkiCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private SkiGameManager gameManager;

    [Header("Camera Position")]
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -8);
    [SerializeField] private float followSpeed = 5f;

    [Header("Camera Rotation")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float lookAheadDistance = 10f;

    [Header("Obstacle Hiding")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float fadedAlpha = 0.75f;
    [SerializeField] private float sphereCastRadius = 1.5f;

    private Vector3 fixedForward;
    private List<Renderer> fadedRenderers = new List<Renderer>();

    private void Start()
    {
        // Lock camera to initial forward direction (downhill)
        if (target != null)
        {
            fixedForward = target.forward;
        }
    }

    private void FixedUpdate()
    {
        // Only update when in Playing state
        if (gameManager != null && gameManager.CurrentState != GameState.Playing)
            return;

        if (target == null)
            return;

        // Follow player position with world-space offset (not rotated with player)
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.fixedDeltaTime);

        // Look ahead in the fixed downhill direction
        Vector3 lookAtPosition = target.position + fixedForward * lookAheadDistance;
        Quaternion desiredRotation = Quaternion.LookRotation(lookAtPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.fixedDeltaTime);

        // Check for obstacles blocking camera view
        CheckObstacles();
    }

    private void CheckObstacles()
    {
        if (target == null)
            return;

        // Raycast from camera to player
        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,      // Start position
            sphereCastRadius,        // Sphere radius
            direction.normalized,    // Direction
            distance,               // Max distance
            obstacleLayer           // Layer mask
        );

        // Fade all obstacles in the way (permanently)
        foreach (RaycastHit hit in hits)
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && !fadedRenderers.Contains(renderer))
            {
                // Only dither if obstacle is behind the player (between camera and player)
                float obstacleZ = renderer.transform.position.z;
                float playerZ = target.position.z;

                if (obstacleZ < playerZ)
                {
                    // Fade all materials on the obstacle
                    Material[] materials = renderer.materials;
                    foreach (Material mat in materials)
                    {
                        if (mat.HasProperty("_FadeAmount"))
                        {
                            mat.SetFloat("_FadeAmount", fadedAlpha);
                        }
                        else
                        {
                            // Fallback for non-dithered materials
                            Color color = mat.color;
                            color.a = fadedAlpha;
                            mat.color = color;
                        }
                    }
                    fadedRenderers.Add(renderer);
                }
            }
        }
    }

    public void ClearFadedRenderers()
    {
        foreach (Renderer renderer in fadedRenderers)
        {
            if (renderer != null)
            {
                // Reset all materials on the obstacle
                Material[] materials = renderer.materials;
                foreach (Material mat in materials)
                {
                    if (mat.HasProperty("_FadeAmount"))
                    {
                        mat.SetFloat("_FadeAmount", 0f); // Reset to fully visible
                    }
                    else
                    {
                        // Fallback for non-dithered materials
                        Color color = mat.color;
                        color.a = 1f; // Reset to opaque
                        mat.color = color;
                    }
                }
            }
        }
        fadedRenderers.Clear();
    }
}
