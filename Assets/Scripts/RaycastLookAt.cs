using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastLookAt : MonoBehaviour
{
    public float maxDistance = 1000;
    public LayerMask lookAtMask;
    public Vector3 lookingAt { get; private set; }

    void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, lookAtMask))
        {
            lookingAt = hit.point;
        }
        else
        {
            lookingAt = transform.position + transform.forward * maxDistance;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(lookingAt, 0.1f);
    }
}
