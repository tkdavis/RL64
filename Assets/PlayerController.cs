using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float accelerationForce = 2000f;
    public float turnForce = 1500f;
    public float maxSpeed = 30f;
    public float airControlFactor = 0.5f;
    public float jumpForce = 500f;
    public float flipTorque = 5000f; // Torque for flipping
    public float jumpCount = 1;
    public float stabilizationForce = 10f; // Adjust the force applied to stabilize the car
    public float rollThreshold = 45f;       // Angle threshold to start stabilization

    [SerializeField]
    private AnimationCurve frictionCurve;

    [SerializeField]
    private AnimationCurve turnCurve;

    private float xVel;
    private float zVel;
    private Rigidbody rb;
    private bool isGrounded;
    private bool isGroundedUpsideDown;
    private bool hasFlipped;
    private float vertInput;
    private float moveInput;
    private float reverseInput;
    private float turnInput;
    private bool jumpRequested;
    private bool isJumping;
    private float flipCooldown = 0.5f;
    private float flipCooldownRemaining = 0.0f;
    private bool canFlip = true;
    private AudioSource engineSfx;
    public AudioClip engineClip;
    public AudioClip engineIdleClip;
    private bool assignedIdleClip = true;


    void Start()
    {
        Application.targetFrameRate = 120;
        rb = GetComponent<Rigidbody>();
        engineSfx = GetComponent<AudioSource>();
        rb.centerOfMass = Vector3.zero;
        hasFlipped = false; // Initialize flip status
    }

    void Update()
    {
        // Read inputs in Update
        moveInput = Input.GetAxis("Throttle"); // Forward/Backward
        reverseInput = Input.GetAxis("ReverseThrottle");
        vertInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal"); // Left/Right
        jumpRequested = Input.GetButtonDown("Fire1");

        if (jumpRequested && jumpCount > 0) isJumping = true;

        // Ground check
        HandleAirControl();
        UpdateFlipTimer();
        AdjustAudioToVelocity();
    }

    void FixedUpdate()
    {
        // Handle physics-based movement and forces in FixedUpdate
        HandleMovement();
        CheckIfGroundedUpsideDown();

        // Check if the car is significantly tilted (rolled)
        if (IsCarRolled())
        {
            StabilizeCar();
        }
    }

    private void HandleMovement()
    {
        if (isGrounded)
        {
            jumpCount = 1;
            // Reset flip status when grounded
            if (hasFlipped)
            {
                hasFlipped = false; // Allow flipping again after touching the ground
            }

            // Accelerate
            if (moveInput != 0 && isGrounded)
            {
                rb.AddForce(transform.forward * moveInput * accelerationForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            }

            if (reverseInput != 0 && isGrounded)
            {
                rb.AddForce(-transform.forward * reverseInput * accelerationForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            }

            // Turning
            if (rb.velocity.magnitude > 0.1f && transform.InverseTransformDirection(rb.velocity).z > 0)
            {
                rb.AddTorque(transform.up * turnInput * CalculateTurnForce()/*turnForce*/ * Time.fixedDeltaTime, ForceMode.Acceleration);
            } else if (rb.velocity.magnitude > 0.1f && transform.InverseTransformDirection(rb.velocity).z < 0)
            {
                // Reverse turn input if going in reverse
                rb.AddTorque(transform.up * -turnInput * CalculateTurnForce()/*turnForce*/ * Time.fixedDeltaTime, ForceMode.Acceleration);
            }

            // Side Friction
            rb.AddForce(CalculateFriction() * transform.right * Mathf.Sign(-xVel) * rb.velocity.magnitude);

            // Limit speed
            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }
        else
        {
            // Air control
            rb.AddTorque(transform.right * vertInput * airControlFactor * Time.fixedDeltaTime, ForceMode.Acceleration);

            // Handle flipping in the air
            //HandleFlip();
        }

        if (isJumping && flipDirectionPressed() && jumpCount > 0)
        {
            HandleFlip();
        } else if (isJumping && jumpCount > 0)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = false;
            jumpCount -= 1;
        }
    }

    private void HandleFlip()
    {
        if (isJumping)
        {
            if (canFlip && !hasFlipped && jumpCount > 0)
            {
                // Perform flip based on input
                if (vertInput > 0) // Moving forward
                {
                    rb.AddTorque(transform.right * flipTorque * Time.fixedDeltaTime, ForceMode.Impulse);
                    rb.AddForce(transform.forward * 2f, ForceMode.Impulse);
                    StartCoroutine(CounterFlipTorque("backflip"));
                }
                else if (vertInput < 0) // Moving backward
                {
                    rb.AddTorque(-transform.right * flipTorque * Time.fixedDeltaTime, ForceMode.Impulse);
                    rb.AddForce(-transform.forward * 2f, ForceMode.Impulse);
                    StartCoroutine(CounterFlipTorque("frontflip"));
                }
                else if (turnInput > 0.1f) // Turning right
                {
                    rb.AddTorque(-transform.forward * (flipTorque / 2.0f) * Time.fixedDeltaTime, ForceMode.Impulse);
                    rb.AddForce(transform.right * 4.0f, ForceMode.Impulse);
                    StartCoroutine(CounterFlipTorque("sideflipLeft"));
                }
                else if (turnInput < -0.1f) // Turning left
                {
                    rb.AddTorque(transform.forward * (flipTorque / 2.0f) * Time.fixedDeltaTime, ForceMode.Impulse);
                    rb.AddForce(-transform.right * 4.0f, ForceMode.Impulse);
                    StartCoroutine(CounterFlipTorque("sideflipRight"));
                }

                hasFlipped = true; // Prevent further flips until grounded
                canFlip = false;
                flipCooldownRemaining = flipCooldown;
                jumpCount = 0;
                isJumping = false;
            }
        }
    }

    private bool flipDirectionPressed()
    {
        return vertInput > 0 || vertInput < 0 || turnInput > 0.1f || turnInput < -0.1f;
    }

    private void HandleAirControl()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + (transform.up * 0.1f), transform.TransformDirection(Vector3.down), out hit, 0.2f))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
            
            // Apply pitch control in the air
            if (isGrounded == false)
            {
                ApplyPitchControl();
            }
        }
    }

    private void CheckIfGroundedUpsideDown()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.up), out hit, 0.6f))
        {
            isGroundedUpsideDown = true;
        }
    }

    private void ApplyPitchControl()
    {
        // Apply pitch control based on vertical input
        float pitchInput = vertInput; // Forward/backward input
        float pitchTorque = pitchInput * airControlFactor * Time.fixedDeltaTime;

        // Apply torque to pitch the car
        rb.AddTorque(transform.right * pitchTorque, ForceMode.Acceleration);
    }

    private float CalculateFriction()
    {
        xVel = transform.InverseTransformDirection(rb.velocity).x;
        zVel = transform.InverseTransformDirection(rb.velocity).z;
        float frictionRatio = xVel / Mathf.Max(xVel + zVel, 0.01f);
        float slideFriction = frictionCurve.Evaluate(frictionRatio);
        return slideFriction;
    }

    private float CalculateTurnForce()
    {
        float forwardVelocity = transform.InverseTransformDirection(rb.velocity).z;
        float turnForceAmount = turnCurve.Evaluate(forwardVelocity);
        if (!isGrounded)
        {
            turnForceAmount = 1;
        }

        return turnForceAmount * turnForce;
    }

    private void UpdateFlipTimer()
    {
        if (!canFlip)
        {
            flipCooldownRemaining -= Time.deltaTime;

            if (flipCooldownRemaining <= 0)
            {
                flipCooldownRemaining = 0;
                canFlip = true;
            }
        }
    }

    IEnumerator CounterFlipTorque(string flipType)
    {
        // Wait for 1 second
        yield return new WaitForSeconds(1.0f);

        // Apply counter-torque to counteract the previous force
        if (flipType == "backflip")
        {
            rb.AddTorque(-transform.right * flipTorque * Time.fixedDeltaTime, ForceMode.Impulse);
        } else if (flipType == "frontflip")
        {
            rb.AddTorque(transform.right * flipTorque * Time.fixedDeltaTime, ForceMode.Impulse);
        } else if (flipType == "sideflipRight")
        {
            rb.AddTorque(-transform.forward * (flipTorque / 2.0f) * Time.fixedDeltaTime, ForceMode.Impulse);
        } else if (flipType == "sideflipLeft")
        {
            rb.AddTorque(transform.forward * (flipTorque / 2.0f) * Time.fixedDeltaTime, ForceMode.Impulse);
        }
    }

    bool IsCarRolled()
    {
        // Check the car's roll (rotation around Z-axis)
        // Assuming the car should be upright with a roll of 0 degrees
        float roll = transform.eulerAngles.z;

        // Normalize roll to be within [-180, 180] degrees
        if (roll > 180f)
        {
            roll -= 360f;
        }

        return Mathf.Abs(roll) > rollThreshold && isGroundedUpsideDown;
    }

    void StabilizeCar()
    {
        if (isGroundedUpsideDown && jumpRequested)
        {
            // Extract current roll angle (rotation around z-axis)
            float currentRoll = transform.eulerAngles.z;

            // Normalize roll to be within [-180, 180] degrees
            if (currentRoll > 180f)
            {
                currentRoll -= 360f;
            }

            // Calculate roll error to bring the car back to upright position (0 degrees roll)
            float rollError = -currentRoll; // Negative of current roll to counteract it

            // Create a torque vector that only affects the roll (z-axis)
            Vector3 torque = new Vector3(0, 0, rollError) * stabilizationForce;

            // Apply the torque to the Rigidbody to correct roll
            rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }

    void AdjustAudioToVelocity()
    {
        if (rb.velocity.magnitude < 0.1f && !assignedIdleClip)
        {
            engineSfx.Stop();
            engineSfx.clip = engineIdleClip;
            assignedIdleClip = true;
            engineSfx.pitch = 0.6f;
            engineSfx.Play();
        } else if (rb.velocity.magnitude > 0.1f && assignedIdleClip) {
            engineSfx.Stop();
            engineSfx.clip = engineClip;
            assignedIdleClip = false;
            engineSfx.Play();
        }

        if (!assignedIdleClip)
        {
            engineSfx.pitch = Mathf.Max(Mathf.Min(1f, rb.velocity.magnitude / maxSpeed), 0.1f);
        }
    }
}
