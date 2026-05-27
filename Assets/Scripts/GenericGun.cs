using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class GenericGun : MonoBehaviour
{
    public int clipMax = 30;
    public int clipCurrent = 30;

    float nextFire;

    public bool automatic = true;
    public bool reloading;

    [Min(1f / 60f)]
    public float fireTime = 0.1f;

    public float reloadTime = 0.5f;
    public UnityEvent onFire;

    public Transform firePoint;
    public GameObject bullet;

    public float positionRecover;
    public float rotationRecover;

    public Vector3 knockbackPosition;
    public Vector3 knockbackRotation;

    Vector3 originalPosition;
    Quaternion originalRotation;

    NetworkObject ownerNetworkObject;

    private void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        ownerNetworkObject = GetComponentInParent<NetworkObject>();
    }

    void Update()
    {
        if (ownerNetworkObject != null && !ownerNetworkObject.IsOwner) return;

        PlayerHealth health = GetComponentInParent<PlayerHealth>();
        if (health != null && health.isDead.Value) return;

        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, positionRecover * Time.deltaTime);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRotation, rotationRecover * Time.deltaTime);

        if (clipCurrent > 0)
        {
            bool wantsToShoot =
                InputManager.actions.Player.Attack.WasPressedThisFrame() ||
                automatic && InputManager.actions.Player.Attack.IsPressed();

            if (wantsToShoot && Time.time >= nextFire)
            {
                nextFire = Time.time + fireTime;
                Fire();
            }
        }
        else if (!reloading)
        {
            StartCoroutine(Reload_Corutine());
        }
    }

    public void Fire()
    {
        clipCurrent--;

        GameObject bulletObject = Instantiate(bullet, firePoint.position, firePoint.rotation);

        Projectile projectile = bulletObject.GetComponentInChildren<Projectile>();

        if (projectile != null)
        {
            projectile.shooterClientId = NetworkManager.Singleton.LocalClientId;
        }

        Destroy(bulletObject, 10);

        onFire.Invoke();
        StartCoroutine(Knockback_Corutine());

        if (ownerNetworkObject != null && ownerNetworkObject.IsOwner)
        {
            PlayerHealth health = ownerNetworkObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.ShootServerRpc(firePoint.position, firePoint.rotation);
            }
        }
    }

    IEnumerator Knockback_Corutine()
    {
        yield return null;

        transform.localPosition -= new Vector3(
            Random.Range(-knockbackPosition.x, knockbackPosition.x),
            Random.Range(0, knockbackPosition.y),
            Random.Range(-knockbackPosition.z, -knockbackPosition.z * .5f)
        );

        transform.localEulerAngles -= new Vector3(
            Random.Range(knockbackRotation.x * 0.5f, knockbackRotation.x),
            Random.Range(-knockbackRotation.y, knockbackRotation.y),
            Random.Range(-knockbackRotation.z, knockbackRotation.z)
        );
    }

    IEnumerator Reload_Corutine()
    {
        reloading = true;
        yield return new WaitForSeconds(reloadTime);
        clipCurrent = clipMax;
        reloading = false;
    }
}
