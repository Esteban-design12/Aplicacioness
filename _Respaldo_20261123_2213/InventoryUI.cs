using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra los 3 recuerdos en pantalla. Se abre/cierra con Tab.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public GameObject panel;
    public Image[] slotIcons = new Image[3];
    public TMP_Text[] slotNames = new TMP_Text[3];
    public TMP_Text descriptionText;
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Opcional")]
    [Tooltip("Arrastra aquí tu script de movimiento/cámara para pausarlo mientras el inventario está abierto.")]
    public Behaviour[] scriptsToDisableWhileOpen;

    void Start()
    {
        Inventory.Instance.Changed += Refresh;
        panel.SetActive(false);
        Refresh();
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null) Inventory.Instance.Changed -= Refresh;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            bool open = !panel.activeSelf;
            panel.SetActive(open);

            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;

            foreach (var script in scriptsToDisableWhileOpen)
                if (script) script.enabled = !open;
        }
    }

    void Refresh()
    {
        var items = Inventory.Instance.Items;
        string desc = "";

        for (int i = 0; i < slotIcons.Length; i++)
        {
            bool filled = i < items.Count;
            slotIcons[i].enabled = filled;
            slotIcons[i].sprite = filled ? items[i].icon : null;
            if (slotNames[i]) slotNames[i].text = filled ? items[i].displayName : "—";
            if (filled) desc += $"<b>{items[i].displayName}</b>: {items[i].description}\n";
        }

        if (descriptionText) descriptionText.text = desc;
    }
}
