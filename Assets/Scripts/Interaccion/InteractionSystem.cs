using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Va en ANTONIO (el objeto Player), no en la cámara.
/// Detecta los objetos interactuables cercanos a él (en un radio) y, si hay
/// uno al frente, muestra "[E] ..." y lo activa al pulsar E.
/// No depende de la cámara para nada.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    public static InteractionSystem Instance { get; private set; }

    [Header("Interacción")]
    [Tooltip("Radio de alcance alrededor de Antonio, en metros.")]
    public float interactRange = 2f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactMask = ~0;

    [Tooltip("Altura desde los pies de Antonio desde donde se mide (≈ pecho).")]
    public float originHeight = 1f;

    [Tooltip("Ángulo total frente a Antonio en el que se detectan objetos. 0 = a su alrededor, sin importar hacia dónde mire.")]
    [Range(0f, 360f)] public float facingAngle = 140f;

    [Header("UI")]
    public TMP_Text promptText;   // texto fijo: "[E] Recoger..."
    public TMP_Text messageText;  // mensajes temporales

    [Header("Depuración")]
    public bool debugLog = false;

    Interactable current;
    Coroutine messageRoutine;
    string lastName = "";

    Vector3 Origin => transform.position + Vector3.up * originHeight;

    void Awake()
    {
        Instance = this;
        if (messageText) messageText.text = "";
    }

    void Update()
    {
        FindTarget();

        if (current != null && Input.GetKeyDown(interactKey))
            current.Interact();
    }

    void FindTarget()
    {
        current = null;
        Vector3 origin = Origin;
        float best = float.MaxValue;

        Collider[] hits = Physics.OverlapSphere(origin, interactRange, interactMask,
            QueryTriggerInteraction.Collide);

        foreach (var col in hits)
        {
            var candidate = col.GetComponentInParent<Interactable>();
            if (candidate == null || !candidate.isActiveAndEnabled) continue;

            Vector3 toObject = col.bounds.ClosestPoint(origin) - origin;
            float distance = toObject.magnitude;

            // Filtro de frente (ignora la altura)
            if (facingAngle > 0f && facingAngle < 360f)
            {
                Vector3 flat = new Vector3(toObject.x, 0f, toObject.z);
                if (flat.sqrMagnitude > 0.01f &&
                    Vector3.Angle(transform.forward, flat) > facingAngle * 0.5f)
                    continue;
            }

            if (distance < best)
            {
                best = distance;
                current = candidate;
            }
        }

        if (debugLog)
        {
            string name = current ? current.name : "";
            if (name != lastName)
            {
                lastName = name;
                Debug.Log(current ? $"[Interacción] Objetivo: {name}" : "[Interacción] Sin objetivo");
            }
        }

        if (promptText)
            promptText.text = current != null ? $"[{interactKey}] {current.GetPrompt()}" : "";
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Origin, interactRange);
    }

    /// <summary>Muestra un mensaje en pantalla unos segundos.</summary>
    public static void ShowMessage(string text, float seconds = 3f)
    {
        if (Instance == null || Instance.messageText == null)
        {
            Debug.Log(text);
            return;
        }
        if (Instance.messageRoutine != null) Instance.StopCoroutine(Instance.messageRoutine);
        Instance.messageRoutine = Instance.StartCoroutine(Instance.MessageCoroutine(text, seconds));
    }

    IEnumerator MessageCoroutine(string text, float seconds)
    {
        messageText.text = "";
        messageText.text = text;
        yield return new WaitForSeconds(seconds);
        messageText.text = "";
    }
}
