using UnityEngine;

/// <summary>
/// Zona de checkpoint. Va en un objeto con un Collider (el tamaño de la zona).
/// Cuando Antonio pasa por aquí, este lugar pasa a ser su punto de regreso,
/// también después de perder y darle "Reintentar".
/// No necesita Rigidbody: revisa la zona por sí mismo.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Opcional: dónde aparece Antonio. Si lo dejas vacío, usa la posición de este objeto.")]
    public Transform respawnPoint;
    [Tooltip("Altura mínima de la zona de detección (m), por si el collider es muy bajito.")]
    public float alturaZona = 2.5f;
    public string mensaje = "Punto de control guardado";

    Collider zona;
    bool dentro;
    float proximaRevision;
    readonly Collider[] hits = new Collider[16];

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        zona = GetComponent<Collider>();
    }

    void Update()
    {
        if (Time.time < proximaRevision) return;
        proximaRevision = Time.time + 0.15f;

        PlayerRespawn jugador = null;
        Bounds b = zona ? zona.bounds : new Bounds(transform.position, Vector3.one * 2f);
        Vector3 centro = b.center, mitad = b.extents;
        if (mitad.y * 2f < alturaZona)
        {
            centro.y = b.min.y + alturaZona * 0.5f - 0.5f;
            mitad.y = alturaZona * 0.5f + 0.5f;
        }

        int n = Physics.OverlapBoxNonAlloc(centro, mitad, hits, Quaternion.identity, ~0,
                                           QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            jugador = hits[i].GetComponentInParent<PlayerRespawn>();
            if (jugador) break;
        }

        if (jugador && !dentro) Activar(jugador);
        dentro = jugador != null;
    }

    void Activar(PlayerRespawn jugador)
    {
        Transform punto = respawnPoint != null ? respawnPoint : transform;
        jugador.SetCheckpoint(punto);
        ProgresoPartida.GuardarCheckpoint(punto.position, Quaternion.Euler(0f, punto.eulerAngles.y, 0f));

        // (no avisar justo al reaparecer en el checkpoint)
        if (!string.IsNullOrEmpty(mensaje) && Time.timeSinceLevelLoad > 1f)
            InteractionSystem.ShowMessage(mensaje);
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
