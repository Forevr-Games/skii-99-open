using System;
using System.Collections.Generic;
using UnityEngine;
using ForevrTools.Audio;

public class SkiTrickSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkiPlayerController playerController;
    [SerializeField] private SkiGameManager gameManager;
    [SerializeField] private ProceduralAudioManager audioManager;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Settings")]
    [SerializeField] private TrickScoreSettings scoreSettings;

    [Header("Near Miss Settings")]
    [SerializeField] private float nearMissRadius = 3f;
    [SerializeField] private float closeCallRadius = 1.5f;

    [Header("Combo End Sound Thresholds")]
    [SerializeField] private int comboEndSmallThreshold = 1000;
    [SerializeField] private int comboEndMediumThreshold = 20000;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // State tracking
    private bool wasGrounded = true;
    private bool isFirstUpdate = true; // Track if this is the first update to avoid false takeoff detection
    private float airborneStartTime;
    private float accumulatedYRotation; // Track Y-axis spins
    private float accumulatedXRotation; // Track X-axis flips
    private int lastDetectedSpinDegrees = 0;
    private int lastDetectedFlips = 0;
    private Quaternion lastRotation; // Store last frame's rotation as quaternion
    private float lastWorldPitch; // Track pitch relative to world horizontal

    private ComboState currentCombo = new ComboState();
    private TrickInstance currentHangtimeTrick = null;
    private TrickInstance currentSpinTrick = null;

    // Near miss tracking
    private HashSet<GameObject> triggeredObstacles = new HashSet<GameObject>();

    // Events for UI
    public event Action<TrickInstance> OnTrickPerformed;
    public event Action<ComboState> OnComboUpdated;
    public event Action<int> OnComboBanked;

    private void Awake()
    {
        if (scoreSettings == null)
        {
            // Create default settings if none assigned
            scoreSettings = new TrickScoreSettings();
        }
    }

    private void Update()
    {
        // Only update when in Playing state
        if (gameManager != null && gameManager.CurrentState != GameState.Playing)
            return;

        if (playerController == null || gameManager == null)
            return;

        if (playerController.HasCrashed())
            return;

        bool isGrounded = playerController.IsGrounded();

        // Skip takeoff/landing detection on first update to avoid false triggers when starting mid-air
        if (!isFirstUpdate)
        {
            // Detect takeoff (was grounded, now in air)
            if (wasGrounded && !isGrounded)
            {
                HandleTakeoff();
            }

            // Detect landing (was in air, now grounded)
            if (!wasGrounded && isGrounded)
            {
                HandleLanding();
            }
        }
        else
        {
            isFirstUpdate = false;
        }

        // Track rotation while airborne
        if (!isGrounded)
        {
            TrackRotation();
            UpdateHangtimeScore();
        }

        // Check for near misses
        CheckNearMisses();

        // Check combo timer expiration
        if (currentCombo.isActive && Time.time - currentCombo.lastTrickTime > scoreSettings.comboTimeWindow)
        {
            EndCombo();
        }

        wasGrounded = isGrounded;
    }

    private void CheckNearMisses()
    {
        if (playerController == null)
            return;

        Vector3 playerPosition = playerController.transform.position;

        // Use overlap sphere to detect nearby obstacles (works both grounded and in air)
        Collider[] nearbyObstacles = Physics.OverlapSphere(playerPosition, nearMissRadius, obstacleLayer);

        foreach (Collider obstacleCollider in nearbyObstacles)
        {
            GameObject obstacle = obstacleCollider.gameObject;

            if (obstacle.CompareTag("ignore"))
            {
                continue;
            }

            // Skip if already triggered for this obstacle
            if (triggeredObstacles.Contains(obstacle))
                continue;

            // Calculate distance to closest point on obstacle
            Vector3 closestPoint = obstacleCollider.ClosestPoint(playerPosition);
            float distance = Vector3.Distance(playerPosition, closestPoint);

            // Determine if it's a close call or near miss
            bool isCloseCall = distance <= closeCallRadius;

            // Trigger near miss
            RegisterNearMiss(isCloseCall);
            triggeredObstacles.Add(obstacle);
        }

        // Clear triggered obstacles that are now far away (cleanup)
        triggeredObstacles.RemoveWhere(obj =>
            obj == null || Vector3.Distance(playerPosition, obj.transform.position) > nearMissRadius * 3f
        );
    }

    private void HandleTakeoff()
    {
        // Play jump sound
        audioManager?.PlayPreset("Jump");

        airborneStartTime = Time.time;
        accumulatedYRotation = 0f;
        accumulatedXRotation = 0f;
        lastDetectedSpinDegrees = 0;
        lastDetectedFlips = 0;
        currentSpinTrick = null;
        lastRotation = playerController.transform.rotation;
        lastWorldPitch = GetWorldPitchAngle();

        // Start hangtime trick
        currentHangtimeTrick = new TrickInstance
        {
            type = TrickType.Hangtime,
            displayName = "Hangtime",
            baseScore = 0,
            multiplier = 1,
            timestamp = Time.time
        };

        // Add to combo immediately but it will update during flight
        if (!currentCombo.isActive)
        {
            currentCombo = new ComboState();
            currentCombo.isActive = true;
        }

        currentCombo.tricks.Add(currentHangtimeTrick);
        currentCombo.lastTrickTime = Time.time;
    }

    private void HandleLanding()
    {
        float airTime = Time.time - airborneStartTime;
        float finalPitch = GetWorldPitchAngle();

        // Finalize hangtime score
        if (currentHangtimeTrick != null)
        {
            currentHangtimeTrick.baseScore = Mathf.FloorToInt(airTime * scoreSettings.hangtimeScorePerSecond);
            OnTrickPerformed?.Invoke(currentHangtimeTrick);
            currentHangtimeTrick = null;
        }

        // Spin and flip tricks are now handled in real-time during flight
        // Reset tracking
        currentSpinTrick = null;
        lastDetectedSpinDegrees = 0;
        lastDetectedFlips = 0;

        // Update combo display
        if (currentCombo.isActive)
        {
            OnComboUpdated?.Invoke(currentCombo);
        }

        // Play landing sound
        audioManager?.PlayPreset("Landing");

        // Reset air tracking
        accumulatedYRotation = 0f;
        accumulatedXRotation = 0f;
    }

    private float GetWorldPitchAngle()
    {
        // Get player's forward and up vectors
        Vector3 forward = playerController.transform.forward;
        Vector3 up = playerController.transform.up;

        // Project forward onto horizontal plane
        Vector3 forwardHorizontal = new Vector3(forward.x, 0, forward.z);
        float horizontalMag = forwardHorizontal.magnitude;

        // Calculate base pitch angle using Atan2
        float basePitch = Mathf.Atan2(forward.y, horizontalMag) * Mathf.Rad2Deg;

        // Check if we're upside down (up vector pointing down)
        bool isUpsideDown = up.y < 0;

        // Adjust angle based on orientation to get full [-180, 180] range
        if (isUpsideDown)
        {
            // When upside down, map the angle to the [90, 180] or [-180, -90] range
            if (basePitch >= 0)
                return 180f - basePitch;  // 90° to 180°
            else
                return -180f - basePitch; // -180° to -90°
        }
        else
        {
            // Right-side up: -90° to 90°
            return basePitch;
        }
    }

    private void TrackRotation()
    {
        // Calculate rotation delta using quaternions (avoids gimbal lock)
        Quaternion currentRotation = playerController.transform.rotation;
        Quaternion deltaRotation = currentRotation * Quaternion.Inverse(lastRotation);

        // Convert to axis-angle to extract rotation amount
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        // Normalize angle to [-180, 180]
        if (angle > 180f)
            angle -= 360f;

        // Project rotation onto world Y-axis for spins
        float yComponent = Vector3.Dot(axis, Vector3.up) * angle;
        accumulatedYRotation += Mathf.Abs(yComponent);

        // Track pitch relative to world horizontal for flips
        float currentWorldPitch = GetWorldPitchAngle();
        float deltaPitch = Mathf.DeltaAngle(lastWorldPitch, currentWorldPitch);
        float previousAccumulated = accumulatedXRotation;
        accumulatedXRotation += deltaPitch;
        lastWorldPitch = currentWorldPitch;

        // Update last rotation
        lastRotation = currentRotation;

        // Detect tricks in real-time
        DetectRealtimeSpins();
        DetectRealtimeFlips();
    }

    private void DetectRealtimeSpins()
    {
        float absRotation = Mathf.Abs(accumulatedYRotation);

        // Need at least 150° to count as a 180 (±30° tolerance)
        const float tolerance = 30f;
        if (absRotation < 180f - tolerance)
        {
            return;
        }

        // Quantize to nearest 180° increment
        int spinIncrement = Mathf.RoundToInt(absRotation / 180f);
        int spinDegrees = spinIncrement * 180;

        // Only update if we've detected a new spin increment
        if (spinDegrees == lastDetectedSpinDegrees)
        {
            return;
        }

        lastDetectedSpinDegrees = spinDegrees;

        int spinScore = scoreSettings.GetSpinScore(spinDegrees);
        if (spinScore == 0)
        {
            return;
        }

        // If we already have a spin trick, update it. Otherwise, create a new one
        if (currentSpinTrick != null)
        {
            // Update existing spin trick
            currentSpinTrick.displayName = $"{spinDegrees}° Spin";
            currentSpinTrick.baseScore = spinScore;
        }
        else
        {
            // Create new spin trick
            currentSpinTrick = new TrickInstance
            {
                type = TrickType.Spin,
                displayName = $"{spinDegrees}° Spin",
                baseScore = spinScore,
                multiplier = 1,
                timestamp = Time.time
            };

            currentCombo.tricks.Add(currentSpinTrick);
        }

        currentCombo.lastTrickTime = Time.time;

        OnTrickPerformed?.Invoke(currentSpinTrick);
        OnComboUpdated?.Invoke(currentCombo);
    }

    private void DetectRealtimeFlips()
    {
        // Track both backflips (negative X rotation) and frontflips (positive X rotation)
        float absRotation = Mathf.Abs(accumulatedXRotation);

        // Detect at 180° and award on completing 360°
        // This triggers halfway through so double/triple flips are counted progressively
        const float minFlipRotation = 180f;
        if (absRotation < minFlipRotation)
        {
            return;
        }

        // Count flips: first flip at 270° (3/4 rotation), then every 360° after
        // This is forgiving for the first flip but requires full rotations for multiples
        int flipCount;
        if (absRotation < 270f)
        {
            flipCount = 0;
        }
        else
        {
            flipCount = 1 + Mathf.FloorToInt((absRotation - 270f) / 360f);
        }

        // Only update if we've detected a new flip
        if (flipCount == lastDetectedFlips)
        {
            return;
        }

        // Award points for each NEW flip since last detection
        int newFlips = flipCount - lastDetectedFlips;
        lastDetectedFlips = flipCount;

        // Determine if it's a backflip or frontflip based on accumulated rotation
        // Backflip = nose goes UP (positive pitch increase)
        // Frontflip = nose goes DOWN (negative pitch decrease)
        bool isBackflip = accumulatedXRotation > 0;
        string flipName = isBackflip ? "Backflip" : "Frontflip";

        // Create a separate trick instance for each new flip
        for (int i = 0; i < newFlips; i++)
        {
            TrickInstance flipTrick = new TrickInstance
            {
                type = TrickType.Backflip,
                displayName = flipName,
                baseScore = scoreSettings.backflipScore,
                multiplier = 1,
                timestamp = Time.time
            };

            currentCombo.tricks.Add(flipTrick);
            currentCombo.lastTrickTime = Time.time;

            OnTrickPerformed?.Invoke(flipTrick);
        }

        OnComboUpdated?.Invoke(currentCombo);
    }

    private void UpdateHangtimeScore()
    {
        if (currentHangtimeTrick == null)
            return;

        float airTime = Time.time - airborneStartTime;
        currentHangtimeTrick.baseScore = Mathf.FloorToInt(airTime * scoreSettings.hangtimeScorePerSecond);

        // Keep combo alive while in the air
        currentCombo.lastTrickTime = Time.time;

        // Update UI periodically (every 0.1 seconds to avoid spamming)
        if (Time.frameCount % 6 == 0) // Roughly every 0.1s at 60fps
        {
            OnComboUpdated?.Invoke(currentCombo);
        }
    }


    public void RegisterNearMiss(bool isVeryClose)
    {
        if (playerController.HasCrashed())
            return;

        // Play near miss sound
        audioManager?.PlayPreset("NearMiss");

        float score = scoreSettings.nearMissBaseScore;
        if (isVeryClose)
            score *= scoreSettings.nearMissCloseMultiplier;

        // Start combo if not active
        if (!currentCombo.isActive)
        {
            currentCombo = new ComboState();
            currentCombo.isActive = true;
        }

        TrickInstance trick = new TrickInstance
        {
            type = TrickType.NearMiss,
            displayName = isVeryClose ? "Close Call!" : "Near Miss!",
            baseScore = Mathf.FloorToInt(score),
            multiplier = 1,
            timestamp = Time.time
        };

        currentCombo.tricks.Add(trick);
        currentCombo.lastTrickTime = Time.time;

        OnTrickPerformed?.Invoke(trick);
        OnComboUpdated?.Invoke(currentCombo);
    }
    public void BankCombo()
    {
        if (!currentCombo.isActive || currentCombo.tricks.Count == 0)
            return;

        EndCombo();
    }

    private void EndCombo()
    {
        if (!currentCombo.isActive)
            return;

        int totalScore = currentCombo.GetTotalScore();

        // Play combo end sound based on score
        if (audioManager != null && totalScore > 0)
        {
            if (totalScore < comboEndSmallThreshold)
            {
                audioManager.PlayPreset("ComboEndSm");
            }
            else if (totalScore < comboEndMediumThreshold)
            {
                audioManager.PlayPreset("ComboEndMd");
            }
            else
            {
                audioManager.PlayPreset("ComboEndLg");
            }
        }

        // Bank into game manager
        if (gameManager != null)
        {
            gameManager.AddTrickScore(totalScore);
        }

        OnComboBanked?.Invoke(totalScore);

        // Reset combo
        currentCombo = new ComboState();
        currentHangtimeTrick = null;
    }

    public void ResetTricks()
    {
        // Reset combo state
        currentCombo = new ComboState();
        currentHangtimeTrick = null;
        currentSpinTrick = null;

        // Clear triggered obstacles
        triggeredObstacles.Clear();

        // Reset rotation tracking
        wasGrounded = true;
        lastDetectedSpinDegrees = 0;
        lastDetectedFlips = 0;
        accumulatedYRotation = 0f;
        accumulatedXRotation = 0f;
    }
}
