using UnityEngine;

/// <summary>
/// Objeto del mundo que Antonio recoge con E y se guarda como recuerdo.
/// </summary>
public class MemoryPickup : Interactable
{
    public MemoryData memory;

    void Reset() => prompt = "Recoger recuerdo";

    void Start()
    {
        // Si ya se recogió en esta partida (antes de perder), no vuelve a aparecer
        if (ProgresoPartida.TieneRecuerdo(memory)) gameObject.SetActive(false);
    }

    public override string GetPrompt() => memory ? $"Recoger: {memory.displayName}" : prompt;

    public override void Interact()
    {
        if (Inventory.Instance.Add(memory))
        {
            InteractionSystem.ShowMessage($"Recuerdo obtenido: {memory.displayName}");

            var s = AudioJuego.Sonidos;
            if (s)
            {
                AudioJuego.Efecto(s.recogerObjeto);
                if (memory.sonido) AudioJuego.EfectoRetrasado(memory.sonido, 0.3f, 0.9f);

                // ¿Ya tiene todos los recuerdos?
                if (Inventory.Instance.Items.Count >= Inventory.Instance.maxSlots)
                    AudioJuego.EfectoRetrasado(s.recuerdoCompleto, 1.5f);
            }
            gameObject.SetActive(false);
        }
        else
        {
            InteractionSystem.ShowMessage("No puedes llevar más recuerdos.");
        }
    }
}
