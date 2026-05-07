using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GroundDetector))]
public class CharacterMover : MonoBehaviour
{
    public Camera cam;
    public float movementAcceleration;
    public float movementDeceleration;
    Vector3 currentMov;
    public float speedMovement;
    public float speedTurn;
    public float jumpForce;
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
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gd = GetComponent<GroundDetector>();
        gd.groundedUp.AddListener(DroppedOff);
    }
    private void Update()
    {
        if (gd.grounded && InputManager.actions.Player.Jump.WasPressedThisFrame())
        {
            rb.linearVelocity = transform.up * jumpForce;
        }
    }
    void FixedUpdate()
    {
        Velocity();
        Movement();

        lastPos = transform.position;
        lastRot = transform.rotation;
    }
    void Velocity()
    {
        velocity = transform.InverseTransformDirection((transform.position - lastPos) / Time.fixedDeltaTime);
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

        airSpeedFollowupCurrent = Mathf.Clamp(airSpeedFollowupCurrent + Time.fixedDeltaTime, 0, airSpeedFollowup);
    }
    void Movement()
    {
        if (gd.grounded)
        {
            Vector3 mov = InputManager.actions.Player.Move.ReadValue<Vector2>();
            float magnitude = Mathf.Clamp01(mov.magnitude);
            if (magnitude > 0)
            {
                Vector3 movForward = cam.transform.forward * mov.y;
                Vector3 movRight = cam.transform.right * mov.x;

                mov = movForward + movRight;

                mov.y = 0;

                mov = mov.normalized * magnitude;
            }

            Debug.DrawRay(transform.position, mov, Color.yellow);
            if (magnitude > currentMov.magnitude)
            {
                currentMov = Vector3.Lerp(currentMov, mov, movementAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                currentMov = Vector3.Lerp(currentMov, mov, movementDeceleration * Time.fixedDeltaTime);
            }

            Debug.DrawRay(transform.position, currentMov, Color.green);
            Quaternion rot = currentMov.magnitude > 0.01f ? Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMov), speedTurn * Time.fixedDeltaTime) : transform.rotation;
            rb.Move(rb.position + currentMov * speedMovement * Time.fixedDeltaTime, rot);
        }
    }
    void DroppedOff()
    {
        if (airSpeedFollowupCurrent > 0)
        {
            rb.linearVelocity += transform.TransformDirection(velocity * airSpeedFollowupCurrent - rb.linearVelocity);
        }
        airSpeedFollowupCurrent = 0;
    }
}
