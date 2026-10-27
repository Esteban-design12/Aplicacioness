using UnityEngine;

/// <summary>
/// Controla el menú principal y el menú de pausa (Esc) dentro de la misma escena.
/// Ponlo en un objeto vacío "MenuManager" que NUNCA se desactive (fuera de los paneles).
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Paneles")]
    [Tooltip("Panel del menú principal (con el botón Jugar).")]
    public GameObject mainMenuPanel;
    [Tooltip("Panel del menú de pausa (con el botón Continuar).")]
    public GameObject pausePanel;
    [Tooltip("Opcional: el HUD. Solo se ve mientras se juega.")]
    public GameObject hud;

    [Header("Se desactivan cuando NO se está jugando")]
    [Tooltip("Movimiento, cámara, InteractionSystem, InventoryUI, CandilejaTouch...")]
    public Behaviour[] scriptsToDisable;

    [Header("Opciones")]
    public bool showMenuOnStart = true;
    public bool freezeTime = true;
    public KeyCode pauseKey = KeyCode.Escape;

    enum State { MainMenu, Playing, Paused }
    State state;

    void Awake()
    {
        // En Awake para desactivar los scripts antes de que corran su Start
        if (showMenuOnStart) GoToMainMenu();
        else StartPlaying();
    }

    void Start()
    {
        // Reafirma el cursor por si otro script lo bloqueó
        if (state != State.Playing) SetCursor(true);
    }

    void Update()
    {
        if (!Input.GetKeyDown(pauseKey)) return;

        if (state == State.Playing) Pause();
        else if (state == State.Paused) ResumeGame();
    }

    // ---------- Botones ----------

    /// <summary>Botón JUGAR (menú principal).</summary>
    public void PlayGame() => StartPlaying();

    /// <summary>Botón CONTINUAR (menú de pausa).</summary>
    public void ResumeGame() => StartPlaying();

    /// <summary>Botón MENÚ PRINCIPAL (menú de pausa).</summary>
    public void BackToMenu() => GoToMainMenu();

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

    void GoToMainMenu()
    {
        state = State.MainMenu;
        Show(mainMenuPanel, true);
        Show(pausePanel, false);
        Show(hud, false);
        Freeze(true);
    }

    void Pause()
    {
        state = State.Paused;
        Show(mainMenuPanel, false);
        Show(pausePanel, true);
        Show(hud, false);
        Freeze(true);
    }

    void StartPlaying()
    {
        state = State.Playing;
        Show(mainMenuPanel, false);
        Show(pausePanel, false);
        Show(hud, true);

        foreach (var script in scriptsToDisable)
            if (script) script.enabled = true;

        Time.timeScale = 1f;
        SetCursor(false);
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
