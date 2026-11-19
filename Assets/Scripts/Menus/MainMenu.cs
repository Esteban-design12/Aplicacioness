using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el menú de pausa (Esc) dentro de la escena de La Candileja.
/// El menú principal ahora está en su PROPIA escena.
/// Ponlo en un objeto vacío "MenuManager" que NUNCA se desactive (fuera de los paneles).
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Paneles")]
    [Tooltip("Panel del menú de pausa (con el botón Continuar).")]
    public GameObject pausePanel;
    [Tooltip("Opcional: el HUD. Solo se ve mientras se juega.")]
    public GameObject hud;

    [Header("Se desactivan cuando NO se está jugando")]
    [Tooltip("Movimiento, cámara, InteractionSystem, InventoryUI, CandilejaTouch...")]
    public Behaviour[] scriptsToDisable;

    [Header("Opciones")]
    [Tooltip("Nombre EXACTO de la escena del menú principal (como aparece en Build Settings).")]
    public string menuSceneName = "MenuPrincipal";
    public bool freezeTime = true;
    public KeyCode pauseKey = KeyCode.Escape;

    bool paused;

    void Start()
    {
        // Al cargar la escena se empieza a jugar de una vez
        StartPlaying();
    }

    void Update()
    {
        if (!Input.GetKeyDown(pauseKey)) return;

        if (paused) ResumeGame();
        else Pause();
    }

    // ---------- Botones ----------

    /// <summary>Botón CONTINUAR (menú de pausa).</summary>
    public void ResumeGame() => StartPlaying();

    /// <summary>Botón MENÚ PRINCIPAL (menú de pausa): carga la escena del menú.</summary>
    public void BackToMenu()
    {
        Time.timeScale = 1f; // que el menú no quede congelado
        SetCursor(true);
        SceneManager.LoadScene(menuSceneName);
    }

    /// <summary>Botón SALIR.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        // En el editor, Application.Quit() no hace nada: se detiene el Play
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Estados ----------

    void Pause()
    {
        paused = true;
        Show(pausePanel, true);
        Show(hud, false);
        Freeze(true);
    }

    void StartPlaying()
    {
        paused = false;
        Show(pausePanel, false);
        Show(hud, true);
        Freeze(false);
    }

    // ---------- Utilidades ----------

    void Freeze(bool frozen)
    {
        foreach (var script in scriptsToDisable)
            if (script) script.enabled = !frozen;

        if (freezeTime) Time.timeScale = frozen ? 0f : 1f;
        SetCursor(frozen);
    }

    static void Show(GameObject obj, bool visible)
    {
        if (obj) obj.SetActive(visible);
    }

    static void SetCursor(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }
}