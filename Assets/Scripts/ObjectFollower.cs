using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectFollower : MonoBehaviour
{
    public Transform follow;
    public Vector3 offset;
    public float followSpeed;
    void Update()
    {
        if (followSpeed >= 0)
        {
            transform.position = Vector3.Lerp(transform.position, follow.position + offset, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = follow.position + offset;
        }
    }
}
