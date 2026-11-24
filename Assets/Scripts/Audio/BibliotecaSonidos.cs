using UnityEngine;

/// <summary>
/// Todos los sonidos del juego en un solo lugar.
/// El archivo está en Assets/Resources/BibliotecaSonidos: ábrelo para cambiar
/// un sonido por otro o ajustar volúmenes, sin tocar código.
/// </summary>
[CreateAssetMenu(menuName = "Candileja/Biblioteca de sonidos", fileName = "BibliotecaSonidos")]
public class BibliotecaSonidos : ScriptableObject
{
    [Header("Música")]
    [Tooltip("Menú principal, Previo, Tutorial, Opciones y Créditos.")]
    public AudioClip musicaMenu;
    [Tooltip("Escenas Lose y Win.")]
    public AudioClip musicaFinal;
    [Tooltip("Suena mientras la Candileja persigue a Antonio.")]
    public AudioClip persecucion;

    [Header("Ambiente del bosque (en bucle)")]
    public AudioClip ambienteGeneral;
    public AudioClip grillos;
    public AudioClip viento;
    [Range(0f, 1f)] public float volumenAmbiente = 0.6f;

    [Header("Ambiente aleatorio (suenan de vez en cuando, alrededor de Antonio)")]
    public AudioClip[] sonidosAleatorios;
    public Vector2 segundosEntreAleatorios = new Vector2(10f, 25f);
    [Range(0f, 1f)] public float volumenAleatorios = 0.7f;

    [Header("Antonio")]
    public AudioClip pasos;
    [Range(0f, 1f)] public float volumenPasos = 0.6f;
    public AudioClip correr;
    public AudioClip respiracionAgitada;

    [Header("Candileja")]
    [Tooltip("Se escucha cerca de la Candileja (en 3D).")]
    public AudioClip murmulloCandileja;
    [Tooltip("Cuando la Candileja ve a Antonio.")]
    public AudioClip jugadorDetectado;
    [Tooltip("Cuando la Candileja atrapa a Antonio.")]
    public AudioClip capturaJugador;

    [Header("Objetos y recuerdos")]
    public AudioClip recogerObjeto;
    [Tooltip("Cuando se completan todos los recuerdos.")]
    public AudioClip recuerdoCompleto;
    public AudioClip colocarObjeto;
    public AudioClip fragmentoDiario;

    [Header("Antorcha y fuego")]
    public AudioClip encenderAntorcha;
    [Tooltip("Bucle de la antorcha ya encendida (en 3D).")]
    public AudioClip antorchaEncendida;
    public AudioClip fuego;

    [Header("Puzles y lugares (para usar cuando se agreguen)")]
    public AudioClip puzleCorrecto;
    public AudioClip puzleIncorrecto;
    public AudioClip puertaDesbloqueada;
    public AudioClip accesoRocas;
    public AudioClip semilla;
    public AudioClip casa;
}
