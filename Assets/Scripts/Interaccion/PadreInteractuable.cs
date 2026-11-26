using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// El papá de Antonio, afuera del laberinto. Al acercarse y pulsar E, Antonio gana
/// y se carga la escena de victoria.
/// Va en el objeto del papá. Si no tiene collider, se le crea uno automáticamente.
/// </summary>
[AddComponentMenu("Juego/Padre interactuable")]
public class PadreInteractuable : Interactable
{
    [Header("Victoria")]
    public string escenaVictoria = "Win";
    [Tooltip("Segundos de espera antes de cargar la escena (para que suene el efecto).")]
    public float espera = 1.2f;

    [Header("Interacción")]
    [Tooltip("Radio extra del área para interactuar con él (metros).")]
    public float margenInteraccion = 0.5f;

    bool terminado;

    void Reset() => prompt = "Hablar con papá";

    void Awake()
    {
        if (string.IsNullOrEmpty(prompt) || prompt == "Interactuar") prompt = "Hablar con papá";
        AsegurarCollider();
    }

    public override string GetPrompt() => terminado ? "" : prompt;

    public override void Interact()
    {
        if (terminado) return;

        terminado = true;
        Movimiento.puedeMoverse = false;   // Antonio se queda quieto
        if (AudioJuego.Sonidos) AudioJuego.Efecto(AudioJuego.Sonidos.recuerdoCompleto);
        StartCoroutine(Ganar());
    }

    IEnumerator Ganar()
    {
        yield return new WaitForSecondsRealtime(espera);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(escenaVictoria);
    }

    /// <summary>Crea un collider (trigger) del tamaño del papá si no tiene ninguno.</summary>
    void AsegurarCollider()
    {
        if (GetComponentInChildren<Collider>()) return;

        var renderers = GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(transform.position + Vector3.up, new Vector3(0.6f, 1.8f, 0.6f));
        if (renderers.Length > 0)
        {
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
        }

        var col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        Vector3 escala = transform.lossyScale;
        float s = Mathf.Max(0.0001f, Mathf.Abs(escala.x), Mathf.Abs(escala.y), Mathf.Abs(escala.z));
        col.center = transform.InverseTransformPoint(b.center);
        col.radius = (Mathf.Max(b.extents.x, b.extents.z) + margenInteraccion) / s;
    }
}
