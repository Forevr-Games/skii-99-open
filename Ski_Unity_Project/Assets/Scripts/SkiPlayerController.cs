using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ForevrTools.Audio;

public class SkiPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float forwardSpeed = 15f;
    [SerializeField] private float turnMultiplier = 3f;
    [SerializeField] private float initialLaunchForce = 5f; // Initial forward impulse when starting

    [Header("Lean Settings")]
    [SerializeField] private float maxLeanAngle = 30f;
    [SerializeField] private float leanSpeed = 5f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxTurnAngle = 85f;
    [SerializeField] private float airSpinSpeed = 180f; // Degrees per second when spinning in air
    [SerializeField] private float airPitchSpeed = 360f; // Degrees per second for pitch control in air

    [Header("Ground Detection")]
    [SerializeField] private float groundRayDistance = 2.5f;
    [SerializeField] private LayerMask groundLayer;

    [Header("References")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private SkiGameManager gameManager;
    [SerializeField] private ProceduralAudioManager audioManager;

    [Header("Crash Settings")]
    [SerializeField] private GameObject playerVisuals;
    [SerializeField] private GameObject equipmentDebrisPrefab;
    [SerializeField] private float explosionForce = 5f;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Debug")]
    [SerializeField] private bool debugInvincible = false;

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private InputAction moveAction;

    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private Vector3 groundNormal;
    private float currentLean;
    private float initialYRotation;
    private float currentYRotation;
    private bool hasCrashed;
    private bool isSkiingBackwards;
    private float downhillYRotation; // The Y rotation for facing downhill

    private float touchInput = 0f;
    private float pitchInput = 0f;
    private bool hasTouchPitchInput = false;
    private GameObject spawnedDebris;
    private ProceduralSoundGenerator skiLoopGenerator;
    private ProceduralSoundGenerator slomoLoopGenerator;
    private float lastHorizontalInput = 0f;
    private float lastTurnSoundTime = -999f;
    private const float turnSoundCooldown = 0.3f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        inputActions = new InputSystem_Actions();
        moveAction = inputActions.Player.Move;

        initialYRotation = transform.eulerAngles.y;
        downhillYRotation = initialYRotation; // Store the original downhill direction
        currentYRotation = 0f;

        // Freeze player immediately on awake (before any physics updates)
        // Will be unfrozen when game starts
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void Start()
    {
        // Subscribe to game state changes
        if (gameManager != null)
        {
            gameManager.OnStateChanged += HandleStateChange;

            // Set initial state based on current game state
            if (gameManager.CurrentState == GameState.MainMenu)
            {
                FreezePlayer();
            }
        }

        // Debug invincibility
        if (debugInvincible)
        {
            gameObject.layer = 7; // User Layer 7 - Invincible
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
        EnhancedTouchSupport.Disable();
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
        if (newState == GameState.MainMenu)
        {
            FreezePlayer();
        }
        else if (newState == GameState.Playing)
        {
            UnfreezePlayer();
        }
    }

    private void FreezePlayer()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Stop ski loop sound when freezing
        if (skiLoopGenerator != null)
        {
            skiLoopGenerator.Stop();
            skiLoopGenerator = null;
        }

        // Stop slomo loop sound when freezing
        if (slomoLoopGenerator != null)
        {
            slomoLoopGenerator.Stop();
            slomoLoopGenerator = null;
        }
    }

    private void UnfreezePlayer()
    {
        if (rb != null)
        {
            rb.isKinematic = false;

            // Apply initial forward impulse to match first play behavior
            Vector3 launchDirection = transform.forward;
            rb.AddForce(launchDirection * initialLaunchForce, ForceMode.Impulse);

            // Start ski loop sound if grounded
            if (isGrounded && audioManager != null && skiLoopGenerator == null)
            {
                skiLoopGenerator = audioManager.PlayPresetLooping("SkiLoop");
            }

            // Start slomo loop sound if airborne
            if (!isGrounded && audioManager != null && slomoLoopGenerator == null)
            {
                slomoLoopGenerator = audioManager.PlayPresetLooping("SlomoLoop");
            }
        }
        else
        {
            Debug.LogError("UnfreezePlayer: Rigidbody is null!");
        }
    }

    private void Update()
    {
        // Only update when in Playing state
        if (gameManager != null && gameManager.CurrentState != GameState.Playing)
            return;

        if (hasCrashed)
            return;

        CheckGround();

        // Detect landing (was in air, now grounded)
        if (!wasGroundedLastFrame && isGrounded)
        {
            CheckBackwardsLanding();
            // Start ski loop sound when landing
            if (audioManager != null && skiLoopGenerator == null)
            {
                skiLoopGenerator = audioManager.PlayPresetLooping("SkiLoop");
            }
            // Stop slomo loop sound when landing
            if (slomoLoopGenerator != null)
            {
                slomoLoopGenerator.Stop();
                slomoLoopGenerator = null;
            }
        }

        // Detect takeoff (was grounded, now in air)
        if (wasGroundedLastFrame && !isGrounded)
        {
            // Stop ski loop sound when going airborne
            if (skiLoopGenerator != null)
            {
                skiLoopGenerator.Stop();
                skiLoopGenerator = null;
            }
            // Start slomo loop sound when going airborne
            if (audioManager != null && slomoLoopGenerator == null)
            {
                slomoLoopGenerator = audioManager.PlayPresetLooping("SlomoLoop");
            }
        }

        HandleInput();

        if (isGrounded)
        {
            AlignToSlope();
        }

        wasGroundedLastFrame = isGrounded;
    }

    private void FixedUpdate()
    {
        // Only update when in Playing state
        if (gameManager != null && gameManager.CurrentState != GameState.Playing)
        {
            // Debug.Log($"SkiPlayerController: FixedUpdate skipped - state is {gameManager.CurrentState}");
            return;
        }

        if (hasCrashed)
            return;

        HandleMovement();
        HandleTurning();
    }

    private void CheckGround()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundRayDistance, groundLayer))
        {
            isGrounded = true;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }

        // Control trail renderer emission based on grounded state
        if (trailRenderer != null)
        {
            trailRenderer.emitting = isGrounded;
        }
    }

    private void HandleInput()
    {
        // Handle touch input for mobile
        HandleTouchInput();

        // Get input from Input System
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float horizontalInput = moveInput.x;
        float verticalInput = moveInput.y;

        // Combine with touch input (touch input takes priority if active)
        if (Mathf.Abs(touchInput) > 0.01f)
        {
            horizontalInput = touchInput;
        }

        // Handle pitch input for air control
        // Negative = backflip (down/S), Positive = frontflip (up/W)
        // Account for backwards skiing - invert controls
        // Only process controller/keyboard input if there's no touch pitch input
        if (!isGrounded && !hasCrashed && !hasTouchPitchInput)
        {
            float rawPitch = verticalInput;
            pitchInput = isSkiingBackwards ? -rawPitch : rawPitch;
        }
        else if (isGrounded || hasCrashed)
        {
            pitchInput = 0f;
        }

        float targetLean = horizontalInput * maxLeanAngle;
        currentLean = Mathf.Lerp(currentLean, targetLean, leanSpeed * Time.deltaTime);

        if (visualModel != null)
        {
            // Invert lean visual when skiing backwards
            float visualLean = isSkiingBackwards ? currentLean : -currentLean;
            visualModel.localRotation = Quaternion.Euler(0, 0, visualLean);
        }

        // Play turn sound when changing direction while grounded
        if (isGrounded && !hasCrashed && Mathf.Abs(horizontalInput) > 0.3f)
        {
            // Check if direction changed or started turning from idle
            bool directionChanged = (Mathf.Sign(horizontalInput) != Mathf.Sign(lastHorizontalInput)) && Mathf.Abs(lastHorizontalInput) > 0.1f;
            bool startedTurning = Mathf.Abs(lastHorizontalInput) < 0.1f;

            if ((directionChanged || startedTurning) && Time.time - lastTurnSoundTime > turnSoundCooldown)
            {
                audioManager?.PlayPreset("SkiTurn");
                lastTurnSoundTime = Time.time;
            }
        }

        lastHorizontalInput = horizontalInput;
    }

    private void HandleTouchInput()
    {
        // Reset touch input
        touchInput = 0f;
        hasTouchPitchInput = false;

        // Get all active touches
        var activeTouches = Touch.activeTouches;

        if (activeTouches.Count == 0)
            return;

        // Define screen zones
        float screenThirdWidth = Screen.width / 3f;
        float leftThird = screenThirdWidth;
        float rightThird = screenThirdWidth * 2f;
        float screenHalfHeight = Screen.height * 0.5f;

        // Track the most recent touch in each zone category
        Touch? mostRecentLeftTouch = null;
        Touch? mostRecentRightTouch = null;
        Touch? mostRecentCenterTouch = null;

        // Find the most recent touch in each zone
        foreach (var touch in activeTouches)
        {
            Vector2 touchPosition = touch.screenPosition;

            if (touchPosition.x < leftThird)
            {
                // Left zone
                if (!mostRecentLeftTouch.HasValue || touch.touchId > mostRecentLeftTouch.Value.touchId)
                {
                    mostRecentLeftTouch = touch;
                }
            }
            else if (touchPosition.x > rightThird)
            {
                // Right zone
                if (!mostRecentRightTouch.HasValue || touch.touchId > mostRecentRightTouch.Value.touchId)
                {
                    mostRecentRightTouch = touch;
                }
            }
            else
            {
                // Center zone
                if (!mostRecentCenterTouch.HasValue || touch.touchId > mostRecentCenterTouch.Value.touchId)
                {
                    mostRecentCenterTouch = touch;
                }
            }
        }

        // Determine horizontal input based on most recent left/right touch
        if (mostRecentLeftTouch.HasValue && mostRecentRightTouch.HasValue)
        {
            // Both left and right are pressed - use the one that started more recently
            if (mostRecentLeftTouch.Value.touchId > mostRecentRightTouch.Value.touchId)
            {
                touchInput = -1f;
            }
            else
            {
                touchInput = 1f;
            }
        }
        else if (mostRecentLeftTouch.HasValue)
        {
            touchInput = -1f;
        }
        else if (mostRecentRightTouch.HasValue)
        {
            touchInput = 1f;
        }

        // Handle center zone for pitch control (only when in air)
        if (mostRecentCenterTouch.HasValue && !isGrounded && !hasCrashed)
        {
            Vector2 centerTouchPos = mostRecentCenterTouch.Value.screenPosition;

            if (centerTouchPos.y > screenHalfHeight)
            {
                // Top half (2a) - frontflip
                float rawPitch = 1f;
                pitchInput = isSkiingBackwards ? -rawPitch : rawPitch;
                hasTouchPitchInput = true;
            }
            else
            {
                // Bottom half (2b) - backflip
                float rawPitch = -1f;
                pitchInput = isSkiingBackwards ? -rawPitch : rawPitch;
                hasTouchPitchInput = true;
            }
        }
    }

    private void AlignToSlope()
    {
        Vector3 slopeForward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;

        if (slopeForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(slopeForward, groundNormal);
            Quaternion slerpedRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Extract only the pitch and roll from the slope alignment, preserve our controlled yaw
            Vector3 euler = slerpedRotation.eulerAngles;
            euler.y = initialYRotation + currentYRotation; // Keep the yaw controlled by HandleTurning
            transform.eulerAngles = euler;
        }
    }

    private void HandleMovement()
    {
        if (isGrounded)
        {
            // When skiing backwards, use opposite direction to move downhill while facing uphill
            Vector3 forward = isSkiingBackwards ? -transform.forward : transform.forward;
            Vector3 moveDirection = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
            Vector3 targetVelocity = moveDirection * forwardSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    private void CheckBackwardsLanding()
    {
        // Get the downhill direction vector
        Vector3 downhillDir = Quaternion.Euler(0, downhillYRotation, 0) * Vector3.forward;
        downhillDir.y = 0;
        downhillDir.Normalize();

        // Get player's current forward direction (flattened)
        Vector3 playerForward = transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        // Dot product: 1 = same direction (forwards), -1 = opposite (backwards)
        float dotProduct = Vector3.Dot(playerForward, downhillDir);

        // Get player's velocity direction (flattened)
        Vector3 velocityDir = rb.linearVelocity;
        velocityDir.y = 0;

        // If velocity is negligible, fall back to downhill/uphill direction
        if (velocityDir.magnitude < 0.1f)
        {
            velocityDir = dotProduct < 0 ? -downhillDir : downhillDir;
        }
        velocityDir.Normalize();

        // If dot product is negative, player is facing more uphill than downhill
        if (dotProduct < 0)
        {
            // Face opposite to velocity direction (so we ski backwards)
            Vector3 targetDir = -velocityDir;

            // Center rotation on uphill direction
            float uphillRotation = downhillYRotation + 180f;
            initialYRotation = uphillRotation;

            // Calculate current rotation relative to uphill center
            currentYRotation = Vector3.SignedAngle(Quaternion.Euler(0, uphillRotation, 0) * Vector3.forward, targetDir, Vector3.up);
            currentYRotation = Mathf.Clamp(currentYRotation, -maxTurnAngle, maxTurnAngle);

            Vector3 euler = transform.eulerAngles;
            euler.y = initialYRotation + currentYRotation;
            transform.eulerAngles = euler;

            isSkiingBackwards = true;
        }
        else
        {

            // Center rotation on downhill direction
            initialYRotation = downhillYRotation;

            // Calculate current rotation relative to downhill center
            currentYRotation = Vector3.SignedAngle(Quaternion.Euler(0, downhillYRotation, 0) * Vector3.forward, velocityDir, Vector3.up);
            currentYRotation = Mathf.Clamp(currentYRotation, -maxTurnAngle, maxTurnAngle);

            Vector3 euler = transform.eulerAngles;
            euler.y = initialYRotation + currentYRotation;
            transform.eulerAngles = euler;

            isSkiingBackwards = false;
        }
    }

    private void HandleTurning()
    {
        if (isGrounded && Mathf.Abs(currentLean) > 0.1f)
        {
            float turnAmount = currentLean * turnMultiplier * Time.fixedDeltaTime;
            currentYRotation += turnAmount;

            // Clamp rotation to prevent turning too far
            currentYRotation = Mathf.Clamp(currentYRotation, -maxTurnAngle, maxTurnAngle);

            // Apply the clamped rotation
            Vector3 euler = transform.eulerAngles;
            euler.y = initialYRotation + currentYRotation;
            transform.eulerAngles = euler;
        }
        else if (!isGrounded)
        {
            // Apply spin based on horizontal input
            if (Mathf.Abs(currentLean) > 0.1f)
            {
                float spinAmount = currentLean * (airSpinSpeed / maxLeanAngle) * Time.fixedDeltaTime;
                transform.Rotate(Vector3.up, spinAmount, Space.World);
            }

            // Apply pitch rotation (backflip/frontflip)
            if (Mathf.Abs(pitchInput) > 0.1f)
            {
                // Negative pitchInput = backflip, Positive = frontflip
                float pitchAmount = pitchInput * airPitchSpeed * Time.fixedDeltaTime;
                transform.Rotate(Vector3.right, pitchAmount, Space.Self);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundRayDistance);

        if (isGrounded)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + groundNormal * 2f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCrashed)
            return;

        // Check if the collision is with an obstacle layer
        int collisionLayer = collision.gameObject.layer;
        if (((1 << collisionLayer) & obstacleLayer) == 0)
            return;

        // Bank any active combo before crashing
        SkiTrickSystem trickSystem = FindFirstObjectByType<SkiTrickSystem>();
        if (trickSystem != null)
        {
            trickSystem.BankCombo();
        }

        Crash();
    }

    private void Crash()
    {
        hasCrashed = true;
        //reset movement is the player crashed mid air
        isGrounded = true;
        HandleMovement();

        // Stop ski loop sound
        if (skiLoopGenerator != null)
        {
            skiLoopGenerator.Stop();
            skiLoopGenerator = null;
        }

        // Stop slomo loop sound
        if (slomoLoopGenerator != null)
        {
            slomoLoopGenerator.Stop();
            slomoLoopGenerator = null;
        }

        // Play hit sound
        audioManager?.PlayPreset("Hit");

        // Stop and freeze the player physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Hide player visuals
        if (playerVisuals != null)
        {
            playerVisuals.SetActive(false);
        }

        // Clear any dithered obstacles so they become visible again
        SkiCameraController cameraController = FindFirstObjectByType<SkiCameraController>();
        if (cameraController != null)
        {
            cameraController.ClearFadedRenderers();
        }

        // Spawn debris
        if (equipmentDebrisPrefab != null)
        {
            spawnedDebris = Instantiate(equipmentDebrisPrefab, transform.position, transform.rotation);

            // Apply explosion force to all rigidbodies in the debris
            Rigidbody[] debrisRigidbodies = spawnedDebris.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody debrisRb in debrisRigidbodies)
            {
                // Add explosion force from crash point
                debrisRb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0.5f, ForceMode.Impulse);

                // Add some upward velocity
                debrisRb.linearVelocity += Vector3.up * 2f;

                // Add random spin
                debrisRb.angularVelocity = Random.insideUnitSphere * 5f;
            }
        }


    }

    public void SetForwardSpeed(float speed)
    {
        forwardSpeed = speed;
    }

    public float GetForwardSpeed()
    {
        return forwardSpeed;
    }

    public bool HasCrashed()
    {
        return hasCrashed;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public float GetPitchInput()
    {
        return pitchInput;
    }

    public void ResetPlayer()
    {
        // Reset crash state
        hasCrashed = false;
        isSkiingBackwards = false;

        // Reset lean and rotation
        currentLean = 0f;
        currentYRotation = 0f;

        // Reset rotation to face downhill
        initialYRotation = downhillYRotation;
        Vector3 euler = transform.eulerAngles;
        euler.y = initialYRotation + currentYRotation; // currentYRotation is 0 at this point
        transform.eulerAngles = euler;

        // Reset inputs
        touchInput = 0f;
        pitchInput = 0f;
        hasTouchPitchInput = false;

        // Clean up spawned debris
        if (spawnedDebris != null)
        {
            Destroy(spawnedDebris);
            spawnedDebris = null;
        }

        // Reset rigidbody velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Show player visuals
        if (playerVisuals != null)
        {
            playerVisuals.SetActive(true);
        }

        // Reset trail renderer
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = false;
        }
    }
}
