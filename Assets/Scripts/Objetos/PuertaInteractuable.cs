using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class PuertaInteractuable : Interactable
{
    [Header("Bisagra (coordenadas del padre)")]
    [SerializeField] Vector3 puntoBisagra;
    [SerializeField] Vector3 ejeBisagra = Vector3.up;

    [Header("Apertura")]
    [SerializeField] float anguloApertura = -95f;
    [SerializeField, Min(0.05f)] float duracion = 0.65f;

    Vector3 posicionCerrada;
    Quaternion rotacionCerrada;
    float avance;
    bool abierta;

    public bool EstaAbierta => abierta;
    public bool EstaMoviendose => avance != (abierta ? 1f : 0f);

    void Awake()
    {
        posicionCerrada = transform.localPosition;
        rotacionCerrada = transform.localRotation;
        ejeBisagra = ejeBisagra.sqrMagnitude > 0.001f ? ejeBisagra.normalized : Vector3.up;
    }

    public override string GetPrompt() => abierta ? "Cerrar puerta" : "Abrir puerta";

    public override void Interact()
    {
        abierta = !abierta;
    }

    void Update()
    {
        if (!EstaMoviendose) return;

        avance = Mathf.MoveTowards(avance, abierta ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.05f, duracion));
        Quaternion giro = Quaternion.AngleAxis(
            anguloApertura * Mathf.SmoothStep(0f, 1f, avance), ejeBisagra);

        transform.SetLocalPositionAndRotation(
            puntoBisagra + giro * (posicionCerrada - puntoBisagra),
            giro * rotacionCerrada);
    }
}
