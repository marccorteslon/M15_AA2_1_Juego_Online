
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterAnimator : MonoBehaviour
{
    public GroundDetector gd;
    public CharacterMover cm;
    public Camera cam;
    Animator anim;
    public float rotationScale;
    public Transform gunPivot;
    public Transform gunRightHand;
    public Transform gunLeftHand;

    [Range(0f, 1f)]
    public float lookAtMaxAngle = 0.5f;
    public RaycastLookAt cameraLookAt;
    public float lookAtSpeed = 10;
    public float lookAtDistance = 10;
    Vector3 lookat;
    private void Start()
    {
        anim = GetComponent<Animator>();
        if(cam == null)
        {
            cam = Camera.main;
        }

        lookat = cameraLookAt.lookingAt;
    }
    void Update()
    {
        anim.SetFloat("Sideways", cm.velocity.x);
        anim.SetFloat("Upwards", cm.velocity.y);
        anim.SetFloat("Forward", cm.velocity.z);
        anim.SetFloat("Rotation", cm.velocityAngular * rotationScale);
        anim.SetBool("Grounded", gd.grounded);

        FixLookat();

        gunPivot.LookAt(lookat);
    }

    private void FixLookat()
    {
        lookat = Vector3.Lerp(lookat, cameraLookAt.lookingAt, lookAtSpeed * Time.deltaTime);

        Vector3 forwardLookAt = (lookat - cameraLookAt.transform.position).normalized;
        float dot = Vector3.Dot(forwardLookAt, transform.forward);
        if (dot < lookAtMaxAngle)
        {
            Vector3 axis = Vector3.Cross(forwardLookAt, transform.forward);
            float angle = Vector3.SignedAngle(forwardLookAt, transform.forward, axis);
            float distance = Vector3.Distance(lookat, cameraLookAt.transform.position);
            float maxAngle = Mathf.Acos(lookAtMaxAngle) * Mathf.Rad2Deg;
            forwardLookAt = Quaternion.AngleAxis(angle > 0 ? -maxAngle : maxAngle, axis) * transform.forward;
            lookat = cameraLookAt.transform.position + forwardLookAt * distance;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
        anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
        anim.SetIKPosition(AvatarIKGoal.RightHand, gunRightHand.position);
        anim.SetIKPosition(AvatarIKGoal.LeftHand, gunLeftHand.position);
        anim.SetIKRotation(AvatarIKGoal.RightHand, gunRightHand.rotation);
        anim.SetIKRotation(AvatarIKGoal.LeftHand, gunLeftHand.rotation);
        anim.SetLookAtPosition(lookat);
        anim.SetLookAtWeight(1, 1, 1, 1);
    }
}
