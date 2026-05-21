using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    public int maxHealth = 100;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ulong shooterClientId)
    {
        if (currentHealth.Value <= 0) return;

        currentHealth.Value -= damage;

        Debug.Log(
            $"Jugador {OwnerClientId} recibió daño. " +
            $"Vida restante: {currentHealth.Value}"
        );

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"Jugador {OwnerClientId} murió.");

        // Si tiene NetworkObject lo destruye en red
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}