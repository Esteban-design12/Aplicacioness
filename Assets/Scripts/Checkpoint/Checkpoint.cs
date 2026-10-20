using UnityEngine;

/// <summary>
/// Zona de checkpoint. Colócalo en un objeto con un Collider marcado como "Is Trigger".
/// Al entrar Antonio, ese lugar pasa a ser su punto de regreso.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Opcional: dónde aparece Antonio. Si lo dejas vacío, usa la posición de este objeto.")]
    public Transform respawnPoint;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerRespawn>();
        if (player == null) return;

        player.SetCheckpoint(respawnPoint != null ? respawnPoint : transform);
        Debug.Log($"[Checkpoint] Guardado: {name}");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Vector3 p = respawnPoint != null ? respawnPoint.position : transform.position;
        Gizmos.DrawWireSphere(p, 0.4f);
        Gizmos.DrawLine(p, p + Vector3.up * 1.5f);
    }
}
