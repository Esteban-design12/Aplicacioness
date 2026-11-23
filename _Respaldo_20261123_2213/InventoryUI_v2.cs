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
    public TMP_Text descriptionText;
    public KeyCode toggleKey = KeyCode.Tab;

    [Tooltip("Botón que cierra el inventario. Si está vacío busca un hijo llamado 'Boton_VolverInve'.")]
    public Button closeButton;

    [Header("Opcional")]
    [Tooltip("Arrastra aquí tu script de movimiento/cámara para pausarlo mientras el inventario está abierto.")]
    public Behaviour[] scriptsToDisableWhileOpen;

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
            if (filled) desc += $"<b>{items[i].displayName}</b>: {items[i].description}\n";
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
