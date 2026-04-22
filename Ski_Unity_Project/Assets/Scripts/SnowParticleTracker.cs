using UnityEngine;

public class SnowParticleTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 15, 20);
    [SerializeField] private bool smoothTracking = true;
    [SerializeField] private float followSpeed = 5f;

    [Header("Position Constraints")]
    [Tooltip("Only track horizontal position, keep Y fixed")]
    [SerializeField] private bool lockYPosition = false;
    [SerializeField] private float fixedYPosition = 20f;

    private void FixedUpdate()
    {
        if (target == null)
            return;

        Vector3 targetPosition = target.position + offset;

        // Lock Y position if needed
        if (lockYPosition)
        {
            targetPosition.y = fixedYPosition;
        }

        // Smooth or instant tracking
        if (smoothTracking)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.fixedDeltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }
    }
}
