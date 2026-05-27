using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GroundDetector))]
public class CharacterMover : NetworkBehaviour
{
    public Camera cam;
    public float movementAcceleration; // Conservada por compatibilidad en inspector
    public float movementDeceleration; // Conservada por compatibilidad en inspector

    Vector3 currentMov;

    public float speedMovement = 6f; // Velocidad de movimiento base
    public float speedTurn = 10f;     // Velocidad de rotación
    public float jumpForce = 5f;      // Fuerza de salto

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

        // Configuración óptima del Rigidbody para evitar rozamientos raros y caídas lentas
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        gd.groundedUp.AddListener(DroppedOff);
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Bloquear entrada si el jugador está muerto
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.isDead.Value) return;

        // Salto básico y responsivo
        if (gd.grounded && InputManager.actions.Player.Jump.WasPressedThisFrame())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        // Detener movimiento por completo si está muerto
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
        // 1. Obtener la entrada del Input System
        Vector2 input = InputManager.actions.Player.Move.ReadValue<Vector2>();

        // 2. Calcular la dirección de movimiento relativa a la cámara pero APLANADA en el eje Y
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        camForward = camForward.normalized;

        Vector3 camRight = cam.transform.right;
        camRight.y = 0;
        camRight = camRight.normalized;

        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        // 3. Aplicar velocidad directa al Rigidbody en X y Z (permite frenar y girar al instante)
        Vector3 targetVelocity = moveDirection * speedMovement;
        
        // Conservamos la velocidad vertical (gravedad y saltos)
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        // 4. Rotar de forma fluida hacia la dirección a la que nos movemos
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
