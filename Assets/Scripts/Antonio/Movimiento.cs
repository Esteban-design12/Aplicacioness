using UnityEngine;

/// <summary>
/// Movimiento de Antonio.
/// - WASD / flechas en la dirección hacia donde mira la cámara.
/// - No atraviesa los muros: se detiene y se desliza a lo largo de ellos.
/// - Sube rampas y escalones bajos (hasta "alturaEscalon").
/// - Baja pegado a las rampas y, si queda en el aire, cae con gravedad hasta el suelo.
/// - El pivote de Antonio está en los PIES, y el detector de muros se calcula desde ahí.
/// </summary>
public class Movimiento : MonoBehaviour
{
    public float speed = 3.0f;
    public float velocidadGiro = 540f;

    [Header("Choque con muros")]
    public bool bloquearConParedes = true;
    [Tooltip("Capas que bloquean a Antonio (muros, pisos, objetos...).")]
    public LayerMask capasBloqueo = ~0;
    [Tooltip("Separación mínima con la pared.")]
    public float margen = 0.05f;
    [Tooltip("Lo que sea más bajo que esto (desde los pies) no lo frena y lo sube: rampas, bordes, escalones.")]
    public float alturaEscalon = 0.25f;
    [Tooltip("Altura del cuerpo de Antonio, medida desde los pies.")]
    public float alturaCuerpo = 1.2f;

    [Header("Rampas, escalones y caída")]
    [Tooltip("Si está activo, Antonio sigue la altura del suelo y cae cuando queda en el aire.")]
    public bool seguirRampas = true;
    [Tooltip("Si el suelo está a esta distancia o menos por debajo, baja pegado a él (rampas). Si está más abajo, cae.")]
    public float pegadoAlSuelo = 0.3f;
    [Tooltip("Fuerza de la caída. Más alto = cae más rápido.")]
    public float gravedad = 20f;
    [Tooltip("Velocidad máxima de caída.")]
    public float velocidadMaximaCaida = 15f;
    [Tooltip("Súbelo si los zapatos se hunden en el piso; bájalo si se ve flotando un poquito.")]
    public float ajustePies = 0f;

    [Header("Depuración")]
    [Tooltip("Escribe en la Console con qué objeto choca Antonio.")]
    public bool depurar = false;

    // La Candileja (u otros scripts) bloquean el movimiento con esto
    public static bool puedeMoverse = true;

    private Animator animator;
    private CapsuleCollider capsula;
    private Transform camara;
    private float ultimoLog;
    private float velocidadVertical;
    private bool enSuelo;
    private readonly RaycastHit[] golpes = new RaycastHit[16];
    private readonly RaycastHit[] golpesSuelo = new RaycastHit[32];

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        capsula = GetComponent<CapsuleCollider>();
        if (Camera.main) camara = Camera.main.transform;
        puedeMoverse = true; // por si quedó bloqueado de una partida anterior

        // La altura la maneja este script: se evita que la física de Unity pelee con él
        Rigidbody cuerpo = GetComponent<Rigidbody>();
        if (cuerpo)
        {
            cuerpo.useGravity = false;
            cuerpo.isKinematic = true;
        }
    }

    void Update()
    {
        // Si está bloqueado, el personaje se queda quieto en Idle
        if (!puedeMoverse)
        {
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            AudioJuego.Pasos(false);
            AplicarAltura(); // aunque esté bloqueado, no se queda flotando
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

        // Se revisa el suelo en cada cuadro, se esté moviendo o no
        AplicarAltura();

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Clamp01(direccion.magnitude));

        // Sonido de pasos mientras camina (no en pausa ni cayendo)
        bool pisando = enSuelo || !seguirRampas;
        AudioJuego.Pasos(direccion.magnitude > 0.1f && Time.timeScale > 0f && pisando);
    }

    /// <summary>
    /// Pone a Antonio a la altura correcta:
    /// - Si el suelo está un poco más arriba (rampa, escalón), lo sube.
    /// - Si venía pisando y el suelo está un poco más abajo, lo baja pegado.
    /// - Si el suelo está lejos hacia abajo, cae con gravedad hasta tocarlo.
    /// </summary>
    void AplicarAltura()
    {
        if (!seguirRampas || Time.deltaTime <= 0f) return; // en pausa no hace nada

        Vector3 pos = transform.position;
        float pies = pos.y - ajustePies;

        // Si no hay ningún suelo debajo, no se deja caer al vacío
        if (!BuscarSuelo(new Vector3(pos.x, pies, pos.z), out float suelo))
        {
            velocidadVertical = 0f;
            return;
        }

        float distancia = pies - suelo; // positivo: está por encima del suelo

        if (distancia <= 0f || (enSuelo && distancia <= pegadoAlSuelo))
        {
            pies = suelo;
            velocidadVertical = 0f;
            enSuelo = true;
        }
        else
        {
            velocidadVertical = Mathf.Max(velocidadVertical - gravedad * Time.deltaTime,
                                          -velocidadMaximaCaida);
            pies += velocidadVertical * Time.deltaTime;
            enSuelo = pies <= suelo;
            if (enSuelo) // aterrizó: nunca pasa por debajo del suelo
            {
                pies = suelo;
                velocidadVertical = 0f;
            }
        }

        transform.position = new Vector3(pos.x, pies + ajustePies, pos.z);
    }

    /// <summary>
    /// Busca el suelo más alto que esté debajo de los pies (o apenas por encima,
    /// hasta "alturaEscalon"). Ignora paredes y a Antonio.
    /// </summary>
    bool BuscarSuelo(Vector3 pies, out float y)
    {
        y = 0f;
        float altoRayo = alturaEscalon + 0.01f;
        Vector3 origen = pies + Vector3.up * altoRayo;
        int n = Physics.RaycastNonAlloc(origen, Vector3.down, golpesSuelo,
                                        100f, capasBloqueo,
                                        QueryTriggerInteraction.Ignore);
        bool hay = false;
        float mejor = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            var h = golpesSuelo[i];
            if (h.collider.transform.IsChildOf(transform)) continue; // el propio Antonio
            if (h.normal.y < 0.6f) continue;                          // paredes
            if (h.point.y > mejor) { mejor = h.point.y; hay = true; }
        }
        if (hay) y = mejor;
        return hay;
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

        // El pivote de Antonio está en los pies: la cápsula se calcula desde ahí,
        // dejando libre lo que esté por debajo de "alturaEscalon".
        float radio = 0.3f;
        if (capsula)
        {
            Bounds b = capsula.bounds;
            radio = Mathf.Min(b.extents.x, b.extents.z) * 0.9f;
        }
        radio = Mathf.Clamp(radio, 0.1f, 0.6f);

        Vector3 pies = transform.position - Vector3.up * ajustePies;
        Vector3 abajo = pies + Vector3.up * (alturaEscalon + radio);
        Vector3 arriba = pies + Vector3.up * Mathf.Max(alturaEscalon + radio, alturaCuerpo - radio);

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

        if (hay && depurar && Time.time - ultimoLog > 0.5f)
        {
            ultimoLog = Time.time;
            Debug.Log("Antonio choca con: " + cercano.collider.name, cercano.collider);
        }
        return hay;
    }

    void OnDisable()
    {
        puedeMoverse = true;
        velocidadVertical = 0f;
        AudioJuego.Pasos(false);
    }
}