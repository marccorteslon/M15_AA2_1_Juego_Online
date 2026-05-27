using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GroundDetector))]
public class CharacterMover : NetworkBehaviour
{
    public Camera cam;
    public float movementAcceleration;
    public float movementDeceleration;

    Vector3 currentMov;

    public float speedMovement = 6f;
    public float speedTurn = 10f;
    public float jumpForce = 5f;

    Rigidbody rb;
    GroundDetector gd;

    public float airSpeedFollowup = 1f;
    float airSpeedFollowupCurrent;

    public Vector3 velocity { get; private set; }
    public float velocityAngular { get; private set; }
    public Vector3 velocityAxis { get; private set; }

    Quaternion velocityRotation;
    Vector3 lastPos;
    Quaternion lastRot;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gd = GetComponent<GroundDetector>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        gd.groundedUp.AddListener(DroppedOff);
    }

    private void Update()
    {
        if (!IsOwner) return;

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.isDead.Value) return;

        if (gd.grounded && InputManager.actions.Player.Jump.WasPressedThisFrame())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.isDead.Value)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Velocity();
        Movement();

        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    void Velocity()
    {
        velocity = transform.InverseTransformDirection(
            (transform.position - lastPos) / Time.fixedDeltaTime
        );

        velocityRotation = Quaternion.Inverse(lastRot) * transform.rotation;

        float _velocityAngular;
        Vector3 _velocityAxis;

        velocityRotation.ToAngleAxis(out _velocityAngular, out _velocityAxis);

        velocityAngular = _velocityAngular / Time.fixedDeltaTime;
        velocityAxis = _velocityAxis;

        if (Vector3.Dot(velocityAxis, transform.up) < 0)
        {
            velocityAngular *= -1;
        }

        airSpeedFollowupCurrent = Mathf.Clamp(
            airSpeedFollowupCurrent + Time.fixedDeltaTime,
            0,
            airSpeedFollowup
        );
    }

    void Movement()
    {
        Vector2 input = InputManager.actions.Player.Move.ReadValue<Vector2>();

        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        camForward = camForward.normalized;

        Vector3 camRight = cam.transform.right;
        camRight.y = 0;
        camRight = camRight.normalized;

        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        Vector3 targetVelocity = moveDirection * speedMovement;
        
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        if (moveDirection.magnitude > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speedTurn * Time.fixedDeltaTime);
        }
    }

    void DroppedOff()
    {
        if (!IsOwner) return;

        if (airSpeedFollowupCurrent > 0)
        {
            rb.linearVelocity += transform.TransformDirection(
                velocity * airSpeedFollowupCurrent - rb.linearVelocity
            );
        }

        airSpeedFollowupCurrent = 0;
    }
}
