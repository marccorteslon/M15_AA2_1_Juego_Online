using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CameraController : NetworkBehaviour
{
    public Transform lookAt;
    public Camera cam;

    [Header("Aiming")]
    public Vector2 sensitivity = new Vector2(1, 1);
    public float verticalRotationMax = 80;

    float verticalRotation;

    [Header("Collision")]
    public float collisionRadius = 1;
    public LayerMask mask;

    [Header("Distance")]
    public float distanceMax = 10;
    public float distanceMin = 1;

    float distanceDesired;
    float distanceCurrent;

    public float distanceRecovery = 1;

    private void Start()
    {
        if (!IsOwner)
        {
            if (cam != null)
            {
                cam.gameObject.SetActive(false);
            }

            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;

        if (cam == null)
        {
            cam = Camera.main;
        }

        verticalRotation = transform.localEulerAngles.x;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (UIManager.interfaceVisible)
        {
            return;
        }

        Vector2 look = InputManager.actions.Player.Look.ReadValue<Vector2>();

        float horizontal = look.x * sensitivity.x;
        transform.Rotate(Vector3.up, horizontal);

        float vertical = look.y * sensitivity.y;

        verticalRotation = Mathf.Clamp(
            verticalRotation + vertical,
            -verticalRotationMax,
            verticalRotationMax
        );

        transform.localEulerAngles = new Vector3(
            verticalRotation,
            transform.localEulerAngles.y,
            0.0f
        );
    }

    void LateUpdate()
    {
        if (!IsOwner) return;

        Ray ray = new Ray(
            transform.position,
            cam.transform.position - transform.position
        );

        Debug.DrawRay(transform.position, ray.direction * distanceMax, Color.green);

        RaycastHit hit;

        if (Physics.SphereCast(ray, collisionRadius, out hit, distanceMax, mask))
        {
            Debug.DrawRay(transform.position, ray.direction * hit.distance, Color.red);
            distanceDesired = Mathf.Max(hit.distance, distanceMin);
        }
        else
        {
            distanceDesired = distanceMax;
        }

        if (distanceDesired >= distanceCurrent)
        {
            distanceCurrent = Mathf.Lerp(
                distanceCurrent,
                distanceDesired,
                distanceRecovery * Time.deltaTime
            );
        }
        else
        {
            distanceCurrent = distanceDesired;
        }

        cam.transform.position = transform.position + ray.direction * distanceCurrent;
        cam.transform.LookAt(lookAt.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, distanceMin);
        Gizmos.DrawWireSphere(transform.position, distanceMax);

        Gizmos.color = distanceDesired > distanceCurrent ? Color.green : Color.red;

        if (cam != null)
        {
            Gizmos.DrawWireSphere(cam.transform.position, collisionRadius);
        }
    }
}