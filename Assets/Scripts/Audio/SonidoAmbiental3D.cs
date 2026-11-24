using UnityEngine;

/// <summary>
/// Pon este componente en cualquier objeto para que suene en bucle a su alrededor
/// (por ejemplo "Casa" en la casa o "Fuego" en una fogata).
/// También sirve para botones/eventos: llama a Reproducir() desde un UnityEvent.
/// </summary>
[AddComponentMenu("Juego/Sonido ambiental 3D")]
public class SonidoAmbiental3D : MonoBehaviour
{
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumen = 0.8f;
    [Tooltip("Hasta qué distancia se escucha (metros).")]
    public float distanciaMaxima = 12f;
    [Tooltip("Desmárcalo si solo quieres que suene al llamar Reproducir().")]
    public bool enBucleAlIniciar = true;

    void Start()
    {
        if (enBucleAlIniciar) AudioJuego.BucleEn(gameObject, sonido, volumen, distanciaMaxima);
    }

    /// <summary>Suena una vez en la posición de este objeto.</summary>
    public void Reproducir()
    {
        AudioJuego.Efecto3D(sonido, transform.position, volumen, distanciaMaxima);
    }
}
