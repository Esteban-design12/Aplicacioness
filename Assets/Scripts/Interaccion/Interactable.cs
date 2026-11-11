using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string prompt = "Interactuar";

    [Tooltip("Si está activo, Antonio hace la animación de agacharse al interactuar.")]
    public bool animacionAgacharse = false;

    public virtual string GetPrompt() => prompt;
    public abstract void Interact();
}