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

    private Vector3 destino;
    private float temporizador;

    private bool persiguiendo = false;

    void Start()
    {
        BuscarNuevoDestino();
    }

    void Update()
    {
        if (jugador == null)
            return;

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
        }
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