using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Navegación entre escenas de menú (Previo, Tutorial, Opciones, Créditos, Lose, Win...).
/// Va en el Canvas de cada escena. En el On Click() de un botón elige
/// CambiarEscena → IrA(string) y escribe el nombre EXACTO de la escena.
/// </summary>
public class CambiarEscena : MonoBehaviour
{
    [Tooltip("En escenas de menú el cursor debe verse para poder hacer clic.")]
    public bool mostrarCursor = true;

    /// <summary>True si esta escena se abrió ENCIMA de otra (ej. Opciones desde la pausa).</summary>
    bool abiertaEncima;

    void Awake()
    {
        abiertaEncima = gameObject.scene != SceneManager.GetActiveScene();
        if (!abiertaEncima) return;

        // Abierta encima del juego: apagar lo que ya tiene la escena del juego
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var c in root.GetComponentsInChildren<Camera>(true)) c.gameObject.SetActive(false);
            foreach (var l in root.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (var e in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                e.gameObject.SetActive(false);
            foreach (var cv in root.GetComponentsInChildren<Canvas>(true))
                if (cv.isRootCanvas) cv.sortingOrder = 100; // por encima del menú de pausa
        }
    }

    void Start()
    {
        // Encima del juego pausado: no tocar el tiempo ni el cursor
        if (abiertaEncima) return;

        // Si venimos del juego, el tiempo pudo quedar en 0 y el cursor bloqueado
        Time.timeScale = 1f;

        if (mostrarCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>Carga la escena con ese nombre (debe estar en Build Settings).</summary>
    public void IrA(string escena)
    {
        // Si esta escena está abierta encima del juego (Opciones desde la pausa),
        // "regresar" es cerrarla y volver al menú de pausa sin perder la partida.
        if (abiertaEncima)
        {
            SceneManager.UnloadSceneAsync(gameObject.scene);
            return;
        }

        if (string.IsNullOrEmpty(escena))
        {
            Debug.LogWarning("CambiarEscena: el botón no tiene nombre de escena.", this);
            return;
        }
        SceneManager.LoadScene(escena);
    }

    /// <summary>Cierra el juego (en el editor detiene el Play).</summary>
    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
