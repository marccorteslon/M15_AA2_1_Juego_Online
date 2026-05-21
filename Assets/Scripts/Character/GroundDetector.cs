using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GroundDetector : MonoBehaviour
{
    public float height;
    public float radius;
    public float length;
    public LayerMask mask;

    public bool grounded { get; private set; }
    bool groundedLast;
    public UnityEvent groundedUp;
    public UnityEvent groundedDown;
    RaycastHit _hit;
    public RaycastHit hit { get { return _hit; } }
    private void FixedUpdate()
    {
        Ray ray = new Ray(transform.position + transform.up * height, -transform.up);


        if(Physics.SphereCast(ray, radius, out _hit, length))
        {
            if (!grounded) 
            {
                groundedDown?.Invoke();
            }
            grounded = true;
        }
        else
        {
            if (grounded)
            {
                groundedUp?.Invoke();
            }
            grounded = false;
        }
        groundedLast = grounded;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position + transform.up * height, radius);
        Gizmos.DrawWireSphere(transform.position + transform.up * height + -transform.up * length, radius);
        if(grounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + transform.up * height + -transform.up * hit.distance, radius);
        }
    }
}
