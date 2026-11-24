using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sistema de sonido de La Candileja. Se crea SOLO al iniciar el juego
/// (no hay que ponerlo en ninguna escena) y sobrevive entre escenas.
///
/// - Música de menús, ambiente del bosque, persecución y finales.
/// - Volumen General / Música / Efectos (se guardan entre partidas).
/// - Desde cualquier script:  AudioJuego.Efecto(AudioJuego.Sonidos.puzleCorrecto);
/// </summary>
public class AudioJuego : MonoBehaviour
{
    public static AudioJuego Instance { get; private set; }
    public static BibliotecaSonidos Sonidos { get; private set; }

    // ---------- Volúmenes (0..1), guardados en PlayerPrefs ----------
    public static float VolGeneral { get; private set; } = 1f;
    public static float VolMusica { get; private set; } = 0.8f;
    public static float VolEfectos { get; private set; } = 1f;
    public static bool SonidoActivado { get; private set; } = true;

    const string kGeneral = "vol_general", kMusica = "vol_musica",
                 kEfectos = "vol_efectos", kActivado = "sonido_activado";

    const string EscenaJuego = "Candileja";

    AudioSource musica, persecucion, respiracion, pasos, efectos2D;
    AudioSource[] ambiente;
    Coroutine aleatorios, finPersecucion, fadeMusica;
    bool enPersecucion;
    float volMusicaActual = 1f;   // multiplicador de fundido
    float volPersecucion = 0f;

    // ------------------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (Instance) return;
        var go = new GameObject("AudioJuego");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioJuego>();
    }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Sonidos = Resources.Load<BibliotecaSonidos>("BibliotecaSonidos");
        if (!Sonidos)
        {
            Debug.LogWarning("AudioJuego: no encontré Assets/Resources/BibliotecaSonidos.");
            Sonidos = ScriptableObject.CreateInstance<BibliotecaSonidos>();
        }

        VolGeneral = PlayerPrefs.GetFloat(kGeneral, 1f);
        VolMusica = PlayerPrefs.GetFloat(kMusica, 0.8f);
        VolEfectos = PlayerPrefs.GetFloat(kEfectos, 1f);
        SonidoActivado = PlayerPrefs.GetInt(kActivado, 1) == 1;

        musica = NuevaFuente(true);
        persecucion = NuevaFuente(true);
        respiracion = NuevaFuente(true);
        pasos = NuevaFuente(true);
        efectos2D = NuevaFuente(false);
        ambiente = new[] { NuevaFuente(true), NuevaFuente(true), NuevaFuente(true) };

        AplicarVolumenes();
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= AlCargarEscena;
    }

    AudioSource NuevaFuente(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f; // 2D
        return s;
    }

    void Update()
    {
        // Volúmenes en vivo (sliders, fundidos)
        float m = VolMusica;
        musica.volume = m * volMusicaActual;
        persecucion.volume = m * volPersecucion;
        respiracion.volume = VolEfectos * volPersecucion * 0.8f;
        foreach (var a in ambiente) a.volume = m * Sonidos.volumenAmbiente;
        pasos.volume = VolEfectos * Sonidos.volumenPasos;
        efectos2D.volume = VolEfectos;

        // En pausa no suenan pasos
        if (Time.timeScale == 0f && pasos.isPlaying) pasos.Pause();
    }

    // ============================================================
    //  ESCENAS
    // ============================================================
    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name == "Opciones") ConectarOpciones(escena);
        if (modo == LoadSceneMode.Additive) return; // Opciones encima del juego

        Pasos(false);
        DetenerPersecucion(true);

        switch (escena.name)
        {
            case EscenaJuego:
                Musica(null);
                IniciarAmbiente();
                break;

            case "Lose":
            case "Win":
                DetenerAmbiente();
                // Dejar que termine el sonido de captura antes de la música
                Musica(Sonidos.musicaFinal, escena.name == "Lose" ? 2.5f : 0f);
                break;

            default: // menús
                DetenerAmbiente();
                Musica(Sonidos.musicaMenu);
                break;
        }
    }

    // ============================================================
    //  MÚSICA
    // ============================================================
    void Musica(AudioClip clip, float retraso = 0f)
    {
        if (clip && musica.clip == clip && musica.isPlaying) return; // sigue sin cortarse
        if (fadeMusica != null) StopCoroutine(fadeMusica);
        fadeMusica = StartCoroutine(CambiarMusica(clip, retraso));
    }

    IEnumerator CambiarMusica(AudioClip clip, float retraso)
    {
        // Bajar la actual
        while (musica.isPlaying && volMusicaActual > 0f)
        {
            volMusicaActual = Mathf.MoveTowards(volMusicaActual, 0f, Time.unscaledDeltaTime * 2f);
            yield return null;
        }
        musica.Stop();
        if (!clip) yield break;

        if (retraso > 0f) yield return new WaitForSecondsRealtime(retraso);

        musica.clip = clip;
        musica.Play();
        while (volMusicaActual < 1f)
        {
            volMusicaActual = Mathf.MoveTowards(volMusicaActual, 1f, Time.unscaledDeltaTime * 0.7f);
            yield return null;
        }
    }

    // ============================================================
    //  AMBIENTE DEL BOSQUE
    // ============================================================
    void IniciarAmbiente()
    {
        var clips = new[] { Sonidos.ambienteGeneral, Sonidos.grillos, Sonidos.viento };
        for (int i = 0; i < ambiente.Length; i++)
        {
            ambiente[i].clip = clips[i];
            if (clips[i])
            {
                ambiente[i].time = Random.Range(0f, clips[i].length * 0.9f); // que no empiecen igual
                ambiente[i].Play();
            }
        }
        if (aleatorios != null) StopCoroutine(aleatorios);
        aleatorios = StartCoroutine(SonidosAleatorios());
    }

    void DetenerAmbiente()
    {
        foreach (var a in ambiente) a.Stop();
        if (aleatorios != null) { StopCoroutine(aleatorios); aleatorios = null; }
    }

    IEnumerator SonidosAleatorios()
    {
        while (true)
        {
            var r = Sonidos.segundosEntreAleatorios;
            yield return new WaitForSeconds(Random.Range(r.x, Mathf.Max(r.x, r.y)));

            var lista = Sonidos.sonidosAleatorios;
            if (lista == null || lista.Length == 0) continue;
            var clip = lista[Random.Range(0, lista.Length)];

            // En algún punto alrededor del que escucha (la cámara)
            var oyente = Camera.main ? Camera.main.transform : transform;
            Vector2 c = Random.insideUnitCircle.normalized * Random.Range(6f, 14f);
            Vector3 pos = oyente.position + new Vector3(c.x, Random.Range(0f, 4f), c.y);
            Efecto3D(clip, pos, Sonidos.volumenAleatorios, 25f);
        }
    }

    // ============================================================
    //  PERSECUCIÓN  (la llama CandilejaIA)
    // ============================================================
    /// <summary>true cuando la Candileja ve a Antonio, false cuando lo pierde.</summary>
    public static void Persecucion(bool activa)
    {
        if (!Instance) return;
        Instance.CambiarPersecucion(activa);
    }

    void CambiarPersecucion(bool activa)
    {
        if (activa)
        {
            if (finPersecucion != null) { StopCoroutine(finPersecucion); finPersecucion = null; }
            if (enPersecucion) return;
            enPersecucion = true;

            Efecto(Sonidos.jugadorDetectado);
            if (Sonidos.persecucion && !persecucion.isPlaying)
            { persecucion.clip = Sonidos.persecucion; persecucion.Play(); }
            if (Sonidos.respiracionAgitada && !respiracion.isPlaying)
            { respiracion.clip = Sonidos.respiracionAgitada; respiracion.Play(); }
            StartCoroutine(Fundir(1f, 1.5f));
        }
        else if (enPersecucion && finPersecucion == null)
        {
            // Esperar un poco: si la vuelve a ver enseguida, no se corta
            finPersecucion = StartCoroutine(TerminarPersecucion());
        }
    }

    IEnumerator TerminarPersecucion()
    {
        yield return new WaitForSeconds(3f);
        finPersecucion = null;
        DetenerPersecucion(false);
    }

    void DetenerPersecucion(bool inmediato)
    {
        enPersecucion = false;
        if (finPersecucion != null) { StopCoroutine(finPersecucion); finPersecucion = null; }
        if (inmediato)
        {
            volPersecucion = 0f;
            persecucion.Stop();
            respiracion.Stop();
        }
        else StartCoroutine(Fundir(0f, 0.5f));
    }

    IEnumerator Fundir(float destino, float velocidad)
    {
        while (!Mathf.Approximately(volPersecucion, destino))
        {
            // Si cambió de estado a mitad del fundido, este fundido se abandona
            if (destino > 0f != enPersecucion) yield break;
            volPersecucion = Mathf.MoveTowards(volPersecucion, destino, Time.unscaledDeltaTime * velocidad);
            yield return null;
        }
        if (destino == 0f) { persecucion.Stop(); respiracion.Stop(); }
    }

    // ============================================================
    //  PASOS  (los llama Movimiento)
    // ============================================================
    public static void Pasos(bool caminando)
    {
        if (!Instance || !Sonidos || !Sonidos.pasos) return;
        var p = Instance.pasos;
        if (caminando)
        {
            if (p.clip != Sonidos.pasos) p.clip = Sonidos.pasos;
            if (!p.isPlaying) p.Play();   // continúa donde iba
        }
        else if (p.isPlaying) p.Pause();
    }

    // ============================================================
    //  EFECTOS  (para cualquier script)
    // ============================================================
    /// <summary>Efecto sin posición (se oye igual en todas partes).</summary>
    public static void Efecto(AudioClip clip, float volumen = 1f)
    {
        if (!Instance || !clip) return;
        Instance.efectos2D.PlayOneShot(clip, volumen);
    }

    /// <summary>Efecto en un punto del mundo (más fuerte cuanto más cerca).</summary>
    public static void Efecto3D(AudioClip clip, Vector3 posicion, float volumen = 1f, float distanciaMax = 20f)
    {
        if (!clip) return;
        var go = new GameObject("Efecto_" + clip.name);
        go.transform.position = posicion;
        var s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 1f;
        s.maxDistance = distanciaMax;
        s.volume = volumen * VolEfectos;
        s.Play();
        Destroy(go, clip.length + 0.1f);
    }

    /// <summary>Efecto después de unos segundos.</summary>
    public static void EfectoRetrasado(AudioClip clip, float segundos, float volumen = 1f)
    {
        if (!Instance || !clip) return;
        Instance.StartCoroutine(Instance.Retrasar(clip, segundos, volumen));
    }

    IEnumerator Retrasar(AudioClip clip, float segundos, float volumen)
    {
        yield return new WaitForSecondsRealtime(segundos);
        Efecto(clip, volumen);
    }

    /// <summary>Crea una fuente 3D en bucle pegada a un objeto (antorcha, Candileja...).</summary>
    public static AudioSource BucleEn(GameObject objeto, AudioClip clip, float volumen = 1f, float distanciaMax = 12f)
    {
        if (!objeto || !clip) return null;
        var s = objeto.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 1f;
        s.maxDistance = distanciaMax;
        s.time = Random.Range(0f, clip.length * 0.9f);
        var v = objeto.AddComponent<VolumenEfecto>();
        v.fuente = s;
        v.volumen = volumen;
        s.Play();
        return s;
    }

    // ============================================================
    //  VOLUMEN
    // ============================================================
    public static void SetGeneral(float v) { VolGeneral = Mathf.Clamp01(v); Guardar(); }
    public static void SetMusica(float v) { VolMusica = Mathf.Clamp01(v); Guardar(); }
    public static void SetEfectos(float v) { VolEfectos = Mathf.Clamp01(v); Guardar(); }
    public static void SetActivado(bool on) { SonidoActivado = on; Guardar(); }

    static void Guardar()
    {
        PlayerPrefs.SetFloat(kGeneral, VolGeneral);
        PlayerPrefs.SetFloat(kMusica, VolMusica);
        PlayerPrefs.SetFloat(kEfectos, VolEfectos);
        PlayerPrefs.SetInt(kActivado, SonidoActivado ? 1 : 0);
        PlayerPrefs.Save();
        AplicarVolumenes();
    }

    static void AplicarVolumenes()
    {
        AudioListener.volume = SonidoActivado ? VolGeneral : 0f;
    }

    /// <summary>Conecta los sliders de la escena Opciones (filas General, Música, Efectos).</summary>
    void ConectarOpciones(Scene escena)
    {
        foreach (var root in escena.GetRootGameObjects())
        {
            foreach (var slider in root.GetComponentsInChildren<Slider>(true))
            {
                string fila = slider.transform.parent ? slider.transform.parent.name : slider.name;
                if (fila.Contains("General"))
                    Enlazar(slider, VolGeneral, SetGeneral);
                else if (fila.Contains("Musica") || fila.Contains("Música"))
                    Enlazar(slider, VolMusica, SetMusica);
                else if (fila.Contains("Efecto"))
                    Enlazar(slider, VolEfectos, SetEfectos);
            }

            foreach (var toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                toggle.SetIsOnWithoutNotify(SonidoActivado);
                toggle.onValueChanged.AddListener(SetActivado);
            }
        }
    }

    static void Enlazar(Slider slider, float valor, UnityEngine.Events.UnityAction<float> accion)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(valor);
        slider.onValueChanged.AddListener(accion);
    }
}
