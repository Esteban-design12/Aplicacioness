using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de inventario (los 3 recuerdos). Se abre/cierra con Tab.
/// Va en el Canvas del inventario: al cerrarse se esconde TODO ese Canvas
/// (fondo, título, botón Volver y panel), así no tapa el juego.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public GameObject panel;
    public Image[] slotIcons = new Image[3];
    public TMP_Text[] slotNames = new TMP_Text[3];
    [Tooltip("Texto viejo con todas las descripciones juntas. Se oculta: ahora cada casilla tiene la suya.")]
    public TMP_Text descriptionText;
    public KeyCode toggleKey = KeyCode.Tab;

    [Tooltip("Botón que cierra el inventario. Si está vacío busca un hijo llamado 'Boton_VolverInve'.")]
    public Button closeButton;

    [Header("Opcional")]
    [Tooltip("Arrastra aquí tu script de movimiento/cámara para pausarlo mientras el inventario está abierto.")]
    public Behaviour[] scriptsToDisableWhileOpen;

    [Header("Organización de cada casilla")]
    [Tooltip("Ubica el nombre debajo del marco y la descripción debajo del nombre.")]
    public bool autoLayout = true;
    [Tooltip("Distancia desde el centro de la imagen hasta el nombre.")]
    public float nameOffsetY = -100f;
    [Tooltip("Ancho de los textos de cada casilla.")]
    public float textWidth = 220f;
    public float nameFontSize = 22f;
    public float descriptionFontSize = 15f;
    public float descriptionHeight = 70f;

    TMP_Text[] slotDescriptions;
    Canvas canvas;
    bool isOpen;

    public bool IsOpen => isOpen;

    void Awake()
    {
        canvas = GetComponent<Canvas>();

        if (closeButton == null)
        {
            var t = FindChild(transform, "Boton_VolverInve");
            if (t) closeButton = t.GetComponent<Button>();
        }
        if (closeButton) closeButton.onClick.AddListener(Close);

        if (autoLayout) OrganizarCasillas();
    }

    /// <summary>Nombre debajo del marco y descripción debajo del nombre, en cada casilla.</summary>
    void OrganizarCasillas()
    {
        slotDescriptions = new TMP_Text[slotIcons.Length];

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (!slotIcons[i] || i >= slotNames.Length || !slotNames[i]) continue;

            var icon = slotIcons[i].rectTransform;
            var name = slotNames[i];
            var nameRect = name.rectTransform;

            // El nombre va en el mismo padre que la imagen, centrado debajo de ella
            nameRect.SetParent(icon.parent, false);
            nameRect.anchorMin = nameRect.anchorMax = icon.anchorMin;
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.sizeDelta = new Vector2(textWidth, 32f);
            nameRect.anchoredPosition = icon.anchoredPosition + new Vector2(0f, nameOffsetY);
            name.alignment = TextAlignmentOptions.Center;
            name.fontSize = nameFontSize;
            name.fontStyle = FontStyles.Bold;
            name.enableAutoSizing = true;
            name.fontSizeMin = 12f;
            name.fontSizeMax = nameFontSize;

            // La descripción: copia del nombre, justo debajo
            var desc = Instantiate(name, icon.parent);
            desc.name = "Descripcion" + (i + 1);
            var descRect = desc.rectTransform;
            descRect.anchorMin = descRect.anchorMax = icon.anchorMin;
            descRect.pivot = new Vector2(0.5f, 1f);
            descRect.sizeDelta = new Vector2(textWidth, descriptionHeight);
            descRect.anchoredPosition = nameRect.anchoredPosition + new Vector2(0f, -18f);
            desc.alignment = TextAlignmentOptions.Top;
            desc.fontStyle = FontStyles.Normal;
            desc.fontSize = descriptionFontSize;
            desc.fontSizeMin = 10f;
            desc.fontSizeMax = descriptionFontSize;
            desc.text = "";
            slotDescriptions[i] = desc;
        }

        // El texto viejo (todas las descripciones juntas) ya no se usa
        if (descriptionText) descriptionText.gameObject.SetActive(false);
    }

    void Start()
    {
        if (Inventory.Instance != null) Inventory.Instance.Changed += Refresh;

        // El juego arranca con el inventario cerrado
        Show(false);
        Refresh();
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null) Inventory.Instance.Changed -= Refresh;
    }

    void Update()
    {
        // No abrir el inventario mientras el juego está en pausa
        if (!isOpen && Time.timeScale == 0f) return;

        if (Input.GetKeyDown(toggleKey))
            SetOpen(!isOpen);
    }

    /// <summary>Abre o cierra el inventario (también lo puede llamar un botón).</summary>
    public void SetOpen(bool open)
    {
        Show(open);

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        if (scriptsToDisableWhileOpen != null)
            foreach (var script in scriptsToDisableWhileOpen)
                if (script) script.enabled = !open;
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    void Show(bool open)
    {
        isOpen = open;
        if (canvas) canvas.enabled = open;   // fondo, título, botón...
        if (panel) panel.SetActive(open);    // los recuerdos
        if (open) Refresh();
    }

    void Refresh()
    {
        if (Inventory.Instance == null) return;

        var items = Inventory.Instance.Items;
        string desc = "";

        for (int i = 0; i < slotIcons.Length; i++)
        {
            bool filled = i < items.Count;
            if (slotIcons[i])
            {
                // Sin imagen asignada Unity muestra un cuadro blanco: mejor ocultarlo
                slotIcons[i].sprite = filled ? items[i].icon : null;
                slotIcons[i].enabled = filled && items[i].icon != null;
                slotIcons[i].preserveAspect = true; // que no se vea estirada
            }
            if (i < slotNames.Length && slotNames[i])
                slotNames[i].text = filled ? items[i].displayName : "—";
            if (slotDescriptions != null && i < slotDescriptions.Length && slotDescriptions[i])
                slotDescriptions[i].text = filled ? items[i].description : "";
            if (filled && !string.IsNullOrEmpty(items[i].description))
                desc += $"<b>{items[i].displayName}</b>: {items[i].description}\n";
        }

        if (descriptionText) descriptionText.text = desc;
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found) return found;
        }
        return null;
    }
}
