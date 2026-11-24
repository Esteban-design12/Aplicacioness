using UnityEngine;

/// <summary>Mantiene el volumen de una fuente 3D según el slider de Efectos.</summary>
public class VolumenEfecto : MonoBehaviour
{
    public AudioSource fuente;
    public float volumen = 1f;
    void Update() { if (fuente) fuente.volume = volumen * AudioJuego.VolEfectos; }
}
