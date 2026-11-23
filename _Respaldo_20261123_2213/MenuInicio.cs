using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal en su PROPIA escena.
/// Ponlo en un objeto vacío "MenuManager" dentro de la escena del menú.
/// </summary>
public class MenuInicio : MonoBehaviour
{
    [Tooltip("Nombre EXACTO de la escena del juego (como aparece en Build Settings).")]
    public string gameSceneName = "Candileja";

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
        SceneManager.LoadScene(gameSceneName);
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