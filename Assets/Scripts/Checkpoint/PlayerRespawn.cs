using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Va en ANTONIO (el objeto Player). Guarda el último checkpoint y lo devuelve ahí.
/// Si todavía no ha tocado ningún checkpoint, usa su posición inicial.
/// Funciona con CharacterController o con Rigidbody.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    [Header("Checkpoint inicial (opcional)")]
    [Tooltip("Déjalo vacío para usar la posición donde Antonio empieza la escena.")]
    public Transform startCheckpoint;

    [Header("Eventos")]
    [Tooltip("Sonido, fundido a negro, etc. Se ejecuta al volver al checkpoint.")]
    public UnityEvent onRespawn;

    [Header("Mensaje")]
    public string respawnMessage = "Volviste al último punto seguro.";

    CharacterController controller;
    Rigidbody body;
    Vector3 checkpointPosition;
    Quaternion checkpointRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        body = GetComponent<Rigidbody>();

        if (startCheckpoint != null)
            SetCheckpoint(startCheckpoint);
        else
        {
            checkpointPosition = transform.position;
            checkpointRotation = transform.rotation;
        }
    }

    /// <summary>Guarda un nuevo checkpoint (lo llama el script Checkpoint).</summary>
    public void SetCheckpoint(Transform point)
    {
        checkpointPosition = point.position;
        checkpointRotation = point.rotation;
    }

    /// <summary>Teletransporta a Antonio al último checkpoint.</summary>
    public void Respawn()
    {
        // El CharacterController pisa la posición si no se desactiva antes de moverlo
        if (controller) controller.enabled = false;

        transform.SetPositionAndRotation(checkpointPosition, checkpointRotation);

        if (body)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        if (controller) controller.enabled = true;
        Physics.SyncTransforms();

        if (!string.IsNullOrEmpty(respawnMessage))
            InteractionSystem.ShowMessage(respawnMessage);

        onRespawn?.Invoke();
    }
}
