using UnityEngine;

public class CandilejaIA : MonoBehaviour
{
    [Header("Jugador")]
    public Transform jugador;

    [Header("Movimiento")]
    public float velocidadPatrulla = 2f;
    public float velocidadPersecucion = 5f;

    [Header("Vision")]
    public float distanciaVision = 10f;
    [Range(0, 360)]
    public float anguloVision = 120f;

    [Header("Patrulla aleatoria")]
    public float distanciaPatrulla = 8f;
    public float tiempoCambioDestino = 3f;

    [Header("Susto")]
    [Tooltip("Distancia al jugador a la que hace la animación Scare.")]
    public float distanciaSusto = 1.5f;
    [Tooltip("Segundos que se queda quieta asustando y que el jugador queda bloqueado.")]
    public float duracionSusto = 2f;
    [Tooltip("Segundos de espera antes de poder asustar otra vez.")]
    public float cooldownSusto = 5f;

    [Header("Animación")]
    public string parametroVelocidad = "Speed";
    public string triggerSusto = "Scare";

    private Animator animator;
    private Vector3 destino;
    private float temporizador;
    private bool persiguiendo = false;

    private bool asustando = false;
    private float finSusto = 0f;
    private float proximoSusto = 0f;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        BuscarNuevoDestino();
    }

    void OnDisable()
    {
        // Si la Candileja se desactiva en pleno susto, libera al jugador
        if (asustando)
            Movimiento.puedeMoverse = true;
    }

    void Update()
    {
        if (jugador == null)
            return;

        // Mientras hace la animación de susto, se queda quieta mirando al jugador
        if (asustando)
        {
            Vector3 haciaJugador = jugador.position - transform.position;
            haciaJugador.y = 0f;
            MirarHacia(haciaJugador);

            SetVelocidadAnimator(0f);

            if (Time.time >= finSusto)
            {
                asustando = false;
                Movimiento.puedeMoverse = true;   // desbloquea al jugador
            }

            return;
        }

        float distancia = Vector3.Distance(transform.position, jugador.position);

        // Si está cerca del jugador, lo asusta
        if (distancia <= distanciaSusto && Time.time >= proximoSusto)
        {
            IniciarSusto();
            return;
        }

        if (JugadorVisible())
        {
            persiguiendo = true;
            PerseguirJugador();
        }
        else
        {
            persiguiendo = false;
            Patrullar();
        }
    }

    // --------------------------------------------------
    // SUSTO
    // --------------------------------------------------

    void IniciarSusto()
    {
        asustando = true;
        finSusto = Time.time + duracionSusto;
        proximoSusto = Time.time + cooldownSusto;

        Movimiento.puedeMoverse = false;   // bloquea al jugador

        SetVelocidadAnimator(0f);

        if (animator != null)
            animator.SetTrigger(triggerSusto);
    }

    // --------------------------------------------------
    // PATRULLA ALEATORIA
    // --------------------------------------------------

    void Patrullar()
    {
        temporizador += Time.deltaTime;

        if (temporizador >= tiempoCambioDestino ||
            Vector3.Distance(transform.position, destino) < 1f)
        {
            BuscarNuevoDestino();
            temporizador = 0f;
        }

        Vector3 direccion = destino - transform.position;
        direccion.y = 0f;

        if (direccion.magnitude > 0.1f)
        {
            transform.position +=
                direccion.normalized *
                velocidadPatrulla *
                Time.deltaTime;

            MirarHacia(direccion);
            SetVelocidadAnimator(1f);
        }
        else
        {
            SetVelocidadAnimator(0f);
        }
    }

    void BuscarNuevoDestino()
    {
        Vector2 aleatorio = Random.insideUnitCircle * distanciaPatrulla;

        destino = transform.position +
                  new Vector3(aleatorio.x, 0f, aleatorio.y);
    }

    // --------------------------------------------------
    // PERSECUCIÓN
    // --------------------------------------------------

    void PerseguirJugador()
    {
        Vector3 direccion = jugador.position - transform.position;

        direccion.y = 0f;

        if (direccion.magnitude > 0.5f)
        {
            transform.position +=
                direccion.normalized *
                velocidadPersecucion *
                Time.deltaTime;

            MirarHacia(direccion);
            SetVelocidadAnimator(1f);
        }
        else
        {
            SetVelocidadAnimator(0f);
        }
    }

    // --------------------------------------------------
    // ANIMATOR
    // --------------------------------------------------

    void SetVelocidadAnimator(float valor)
    {
        if (animator != null)
            animator.SetFloat(parametroVelocidad, valor);
    }

    // --------------------------------------------------
    // CAMPO DE VISION
    // --------------------------------------------------

    bool JugadorVisible()
    {
        Vector3 direccionAlJugador =
            jugador.position - transform.position;

        float distancia =
            direccionAlJugador.magnitude;

        // El jugador está demasiado lejos
        if (distancia > distanciaVision)
            return false;

        direccionAlJugador.Normalize();

        float angulo =
            Vector3.Angle(transform.forward, direccionAlJugador);

        // El jugador está fuera del cono de visión
        if (angulo > anguloVision / 2f)
            return false;

        // Comprobar si hay obstáculos
        RaycastHit hit;

        if (Physics.Raycast(
            transform.position,
            direccionAlJugador,
            out hit,
            distanciaVision))
        {
            if (hit.transform.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    // --------------------------------------------------
    // GIRAR HACIA EL OBJETIVO
    // --------------------------------------------------

    void MirarHacia(Vector3 direccion)
    {
        if (direccion == Vector3.zero)
            return;

        Quaternion rotacion =
            Quaternion.LookRotation(direccion);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                rotacion,
                5f * Time.deltaTime);
    }

    // --------------------------------------------------
    // DIBUJAR CAMPO DE VISION
    // --------------------------------------------------

    void OnDrawGizmosSelected()
    {
        // Distancia de visión
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            distanciaVision);

        // Distancia de susto
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(
            transform.position,
            distanciaSusto);

        // Dirección central
        Gizmos.color = Color.red;

        Vector3 izquierda =
            Quaternion.Euler(
                0,
                -anguloVision / 2,
                0) * transform.forward;

        Vector3 derecha =
            Quaternion.Euler(
                0,
                anguloVision / 2,
                0) * transform.forward;

        Gizmos.DrawRay(
            transform.position,
            izquierda * distanciaVision);

        Gizmos.DrawRay(
            transform.position,
            derecha * distanciaVision);

        Gizmos.DrawRay(
            transform.position,
            transform.forward * distanciaVision);
    }
}