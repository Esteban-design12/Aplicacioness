using UnityEngine;

/// <summary>
/// Muestra las alertas del HUD:
///  - PELIGRO: cuando la Candileja detecta a Antonio (lo persigue o lo asusta).
///  - PISTA:   cuando Antonio está cerca de un recuerdo para recoger o de una antorcha apagada.
/// Las imágenes de cada grupo (blanco, ámbar, naranja, rojo) se alternan como un parpadeo.
/// Va en Canvas_HUD.
/// </summary>
[AddComponentMenu("Juego/Alertas HUD")]
public class AlertasHUD : MonoBehaviour
{
    [Tooltip("Grupo con las imágenes de PELIGRO. Si está vacío busca 'Grupo_Alertas'.")]
    public Transform grupoPeligro;
    [Tooltip("Grupo con las imágenes de PISTA. Si está vacío busca 'Grupo_Pistas'.")]
    public Transform grupoPista;

    [Header("Animación")]
    [Tooltip("Cambios de color por segundo.")]
    public float velocidadPeligro = 8f;
    public float velocidadPista = 4f;
    [Tooltip("Cuánto 'late' la alerta (0 = nada).")]
    [Range(0f, 0.3f)] public float pulso = 0.08f;

    GameObject[] framesPeligro, framesPista;
    Vector3 escalaPeligro, escalaPista;

    void Awake()
    {
        if (!grupoPeligro) grupoPeligro = Buscar(transform, "Grupo_Alertas");
        if (!grupoPista) grupoPista = Buscar(transform, "Grupo_Pistas");
        framesPeligro = Hijos(grupoPeligro);
        framesPista = Hijos(grupoPista);
        if (grupoPeligro) escalaPeligro = grupoPeligro.localScale;
        if (grupoPista) escalaPista = grupoPista.localScale;
        Mostrar(framesPeligro, -1);
        Mostrar(framesPista, -1);
    }

    void Update()
    {
        bool jugando = Time.timeScale > 0f;

        bool peligro = jugando && CandilejaIA.JugadorDetectado;
        bool pista = jugando && !peligro && HayPistaCerca(); // comparten lugar: manda Peligro

        Animar(framesPeligro, grupoPeligro, escalaPeligro, peligro, velocidadPeligro);
        Animar(framesPista, grupoPista, escalaPista, pista, velocidadPista);
    }

    static bool HayPistaCerca()
    {
        var sistema = InteractionSystem.Instance;
        if (!sistema) return false;
        var actual = sistema.Actual;
        if (!actual) return false;

        if (actual is MemoryPickup) return true;
        if (actual is AntorchaInteractuable antorcha) return !antorcha.EstaEncendida && !antorcha.EstaEncendiendo;
        return false;
    }

    void Animar(GameObject[] frames, Transform grupo, Vector3 escala, bool activa, float velocidad)
    {
        if (frames == null || frames.Length == 0) return;
        if (!activa) { Mostrar(frames, -1); return; }

        // Ida y vuelta: 0,1,2,3,2,1,0...
        int n = frames.Length;
        int ciclo = Mathf.Max(1, n * 2 - 2);
        int paso = Mathf.FloorToInt(Time.unscaledTime * velocidad) % ciclo;
        int indice = paso < n ? paso : ciclo - paso;
        Mostrar(frames, indice);

        if (grupo && pulso > 0f)
            grupo.localScale = escala * (1f + Mathf.Sin(Time.unscaledTime * velocidad) * pulso);
    }

    static void Mostrar(GameObject[] frames, int indice)
    {
        if (frames == null) return;
        for (int i = 0; i < frames.Length; i++)
            if (frames[i] && frames[i].activeSelf != (i == indice)) frames[i].SetActive(i == indice);
    }

    static GameObject[] Hijos(Transform t)
    {
        if (!t) return new GameObject[0];
        var lista = new GameObject[t.childCount];
        for (int i = 0; i < t.childCount; i++) lista[i] = t.GetChild(i).gameObject;
        return lista;
    }

    static Transform Buscar(Transform padre, string nombre)
    {
        foreach (Transform hijo in padre)
        {
            if (hijo.name == nombre) return hijo;
            var r = Buscar(hijo, nombre);
            if (r) return r;
        }
        return null;
    }
}
