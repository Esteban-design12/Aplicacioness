using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class MuroFinalAnimacion : MonoBehaviour
{
    static readonly int AbrirTrigger = Animator.StringToHash("Abrir");
    static readonly int CerradoState = Animator.StringToHash("Base Layer.Cerrado");
    Animator animator;
    bool aperturaSolicitada;

    public bool AperturaSolicitada => aperturaSolicitada;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Conectar el evento de puzzle resuelto a este metodo.
    [ContextMenu("Probar apertura (solo Play)")]
    public void Abrir()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || aperturaSolicitada) return;
        if (!animator) animator = GetComponent<Animator>();
        if (!animator.runtimeAnimatorController) return;

        aperturaSolicitada = true;
        animator.SetTrigger(AbrirTrigger);
    }

    [ContextMenu("Restablecer muro (solo Play)")]
    public void Restablecer()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        if (!animator) animator = GetComponent<Animator>();
        if (!animator.runtimeAnimatorController) return;

        aperturaSolicitada = false;
        animator.ResetTrigger(AbrirTrigger);
        animator.Play(CerradoState, 0, 0f);
        animator.Update(0f);
    }
}
