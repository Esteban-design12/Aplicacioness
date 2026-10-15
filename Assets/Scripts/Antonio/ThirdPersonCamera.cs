using UnityEngine;

/// <summary>
/// Cámara en tercera persona (sobre el hombro) con rotación por mouse
/// y colisión contra paredes. Va en la Main Camera, SIN ser hija de Antonio.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Hijo vacío de Antonio a la altura de los hombros/cabeza (≈1.6 m).")]
    public Transform target;

    [Header("Posición")]
    public float distance = 3.5f;
    [Tooltip("X = desplazamiento lateral (hombro), Y = altura extra.")]
    public Vector2 shoulderOffset = new Vector2(0.5f, 0.1f);

    [Header("Rotación")]
    public float mouseSensitivity = 2f;
    public float minPitch = -20f;
    public float maxPitch = 60f;

    [Header("Suavizado y colisión")]
    public float smoothTime = 0.04f;
    public float collisionRadius = 0.25f;
    [Tooltip("Excluye la capa de Antonio para que la cámara no choque con él.")]
    public LayerMask collisionMask = ~0;

    /// <summary>Dirección horizontal de la cámara: úsala en tu movimiento.</summary>
    public Quaternion PlanarRotation => Quaternion.Euler(0f, yaw, 0f);

    float yaw, pitch;
    Vector3 velocity;

    void Start()
    {
        if (target) yaw = target.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Si el cursor está libre (inventario/menú abierto), la cámara no gira.
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Punto de pivote: el objetivo desplazado hacia el hombro
        Vector3 pivot = target.position
                        + PlanarRotation * new Vector3(shoulderOffset.x, shoulderOffset.y, 0f);

        // Posición deseada detrás del pivote
        Vector3 direction = -(rotation * Vector3.forward);
        float finalDistance = distance;

        // Si hay una pared en medio, acerca la cámara
        if (Physics.SphereCast(pivot, collisionRadius, direction, out RaycastHit hit,
                distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            finalDistance = Mathf.Max(0.3f, hit.distance);
        }

        Vector3 desired = pivot + direction * finalDistance;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        transform.rotation = rotation;
    }
}
