using UnityEngine;

/// <summary>
/// Clase base: todo lo que Antonio pueda usar con la tecla E hereda de aquí.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    public string prompt = "Interactuar";

    public virtual string GetPrompt() => prompt;
    public abstract void Interact();
}
