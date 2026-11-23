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

    void Start()
    {
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
