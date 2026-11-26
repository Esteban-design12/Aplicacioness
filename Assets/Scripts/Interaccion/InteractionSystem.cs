using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Animación")]
    [Tooltip("Nombre exacto del parámetro Trigger en el Animator.")]
    public string pickupTrigger = "PickUp";

    [Header("UI")]
    public TMP_Text promptText;   // texto fijo: "[E] Recoger..."
    public TMP_Text messageText;  // mensajes temporales
    [Tooltip("Pone los textos en un Canvas propio, siempre visible (no depende del inventario).")]
    public bool hudPropio = true;
    public float tamanoPrompt = 34f;
    public float tamanoMensaje = 30f;

    [Header("Depuración")]
    public bool debugLog = false;

    Interactable current;
    Coroutine messageRoutine;
    string lastName = "";
    Animator animator;

    Vector3 Origin => transform.position + Vector3.up * originHeight;

    void Awake()
    {
        Instance = this;
        animator = GetComponentInChildren<Animator>();
        if (hudPropio) CrearHUD();
        if (messageText) messageText.text = "";
        if (promptText) promptText.text = "";
    }

    void Update()
    {
        FindTarget();

        if (current != null && Input.GetKeyDown(interactKey))
        {
            // Dispara la animación de agacharse solo si el objeto lo pide
            if (current.animacionAgacharse && animator != null)
                animator.SetTrigger(pickupTrigger);

            current.Interact();
        }
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
        {
            // Sin texto en pausa o con el inventario/menú abierto (cursor libre)
            bool jugando = Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked;
            string texto = current != null ? current.GetPrompt() : "";
            promptText.text = jugando && !string.IsNullOrEmpty(texto) ? $"[{interactKey}] {texto}" : "";
        }
    }

    /// <summary>
    /// Canvas propio para los textos de interacción, por encima del juego y siempre activo.
    /// Reutiliza el texto asignado (para conservar la fuente) y crea el de mensajes si falta.
    /// </summary>
    void CrearHUD()
    {
        var go = new GameObject("HUD_Interaccion");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (promptText)
            promptText.rectTransform.SetParent(go.transform, false);
        else
            promptText = new GameObject("Prompt", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        promptText.rectTransform.SetParent(go.transform, false);
        Configurar(promptText, 150f, tamanoPrompt);
        promptText.gameObject.SetActive(true);
        promptText.enabled = true;

        if (!messageText)
        {
            messageText = Instantiate(promptText, go.transform);
            messageText.name = "Mensaje";
        }
        else messageText.rectTransform.SetParent(go.transform, false);
        Configurar(messageText, 230f, tamanoMensaje);
        messageText.fontStyle = FontStyles.Italic;
        messageText.gameObject.SetActive(true);
        messageText.enabled = true;
    }

    static void Configurar(TMP_Text t, float altura, float tamano)
    {
        var r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(0f, altura);
        r.sizeDelta = new Vector2(1400f, 70f);
        r.localScale = Vector3.one;
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = tamano;
        t.color = Color.white;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(0, 0, 0, 220);
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