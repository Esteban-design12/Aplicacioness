using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Juego/Objetos/Antorcha interactuable")]
public class AntorchaInteractuable : Interactable
{
    [Header("Referencias")]
    [SerializeField] Transform llama;
    [SerializeField] Renderer cuerpoRenderer;
    [SerializeField] Renderer llamaRenderer;
    [SerializeField] Light luzFuego;

    [Header("Encendido")]
    [SerializeField, Min(0.01f)] float duracionEncendido = 0.55f;
    [SerializeField, Min(0f)] float intensidadLuz = 1.6f;
    [SerializeField, ColorUsage(false, true)] Color colorEmision = new Color(1.8f, 1.25f, 0.65f);

    [Header("Movimiento de la llama")]
    [SerializeField, Range(0f, 0.2f)] float variacionTamano = 0.055f;
    [SerializeField, Range(0f, 10f)] float inclinacion = 2.5f;

    [Header("Minijuego")]
    public UnityEvent alEncender = new UnityEvent();

    [Header("Bloqueo (lo usa el puzle de antorchas)")]
    [Tooltip("Si está bloqueada no se puede encender todavía.")]
    public bool bloqueada = false;
    public string mensajeBloqueada = "No prende... quizá falte encender otras antorchas.";

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    MaterialPropertyBlock propiedadesCuerpo;
    MaterialPropertyBlock propiedadesLlama;
    Vector3 escalaOriginal;
    Quaternion rotacionOriginal;
    float tiempoEncendido;
    bool encendiendo;
    bool encendida;
    AudioSource sonidoFuego;

    public bool EstaEncendida => encendida;
    public bool EstaEncendiendo => encendiendo;

    void Reset()
    {
        prompt = "Encender antorcha";
        animacionAgacharse = false;
    }

    void Awake()
    {
        if (llama == null || cuerpoRenderer == null || llamaRenderer == null || luzFuego == null)
        {
            Debug.LogError("Faltan referencias en la antorcha interactuable.", this);
            enabled = false;
            return;
        }

        escalaOriginal = llama.localScale;
        rotacionOriginal = llama.localRotation;
        propiedadesCuerpo = new MaterialPropertyBlock();
        propiedadesLlama = new MaterialPropertyBlock();
        Apagar();

        // Si ya se había encendido en esta partida (antes de perder), queda encendida
        if (ProgresoPartida.AntorchaEncendida(this)) EncenderYa();
    }

    /// <summary>Enciende la antorcha al instante, sin animación ni sonido de encendido.</summary>
    public void EncenderYa()
    {
        if (llama == null) return;
        encendiendo = false;
        encendida = true;
        tiempoEncendido = duracionEncendido;
        llama.localScale = escalaOriginal;
        llama.gameObject.SetActive(true);
        luzFuego.enabled = true;
        luzFuego.intensity = intensidadLuz;
        AplicarEmision(1f);
        if (AudioJuego.Sonidos && !sonidoFuego)
            sonidoFuego = AudioJuego.BucleEn(gameObject, AudioJuego.Sonidos.antorchaEncendida, 0.8f, 10f);
    }

    public override string GetPrompt()
    {
        if (encendida) return "Antorcha encendida";
        if (encendiendo) return "Encendiendo antorcha...";
        if (bloqueada) return "Antorcha (aún no prende)";
        return "Encender antorcha";
    }

    public override void Interact()
    {
        if (bloqueada && !encendida)
        {
            InteractionSystem.ShowMessage(mensajeBloqueada);
            return;
        }
        Encender();
    }

    public void Encender()
    {
        if (!isActiveAndEnabled || llama == null || propiedadesLlama == null || encendiendo || encendida)
            return;

        tiempoEncendido = 0f;
        encendiendo = true;
        llama.localScale = escalaOriginal * 0.001f;
        llama.gameObject.SetActive(true);
        luzFuego.enabled = true;

        if (AudioJuego.Sonidos)
            AudioJuego.Efecto3D(AudioJuego.Sonidos.encenderAntorcha, transform.position, 1f, 15f);
    }

    public void Apagar()
    {
        encendiendo = false;
        encendida = false;
        tiempoEncendido = 0f;

        if (llama != null)
        {
            llama.localScale = escalaOriginal;
            llama.localRotation = rotacionOriginal;
            llama.gameObject.SetActive(false);
        }

        if (luzFuego != null)
        {
            luzFuego.intensity = 0f;
            luzFuego.enabled = false;
        }

        AplicarEmision(0f);

        if (sonidoFuego) sonidoFuego.Stop();
    }

    void Update()
    {
        if (!encendiendo && !encendida) return;

        tiempoEncendido += Time.deltaTime;
        float progreso = Mathf.Clamp01(tiempoEncendido / Mathf.Max(0.01f, duracionEncendido));
        float crecimiento = Mathf.SmoothStep(0f, 1f, progreso);
        float ritmo = tiempoEncendido * 6.5f;
        float oscilacion = Mathf.Sin(ritmo) * 0.65f + Mathf.Sin(ritmo * 1.73f) * 0.35f;
        float ancho = 1f - oscilacion * variacionTamano * 0.35f;
        float alto = 1f + oscilacion * variacionTamano;

        llama.localScale = Vector3.Scale(escalaOriginal, new Vector3(ancho, alto, ancho))
                           * Mathf.Max(0.001f, crecimiento);
        llama.localRotation = rotacionOriginal * Quaternion.Euler(
            Mathf.Sin(ritmo * 0.71f) * inclinacion * crecimiento,
            0f,
            Mathf.Sin(ritmo * 0.83f + 0.4f) * inclinacion * crecimiento);

        float brillo = crecimiento * (1f + oscilacion * 0.06f);
        luzFuego.intensity = intensidadLuz * brillo;
        AplicarEmision(brillo);

        if (encendiendo && progreso >= 1f)
        {
            encendiendo = false;
            encendida = true;
            ProgresoPartida.MarcarAntorcha(this);

            // Crepitar de la antorcha encendida
            if (sonidoFuego) sonidoFuego.Play();
            else if (AudioJuego.Sonidos)
                sonidoFuego = AudioJuego.BucleEn(gameObject, AudioJuego.Sonidos.antorchaEncendida, 0.8f, 10f);

            alEncender.Invoke();
        }
    }

    void AplicarEmision(float brillo)
    {
        if (propiedadesCuerpo == null || propiedadesLlama == null) return;

        cuerpoRenderer.GetPropertyBlock(propiedadesCuerpo);
        propiedadesCuerpo.SetColor(EmissionColor, colorEmision * brillo);
        cuerpoRenderer.SetPropertyBlock(propiedadesCuerpo);

        llamaRenderer.GetPropertyBlock(propiedadesLlama);
        propiedadesLlama.SetColor(EmissionColor, colorEmision * brillo);
        llamaRenderer.SetPropertyBlock(propiedadesLlama);
    }
}
