using UnityEngine;

/// <summary>
/// Datos de un recuerdo. Crea uno por cada recuerdo:
/// clic derecho en Project > Create > Candileja > Recuerdo
/// </summary>
[CreateAssetMenu(menuName = "Candileja/Recuerdo", fileName = "Recuerdo_")]
public class MemoryData : ScriptableObject
{
    public string id;              // ej: "retrato", "rosario", "carta"
    public string displayName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;
}
