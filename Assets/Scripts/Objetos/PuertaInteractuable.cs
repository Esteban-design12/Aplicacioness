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

    [Header("Puzle de recuerdos (opcional)")]
    [Tooltip("Si está activo, la puerta solo se abre cuando Antonio tiene todos los recuerdos.")]
    [SerializeField] bool requiereTodosLosRecuerdos = false;
    [Tooltip("{0} = recuerdos que tiene, {1} = total")]
    [SerializeField] string mensajeBloqueada = "Está cerrada con llave... Siento que me faltan recuerdos ({0}/{1}).";
    [SerializeField] string mensajeDesbloqueada = "Con todos los recuerdos, la puerta por fin cede...";
    bool desbloqueada;

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

    public override string GetPrompt()
    {
        if (abierta) return "Cerrar puerta";
        return Bloqueada ? "Puerta cerrada con llave" : "Abrir puerta";
    }

    int RecuerdosActuales => Inventory.Instance != null ? Inventory.Instance.Items.Count : 0;
    int RecuerdosTotales => Inventory.Instance != null ? Inventory.Instance.maxSlots : 3;

    bool Bloqueada => requiereTodosLosRecuerdos && !desbloqueada && RecuerdosActuales < RecuerdosTotales;

    public override void Interact()
    {
        if (!abierta && Bloqueada)
        {
            InteractionSystem.ShowMessage(string.Format(mensajeBloqueada, RecuerdosActuales, RecuerdosTotales));
            if (AudioJuego.Sonidos) AudioJuego.Efecto3D(AudioJuego.Sonidos.puzleIncorrecto, transform.position, 1f, 15f);
            return;
        }

        // Primera vez que se abre con los recuerdos: se desbloquea para siempre
        if (requiereTodosLosRecuerdos && !desbloqueada)
        {
            desbloqueada = true;
            if (!string.IsNullOrEmpty(mensajeDesbloqueada)) InteractionSystem.ShowMessage(mensajeDesbloqueada, 4f);
            if (AudioJuego.Sonidos) AudioJuego.Efecto3D(AudioJuego.Sonidos.puertaDesbloqueada, transform.position, 1f, 20f);
        }

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
