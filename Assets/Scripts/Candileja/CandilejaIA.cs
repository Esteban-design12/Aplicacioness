using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// IA de la Candileja dentro del laberinto.
///
/// Se mueve SOLO por los pasillos (calcula un mapa del laberinto al iniciar y busca
/// caminos con A*), y alterna entre estados para que la persecución sea justa:
///  - Patrulla:     recorre el laberinto a puntos al azar.
///  - Caza:         de vez en cuando "presiente" a Antonio y va hacia donde está.
///  - Persecución:  si lo VE lo persigue, pero solo un tiempo máximo.
///  - Búsqueda:     si lo pierde de vista, va al último lugar donde lo vio y mira alrededor.
///  - Descanso:     después de perseguir o asustar, lo deja en paz un rato.
/// Con cada recuerdo que recoge Antonio, la Candileja caza más seguido y se mueve más rápido.
/// </summary>
public class CandilejaIA : MonoBehaviour
{
    [Header("Jugador")]
    public Transform jugador;

    [Header("Laberinto")]
    [Tooltip("Objeto que contiene los muros. Si está vacío busca uno llamado 'Laberinto'.")]
    public Transform laberinto;
    [Tooltip("Tamaño de cada celda del mapa (m). Más pequeño = más preciso pero más lento de calcular.")]
    public float tamCelda = 1f;
    [Tooltip("Qué tan 'gorda' es la Candileja para no rozar los muros.")]
    public float radioCandileja = 0.4f;

    [Header("Velocidades")]
    public float velocidadPatrulla = 2.5f;
    [Tooltip("Velocidad cuando 'presiente' a Antonio y va a buscarlo.")]
    public float velocidadCaza = 3.8f;
    [Tooltip("Velocidad cuando lo ve. Nunca supera a Antonio (ver 'Ventaja del jugador').")]
    public float velocidadPersecucion = 4.2f;
    [Tooltip("La persecución nunca pasa de este % de la velocidad de Antonio: siempre se puede escapar.")]
    [Range(0.5f, 1.2f)] public float ventajaJugador = 0.95f;

    [Header("Visión")]
    public float distanciaVision = 15f;
    [Range(0, 360)] public float anguloVision = 140f;
    [Tooltip("Si Antonio está así de cerca lo nota aunque esté a su espalda.")]
    public float distanciaPresencia = 2.5f;
    public float alturaOjos = 1.5f;

    [Header("Persecución justa")]
    [Tooltip("Segundos máximos persiguiendo antes de rendirse.")]
    public float duracionMaxPersecucion = 15f;
    [Tooltip("Segundos mirando alrededor donde lo perdió de vista.")]
    public float tiempoBusqueda = 5f;
    [Tooltip("Segundos que lo deja en paz después de una persecución.")]
    public float descansoTrasPersecucion = 7f;

    [Header("Caza (de vez en cuando va a buscarlo)")]
    [Tooltip("Segundos entre cazas (mínimo, máximo).")]
    public Vector2 intervaloCaza = new Vector2(15f, 30f);
    [Tooltip("Segundos máximos que dura una caza si no lo encuentra.")]
    public float duracionCaza = 35f;
    [Tooltip("Segundos después de que Antonio entra al laberinto antes de la primera caza.")]
    public float graciaInicial = 8f;

    [Header("Acecho")]
    [Tooltip("Probabilidad de que, al patrullar, elija un punto cerca de Antonio.")]
    [Range(0f, 1f)] public float probabilidadPatrullaCerca = 0.65f;
    [Tooltip("Radio alrededor de Antonio para esos puntos de patrulla.")]
    public float radioPatrullaCerca = 25f;
    [Tooltip("Si Antonio se mueve a esta distancia, ella lo OYE (aunque no lo vea) y va a revisar.")]
    public float distanciaOido = 9f;

    [Header("Dificultad")]
    [Tooltip("Por cada recuerdo recogido: +% velocidad y cazas más seguidas.")]
    [Range(0f, 0.5f)] public float aumentoPorRecuerdo = 0.12f;

    [Header("Susto")]
    [Tooltip("Distancia al jugador a la que hace la animación Scare.")]
    public float distanciaSusto = 1.5f;
    [Tooltip("Segundos que se queda quieta asustando y que el jugador queda bloqueado.")]
    public float duracionSusto = 2f;
    [Tooltip("Segundos antes de poder asustar otra vez (mientras tanto, si lo alcanza, lo atrapa).")]
    public float cooldownSusto = 40f;
    [Tooltip("Si está activo, al terminar la animación del susto Antonio PIERDE (carga la escena de derrota).")]
    public bool perderAlAsustar = true;
    [Tooltip("Escena que se carga cuando Antonio pierde.")]
    public string escenaDerrota = "Lose";
    [Tooltip("Solo si 'Perder Al Asustar' está apagado: se aleja estos segundos para darle chance de huir.")]
    public float descansoTrasSusto = 6f;

    [Header("Sonido")]
    [Range(0f, 1f)] public float volumenMurmullo = 0.9f;
    public float distanciaMurmullo = 14f;

    [Header("Animación")]
    public string parametroVelocidad = "Speed";
    public string triggerSusto = "Scare";

    [Header("Depuración")]
    public bool dibujarCamino = true;
    [SerializeField] string estadoActual;

    enum Estado { Patrulla, Caza, Persecucion, Busqueda, Descanso, Susto }
    Estado estado = Estado.Patrulla;

    static CandilejaIA cazando; // solo una Candileja caza a la vez
    static readonly System.Collections.Generic.HashSet<CandilejaIA> detectando =
        new System.Collections.Generic.HashSet<CandilejaIA>();

    /// <summary>true si alguna Candileja tiene detectado a Antonio (lo persigue o lo asusta).</summary>
    public static bool JugadorDetectado
    {
        get
        {
            foreach (var c in detectando)
                if (c && c.isActiveAndEnabled) return true;
            return false;
        }
    }

    MapaLaberinto mapa;
    Animator animator;
    Movimiento movJugador;
    readonly RaycastHit[] golpes = new RaycastHit[8];

    List<Vector3> camino = new List<Vector3>();
    int indiceCamino;
    float proximoRecalculo;

    float finEstado, proximaCaza, proximoSusto, esperaHasta;
    float finAcoso; // límite total de persecución (aunque lo pierda y lo vuelva a ver)
    bool jugadorEntro;           // ¿Antonio ya entró al laberinto?
    Vector3 posJugadorAnterior;
    float proximoOido;
    Vector3 ultimaPosicionVista;
    Vector3 ultimaPos; float chequeoAtasco;
    float altura;

    // ============================================================
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (jugador) movJugador = jugador.GetComponentInChildren<Movimiento>();
        altura = transform.position.y;

        if (!laberinto)
        {
            var go = GameObject.Find("Laberinto");
            if (go) laberinto = go.transform;
        }
        mapa = MapaLaberinto.Obtener(laberinto, tamCelda, radioCandileja);
        if (mapa == null)
            Debug.LogWarning("CandilejaIA: no encontré el laberinto; se moverá sin respetar muros.", this);

        proximaCaza = float.MaxValue; // se programa cuando Antonio entra al laberinto
        posJugadorAnterior = jugador ? jugador.position : Vector3.zero;
        ultimaPos = transform.position;

        if (AudioJuego.Sonidos)
            AudioJuego.BucleEn(gameObject, AudioJuego.Sonidos.murmulloCandileja, volumenMurmullo, distanciaMurmullo);

        CambiarA(Estado.Patrulla);
    }

    void OnDisable()
    {
        if (estado == Estado.Susto && !perderAlAsustar) Movimiento.puedeMoverse = true;
        if (cazando == this) cazando = null;
        detectando.Remove(this);
        AudioJuego.Persecucion(false);
    }

    // ============================================================
    void Update()
    {
        if (!jugador) return;
        estadoActual = estado.ToString();
        if (estado == Estado.Persecucion || estado == Estado.Susto) detectando.Add(this);
        else detectando.Remove(this);

        if (estado == Estado.Susto)
        {
            MirarHacia(jugador.position - transform.position);
            SetVelocidadAnimator(0f);
            if (Time.time >= finEstado)
            {
                if (perderAlAsustar && !string.IsNullOrEmpty(escenaDerrota))
                {
                    Perder();
                    return;
                }
                Movimiento.puedeMoverse = true;
                Descansar(descansoTrasSusto);
            }
            return;
        }

        float distancia = Plano(jugador.position - transform.position).magnitude;
        bool enLaberinto = mapa == null || mapa.Dentro(jugador.position);
        bool visible = enLaberinto && estado != Estado.Descanso && JugadorVisible(distancia);

        // Susto: el primer contacto es un aviso; si lo vuelve a alcanzar pronto, lo atrapa (CandilejaTouch)
        if (distancia <= distanciaSusto && Time.time >= proximoSusto && estado != Estado.Descanso)
        {
            IniciarSusto();
            return;
        }

        // Primera vez que Antonio entra: la caza empieza pronto
        if (enLaberinto && !jugadorEntro)
        {
            jugadorEntro = true;
            proximaCaza = Time.time + graciaInicial;
        }

        // Oído: si Antonio se mueve cerca, va a revisar aunque no lo vea
        bool seMueve = (jugador.position - posJugadorAnterior).sqrMagnitude > 0.0004f;
        posJugadorAnterior = jugador.position;
        if (!visible && seMueve && enLaberinto && distancia <= distanciaOido &&
            Time.time >= proximoOido &&
            (estado == Estado.Patrulla || estado == Estado.Caza))
        {
            proximoOido = Time.time + 1.5f;
            if (estado == Estado.Caza) ProgramarCaza();
            ultimaPosicionVista = jugador.position;
            CambiarA(Estado.Busqueda);
            IrA(ultimaPosicionVista);
        }

        if (visible)
        {
            ultimaPosicionVista = jugador.position;
            if (estado != Estado.Persecucion) EmpezarPersecucion();
        }

        switch (estado)
        {
            case Estado.Persecucion:
                if (Time.time >= finEstado) { Descansar(descansoTrasPersecucion); break; }  // se cansa
                if (!visible)
                {
                    CambiarA(Estado.Busqueda);
                    IrA(ultimaPosicionVista);
                    AudioJuego.Persecucion(false); // se apaga a los pocos segundos si no lo vuelve a ver
                    break;
                }
                if (Time.time >= proximoRecalculo) { IrA(jugador.position); proximoRecalculo = Time.time + 0.3f; }
                Avanzar(VelPersecucion());
                break;

            case Estado.Busqueda:
                if (Avanzar(velocidadCaza * Multiplicador()))
                {
                    // Llegó: mira a su alrededor un rato
                    if (esperaHasta == 0f) esperaHasta = Time.time + tiempoBusqueda;
                    transform.Rotate(0f, 90f * Time.deltaTime, 0f);
                    if (Time.time >= esperaHasta) CambiarA(Estado.Patrulla);
                }
                break;

            case Estado.Caza:
                if (Time.time >= finEstado || !enLaberinto) { TerminarCaza(); break; }
                if (Time.time >= proximoRecalculo) { IrA(jugador.position); proximoRecalculo = Time.time + 1f; }
                if (Avanzar(velocidadCaza * Multiplicador()))
                {
                    // Llegó a donde estaba Antonio: lo busca por ahí
                    ultimaPosicionVista = jugador.position;
                    ProgramarCaza();
                    CambiarA(Estado.Busqueda);
                    IrA(ultimaPosicionVista);
                }
                break;

            case Estado.Descanso:
                Patrullar();
                if (Time.time >= finEstado) CambiarA(Estado.Patrulla);
                break;

            default: // Patrulla
                Patrullar();
                if (Time.time >= proximaCaza && enLaberinto && cazando == null) EmpezarCaza();
                break;
        }

        RevisarAtasco();
    }

    // ============================================================
    //  ESTADOS
    // ============================================================
    void CambiarA(Estado nuevo)
    {
        estado = nuevo;
        esperaHasta = 0f;
        if (nuevo != Estado.Caza && cazando == this) cazando = null;
    }

    void EmpezarPersecucion()
    {
        // Si lo estaba buscando, sigue contando el mismo tiempo de acoso
        if (estado != Estado.Busqueda) finAcoso = Time.time + duracionMaxPersecucion;
        CambiarA(Estado.Persecucion);
        finEstado = finAcoso;
        ProgramarCaza();
        proximoRecalculo = 0f;
        AudioJuego.Persecucion(true);
    }

    void EmpezarCaza()
    {
        CambiarA(Estado.Caza);
        cazando = this;
        finEstado = Time.time + duracionCaza;
        proximoRecalculo = 0f;
    }

    void TerminarCaza()
    {
        ProgramarCaza();
        CambiarA(Estado.Patrulla);
        camino.Clear();
    }

    void ProgramarCaza()
    {
        float espera = Random.Range(intervaloCaza.x, intervaloCaza.y) / Multiplicador();
        proximaCaza = Time.time + espera;
    }

    void Descansar(float segundos)
    {
        CambiarA(Estado.Descanso);
        finEstado = Time.time + segundos;
        ProgramarCaza();
        camino.Clear();
        AudioJuego.Persecucion(false);

        // Irse a un punto lejos de Antonio
        if (mapa != null)
        {
            Vector3 mejor = mapa.PuntoAleatorioLibre();
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = mapa.PuntoAleatorioLibre();
                if ((p - jugador.position).sqrMagnitude > (mejor - jugador.position).sqrMagnitude) mejor = p;
            }
            IrA(mejor);
        }
    }

    void IniciarSusto()
    {
        CambiarA(Estado.Susto);
        finEstado = Time.time + duracionSusto;
        proximoSusto = Time.time + cooldownSusto;
        Movimiento.puedeMoverse = false;   // bloquea al jugador
        SetVelocidadAnimator(0f);
        if (animator) animator.SetTrigger(triggerSusto);

        if (AudioJuego.Sonidos)
            AudioJuego.Efecto3D(AudioJuego.Sonidos.jugadorDetectado, transform.position, 1f, 15f);
    }

    /// <summary>Antonio pierde: sigue sin poder moverse y se carga la escena de derrota.</summary>
    void Perder()
    {
        enabled = false;                       // la Candileja deja de actuar
        if (AudioJuego.Sonidos) AudioJuego.Efecto(AudioJuego.Sonidos.capturaJugador);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(escenaDerrota);
    }

    void Patrullar()
    {
        if (Avanzar(velocidadPatrulla * Multiplicador()))
        {
            // Pausa corta en cada punto y luego otro destino
            if (esperaHasta == 0f) esperaHasta = Time.time + Random.Range(0.8f, 2.5f);
            if (Time.time >= esperaHasta)
            {
                esperaHasta = 0f;
                IrA(DestinoPatrulla());
            }
        }
    }

    /// <summary>Punto de patrulla: muchas veces cerca de Antonio (lo acecha).</summary>
    Vector3 DestinoPatrulla()
    {
        if (mapa == null)
            return transform.position + new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f));

        bool acechar = estado != Estado.Descanso && jugadorEntro && mapa.Dentro(jugador.position) &&
                       Random.value < Mathf.Clamp01(probabilidadPatrullaCerca * Multiplicador());
        if (acechar)
        {
            float r2 = radioPatrullaCerca * radioPatrullaCerca;
            for (int i = 0; i < 40; i++)
            {
                Vector3 p = mapa.PuntoAleatorioLibre();
                if (Plano(p - jugador.position).sqrMagnitude <= r2) return p;
            }
        }
        return mapa.PuntoAleatorioLibre();
    }

    // ============================================================
    //  DIFICULTAD
    // ============================================================
    float Multiplicador()
    {
        int recuerdos = Inventory.Instance != null ? Inventory.Instance.Items.Count : 0;
        return 1f + aumentoPorRecuerdo * recuerdos;
    }

    float VelPersecucion()
    {
        float v = velocidadPersecucion * Multiplicador();
        if (movJugador) v = Mathf.Min(v, movJugador.speed * ventajaJugador); // siempre se puede escapar
        return v;
    }

    // ============================================================
    //  MOVIMIENTO POR EL CAMINO
    // ============================================================
    void IrA(Vector3 destino)
    {
        indiceCamino = 0;
        if (mapa != null)
        {
            camino = mapa.Camino(transform.position, destino);
            // El primer punto es su propia celda: saltarlo si ya está ahí
            if (camino.Count > 1) indiceCamino = 1;
        }
        else
        {
            camino.Clear();
            camino.Add(destino);
        }
    }

    /// <summary>Avanza por el camino. Devuelve true si ya llegó (o no tiene camino).</summary>
    bool Avanzar(float velocidad)
    {
        if (camino == null || indiceCamino >= camino.Count)
        {
            SetVelocidadAnimator(0f);
            return true;
        }

        Vector3 objetivo = camino[indiceCamino];
        objetivo.y = altura;
        Vector3 hacia = objetivo - transform.position;
        hacia.y = 0f;

        float paso = velocidad * Time.deltaTime;
        if (hacia.magnitude <= Mathf.Max(0.15f, paso))
        {
            transform.position = new Vector3(objetivo.x, altura, objetivo.z);
            indiceCamino++;
            return indiceCamino >= camino.Count;
        }

        transform.position += hacia.normalized * paso;
        MirarHacia(hacia);
        SetVelocidadAnimator(1f);
        return false;
    }

    void RevisarAtasco()
    {
        if (Time.time < chequeoAtasco) return;
        chequeoAtasco = Time.time + 2f;
        bool moviendo = camino != null && indiceCamino < camino.Count;
        if (moviendo && Plano(transform.position - ultimaPos).sqrMagnitude < 0.04f && estado != Estado.Busqueda)
            IrA(mapa != null ? mapa.PuntoAleatorioLibre() : transform.position); // destrabar
        ultimaPos = transform.position;
    }

    // ============================================================
    //  VISIÓN
    // ============================================================
    bool JugadorVisible(float distancia)
    {
        if (distancia > distanciaVision) return false;

        Vector3 ojos = transform.position + Vector3.up * alturaOjos;
        Vector3 pecho = jugador.position + Vector3.up * 1f;
        Vector3 dir = pecho - ojos;

        // Fuera del cono de visión (salvo que esté muy cerca)
        if (distancia > distanciaPresencia &&
            Vector3.Angle(Plano(transform.forward), Plano(dir)) > anguloVision * 0.5f)
            return false;

        // ¿Hay un muro en medio?
        int n = Physics.RaycastNonAlloc(ojos, dir.normalized, golpes, dir.magnitude + 0.5f, ~0,
                                        QueryTriggerInteraction.Ignore);
        float masCerca = float.MaxValue; Transform primero = null;
        for (int i = 0; i < n; i++)
        {
            var t = golpes[i].transform;
            if (t.IsChildOf(transform)) continue; // ella misma
            if (golpes[i].distance < masCerca) { masCerca = golpes[i].distance; primero = t; }
        }
        return primero == null || primero.IsChildOf(jugador) || primero.CompareTag("Player");
    }

    // ============================================================
    //  UTILIDADES
    // ============================================================
    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }

    void SetVelocidadAnimator(float valor)
    {
        if (animator) animator.SetFloat(parametroVelocidad, valor);
    }

    void MirarHacia(Vector3 direccion)
    {
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(direccion), 8f * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaVision);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, distanciaSusto);

        Vector3 izq = Quaternion.Euler(0, -anguloVision / 2, 0) * transform.forward;
        Vector3 der = Quaternion.Euler(0, anguloVision / 2, 0) * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, izq * distanciaVision);
        Gizmos.DrawRay(transform.position, der * distanciaVision);

        if (dibujarCamino && camino != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = Mathf.Max(0, indiceCamino - 1); i < camino.Count - 1; i++)
                Gizmos.DrawLine(camino[i] + Vector3.up * 0.3f, camino[i + 1] + Vector3.up * 0.3f);
        }
    }
}
