using UnityEngine;

/// <summary>
/// Movimiento de Antonio.
/// - WASD / flechas en la dirección hacia donde mira la cámara.
/// - No atraviesa los muros: se detiene y se desliza a lo largo de ellos.
/// </summary>
public class Movimiento : MonoBehaviour
{
    public float speed = 3.0f;
    public float velocidadGiro = 540f;

    [Header("Choque con muros")]
    public bool bloquearConParedes = true;
    [Tooltip("Capas que bloquean a Antonio (muros, objetos...).")]
    public LayerMask capasBloqueo = ~0;
    [Tooltip("Separación mínima con la pared.")]
    public float margen = 0.05f;

    // La Candileja (u otros scripts) bloquean el movimiento con esto
    public static bool puedeMoverse = true;

    private Animator animator;
    private CapsuleCollider capsula;
    private Transform camara;
    private readonly RaycastHit[] golpes = new RaycastHit[16];

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        capsula = GetComponent<CapsuleCollider>();
        if (Camera.main) camara = Camera.main.transform;
        puedeMoverse = true; // por si quedó bloqueado de una partida anterior
    }

    void Update()
    {
        // Si está bloqueado, el personaje se queda quieto en Idle
        if (!puedeMoverse)
        {
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direccion = new Vector3(horizontal, 0.0f, vertical);

        // Adelante = hacia donde mira la cámara (solo en horizontal)
        if (camara)
        {
            Vector3 adelante = camara.forward; adelante.y = 0f;
            if (adelante.sqrMagnitude > 0.001f)
                direccion = Quaternion.LookRotation(adelante.normalized) * direccion;
        }

        if (direccion.magnitude > 0.1f)
        {
            Vector3 paso = direccion.normalized * speed * Time.deltaTime;
            if (bloquearConParedes) paso = Deslizar(paso);
            transform.position += paso;

            Quaternion destino = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, destino, velocidadGiro * Time.deltaTime);
        }

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Clamp01(direccion.magnitude));
    }

    /// <summary>Recorta el paso si hay un muro y se desliza a lo largo de él.</summary>
    Vector3 Deslizar(Vector3 paso)
    {
        for (int intento = 0; intento < 2 && paso.sqrMagnitude > 0f; intento++)
        {
            if (!Choca(paso, out RaycastHit hit)) return paso;

            Vector3 normal = hit.normal; normal.y = 0f;
            if (normal.sqrMagnitude < 0.001f) return Vector3.zero;
            normal.Normalize();

            // Avanzar hasta la pared y deslizar el resto a lo largo de ella
            float hasta = Mathf.Max(0f, hit.distance - margen);
            Vector3 dir = paso.normalized;
            Vector3 resto = paso - dir * hasta;
            transform.position += dir * hasta;
            paso = Vector3.ProjectOnPlane(resto, normal);
        }
        return Choca(paso, out _) ? Vector3.zero : paso;
    }

    bool Choca(Vector3 paso, out RaycastHit cercano)
    {
        cercano = default;
        float dist = paso.magnitude;
        if (dist < 0.00001f) return false;

        // Cápsula de Antonio en metros reales, levantada un poco para no rozar el suelo
        float radio, alto; Vector3 centro;
        Bounds b = capsula ? capsula.bounds : default;
        if (capsula && b.size.y > 0.5f && b.size.y < 3f)
        {
            centro = b.center;
            radio = Mathf.Min(b.extents.x, b.extents.z) * 0.9f;
            alto = b.size.y;
        }
        else // sin cápsula o con medidas irreales: tamaño de una persona
        {
            radio = 0.3f; alto = 1.7f;
            centro = transform.position + Vector3.up * alto * 0.5f;
        }
        radio = Mathf.Clamp(radio, 0.1f, 0.6f);
        float subida = Mathf.Min(0.3f, alto * 0.2f); // escalones/suelo
        Vector3 abajo = new Vector3(centro.x, centro.y - alto * 0.5f + radio + subida, centro.z);
        Vector3 arriba = new Vector3(centro.x, centro.y + alto * 0.5f - radio, centro.z);
        if (arriba.y < abajo.y) arriba.y = abajo.y;

        int n = Physics.CapsuleCastNonAlloc(abajo, arriba, radio, paso / dist, golpes,
                                            dist + margen, capasBloqueo, QueryTriggerInteraction.Ignore);
        bool hay = false;
        float mejor = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var h = golpes[i];
            if (h.collider.transform.IsChildOf(transform)) continue;   // el propio Antonio
            if (h.distance <= 0f && h.point == Vector3.zero) continue;  // superpuesto al empezar
            if (h.normal.y > 0.6f) continue;                            // suelo / rampas
            if (h.distance < mejor) { mejor = h.distance; cercano = h; hay = true; }
        }
        return hay;
    }

    void OnDisable()
    {
        puedeMoverse = true;
    }
}
