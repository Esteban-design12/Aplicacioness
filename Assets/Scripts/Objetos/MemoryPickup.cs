using UnityEngine;

/// <summary>
/// Objeto del mundo que Antonio recoge con E y se guarda como recuerdo.
/// </summary>
public class MemoryPickup : Interactable
{
    public MemoryData memory;

    void Reset() => prompt = "Recoger recuerdo";

    public override string GetPrompt() => memory ? $"Recoger: {memory.displayName}" : prompt;

    public override void Interact()
    {
        if (Inventory.Instance.Add(memory))
        {
            InteractionSystem.ShowMessage($"Recuerdo obtenido: {memory.displayName}");
            gameObject.SetActive(false);
        }
        else
        {
            InteractionSystem.ShowMessage("No puedes llevar más recuerdos.");
        }
    }
}
