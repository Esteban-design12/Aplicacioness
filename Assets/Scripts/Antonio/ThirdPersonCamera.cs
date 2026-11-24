using UnityEngine;

/// <summary>
/// Cámara en tercera persona para el laberinto de La Candileja.
/// - Sigue a Antonio desde atrás, baja y cerca (estilo terror).
/// - No puede subir por encima de los muros (límite de inclinación y de altura).
/// - Si hay un muro entre Antonio y la cámara, se acerca para no atravesarlo.
/// Va en la Main Camera (NO como hija de Antonio).
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Antonio. Si está vacío, se busca solo (el objeto con el script Movimiento).")]
    public Transform target;
    [Tooltip("Altura del punto que mira la cámara (≈ hombros). 0 = calcularla según el tamaño de Antonio.")]
    public float pivotHeight = 0f;

    [Header("Posición")]
    [Tooltip("Distancia detrás de Antonio.")]
    public float distance = 2.6f;
    [Tooltip("Desplazamiento hacia el hombro derecho.")]
    public float shoulderOffset = 0.35f;

    [Header("Rotación con el mouse")]
    public float mouseSensitivity = 2f;
    [Tooltip("Inclinación inicial (grados hacia abajo).")]
    public float startPitch = 12f;
    [Tooltip("Cuánto puede mirar hacia arriba.")]
    public float minPitch = -10f;
    [Tooltip("Cuánto puede mirar hacia abajo. Bajo = no se ve por encima de los muros.")]
    public float maxPitch = 28f;

    [Header("No ver por encima de los muros")]
    [Tooltip("Altura máxima de la cámara sobre los pies de Antonio (metros).")]
    public float maxHeightAboveFeet = 2.4f;

    [Header("Colisión con paredes")]
    public float collisionRadius = 0.25f;
    public LayerMask collisionMask = ~0;
    [Tooltip("Qué tan rápido vuelve a alejarse después de acercarse por un muro.")]
    public float returnSpeed = 4f;

    [Header("Suavizado")]
    public float followSmooth = 0.06f;

    /// <summary>Dirección horizontal de la cámara (la usa Movimiento).</summary>
    public Quaternion PlanarRotation => Quaternion.Euler(0f, yaw, 0f);

    float yaw, pitch;
    float currentDistance;
    Vector3 smoothedPivot, pivotVelocity;
    Collider[] targetColliders = new Collider[0];
    InventoryUI inventario;
    readonly RaycastHit[] hits = new RaycastHit[16];

    void Start()
    {
        if (!target)
        {
            var player = FindFirstObjectByType<Movimiento>();
            if (player) target = player.transform;
        }
        if (!target)
        {
            Debug.LogWarning("ThirdPersonCamera: no encontré a Antonio (script Movimiento).", this);
            enabled = false;
            return;
        }

        targetColliders = target.GetComponentsInChildren<Collider>();
        inventario = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);

        if (pivotHeight <= 0f)
        {
            // ~85 % de la estatura de Antonio = altura de los hombros
            float h = 1.7f;
            foreach (var c in targetColliders)
                if (!c.isTrigger) { h = c.bounds.size.y; break; }
            if (h < 0.5f || h > 3f) h = 1.7f; // colisionador con medidas irreales
            pivotHeight = Mathf.Clamp(h * 0.85f, 0.8f, 2.2f);
        }

        var cam = GetComponent<Camera>();
        if (cam) cam.nearClipPlane = 0.08f; // evita ver "a través" de muros muy cercanos

        yaw = target.eulerAngles.y;
        pitch = startPitch;
        currentDistance = distance;
        smoothedPivot = Pivot();
        Place(true);
    }

    void LateUpdate()
    {
        if (!target) return;

        // Gira siempre que se esté jugando (sin pausa ni inventario abierto).
        // Además asegura que el cursor quede bloqueado: así no hace falta pulsar Tab al empezar.
        if (PuedeGirar())
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Place(false);
    }

    bool PuedeGirar()
    {
        if (Time.timeScale == 0f) return false;                 // pausa u opciones
        if (inventario && inventario.IsOpen) return false;      // inventario abierto
        if (!Application.isFocused) return false;               // ventana sin foco
        return true;
    }

    Vector3 Pivot()
    {
        return target.position + Vector3.up * pivotHeight
               + PlanarRotation * new Vector3(shoulderOffset, 0f, 0f);
    }

    void Place(bool instant)
    {
        Vector3 head = target.position + Vector3.up * pivotHeight;
        Vector3 pivot = Pivot();

        // Que el desplazamiento al hombro tampoco atraviese un muro
        Vector3 side = pivot - head;
        if (side.sqrMagnitude > 0.0001f &&
            Cast(head, side.normalized, side.magnitude, out float sideHit))
            pivot = head + side.normalized * Mathf.Max(0f, sideHit - 0.05f);

        smoothedPivot = instant ? pivot
            : Vector3.SmoothDamp(smoothedPivot, pivot, ref pivotVelocity, followSmooth);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = -(rotation * Vector3.forward);

        // Si hay un muro detrás, la cámara se acerca de inmediato; luego vuelve suave
        float wanted = distance;
        if (Cast(smoothedPivot, back, distance, out float hitDist))
            wanted = Mathf.Max(0.2f, hitDist - 0.05f);

        currentDistance = (wanted < currentDistance || instant)
            ? wanted
            : Mathf.MoveTowards(currentDistance, wanted, returnSpeed * Time.deltaTime);

        Vector3 pos = smoothedPivot + back * currentDistance;

        // Nunca más alto que el límite: así no se asoma por encima de los muros
        float maxY = target.position.y + maxHeightAboveFeet;
        bool limitada = pos.y > maxY;
        if (limitada) pos.y = maxY;

        transform.position = pos;
        // Si se limitó la altura, mirar a Antonio; si no, usar la rotación del mouse
        transform.rotation = limitada
            ? Quaternion.LookRotation(smoothedPivot - pos, Vector3.up)
            : rotation;
    }

    /// <summary>SphereCast que ignora a Antonio y a los triggers.</summary>
    bool Cast(Vector3 from, Vector3 dir, float maxDist, out float nearest)
    {
        nearest = maxDist;
        bool found = false;
        int n = Physics.SphereCastNonAlloc(from, collisionRadius, dir, hits, maxDist,
                                           collisionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (IsTarget(h.collider)) continue;
            if (h.distance <= 0f && h.point == Vector3.zero) continue; // ya superpuesto al empezar
            if (h.distance < nearest) { nearest = h.distance; found = true; }
        }
        return found;
    }

    bool IsTarget(Collider c)
    {
        if (c.transform.IsChildOf(target)) return true;
        foreach (var t in targetColliders) if (t == c) return true;
        return false;
    }
}
