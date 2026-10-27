using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Va en la Candileja (el objeto "IA"). Si toca a Antonio, lo devuelve a su checkpoint.
/// Detecta el contacto por distancia, así que no depende de Rigidbody ni de triggers.
/// </summary>
public class CandilejaTouch : MonoBehaviour
{
    [Header("Contacto")]
    [Tooltip("Distancia a la que cuenta como 'tocó al jugador'.")]
    public float catchRadius = 1f;
    [Tooltip("Altura desde los pies de la Candileja donde se mide (≈ centro del cuerpo).")]
    public float heightOffset = 1f;
    public LayerMask detectMask = ~0;

    [Header("Después de atrapar")]
    [Tooltip("Evita que atrape varias veces seguidas al reaparecer Antonio.")]
    public float cooldown = 2f;
    [Tooltip("La Candileja vuelve a su punto inicial para no dejar a Antonio en un bucle.")]
    public bool returnToStartOnCatch = true;

    [Header("Eventos")]
    [Tooltip("Sonido de susto, animación, etc.")]
    public UnityEvent onCaught;

    NavMeshAgent agent;
    Vector3 startPosition;
    Quaternion startRotation;
    float nextAllowedTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        if (Time.time < nextAllowedTime) return;

        Vector3 center = transform.position + Vector3.up * heightOffset;
        Collider[] hits = Physics.OverlapSphere(center, catchRadius, detectMask,
            QueryTriggerInteraction.Ignore);

        foreach (var hit in hits)
        {
            var player = hit.GetComponentInParent<PlayerRespawn>();
            if (player == null) continue;

            Catch(player);
            break;
        }
    }

    void Catch(PlayerRespawn player)
    {
        nextAllowedTime = Time.time + cooldown;

        player.Respawn();
        onCaught?.Invoke();

        if (returnToStartOnCatch) ReturnToStart();
    }

    void ReturnToStart()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(startPosition);
            transform.rotation = startRotation;
        }
        else
        {
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * heightOffset, catchRadius);
    }
}
