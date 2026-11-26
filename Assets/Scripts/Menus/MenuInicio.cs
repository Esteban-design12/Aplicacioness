using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal en su PROPIA escena (va en CanvasMenuPrincipal).
/// Jugar → Previo → Tutorial → Candileja.
/// </summary>
public class MenuInicio : MonoBehaviour
{
    [Tooltip("Escena que se abre al pulsar JUGAR (primero el Previo).")]
    public string gameSceneName = "Previo";

    void Start()
    {
        // Si venimos del menú de pausa, el tiempo pudo quedar en 0
        Time.timeScale = 1f;

        // En el menú el cursor debe verse y moverse libre
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Botón JUGAR.</summary>
    public void PlayGame()
    {
        ProgresoPartida.Reiniciar(); // partida nueva
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Botones OPCIONES y CRÉDITOS: carga la escena que se escriba en el botón.</summary>
    public void IrA(string escena)
    {
        SceneManager.LoadScene(escena);
    }

    /// <summary>Botón SALIR.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
