using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Puzle de las antorchas: cuando TODAS las antorchas están encendidas,
/// el Muro Final hace su animación de apertura (MuroFinalAnimacion.Abrir).
/// Va en el objeto MuroFinal (junto a MuroFinalAnimacion).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Juego/Objetos/Puzle de antorchas")]
public class PuzleAntorchas : MonoBehaviour
{
    [Tooltip("Muro que se abre. Si está vacío usa el de este mismo objeto.")]
    public MuroFinalAnimacion muro;
    [Tooltip("Antorchas que hay que encender. Si está vacío usa TODAS las antorchas de la escena.")]
    public AntorchaInteractuable[] antorchas;
    [Tooltip("Antorcha que debe encenderse de ÚLTIMA (la que está cerca del muro, para ver la apertura). " +
             "Si está vacío se usa la más cercana al muro.")]
    public AntorchaInteractuable antorchaFinal;

    [Header("Mensajes")]
    [Tooltip("{0} = encendidas, {1} = total")]
    public string mensajeProgreso = "Antorchas encendidas: {0}/{1}";
    public string mensajeCompleto = "¡El muro se está abriendo!";
    public string mensajeFinalLista = "Ahora la antorcha junto al muro de salida puede encenderse...";

    [Header("Al completar (opcional)")]
    public UnityEvent alCompletar = new UnityEvent();

    bool completado;

    void Start()
    {
        if (!muro) muro = GetComponent<MuroFinalAnimacion>();
        if (antorchas == null || antorchas.Length == 0)
            antorchas = FindObjectsByType<AntorchaInteractuable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var a in antorchas)
            if (a) a.alEncender.AddListener(AlEncenderUna);

        // La antorcha cercana al muro queda bloqueada hasta que las demás estén encendidas
        if (!antorchaFinal && muro)
        {
            float mejor = float.MaxValue;
            foreach (var a in antorchas)
            {
                if (!a) continue;
                float d = (a.transform.position - muro.transform.position).sqrMagnitude;
                if (d < mejor) { mejor = d; antorchaFinal = a; }
            }
        }
        if (antorchaFinal) antorchaFinal.bloqueada = !OtrasEncendidas();

        // Si al reintentar ya estaban todas encendidas, el muro vuelve a quedar abierto
        if (antorchas.Length > 0 && Encendidas == antorchas.Length)
        {
            completado = true;
            if (muro) muro.Abrir();
        }

        if (!muro) Debug.LogWarning("PuzleAntorchas: no hay MuroFinalAnimacion asignado.", this);
        if (antorchas.Length == 0) Debug.LogWarning("PuzleAntorchas: no encontré antorchas.", this);
    }

    void OnDestroy()
    {
        if (antorchas == null) return;
        foreach (var a in antorchas)
            if (a) a.alEncender.RemoveListener(AlEncenderUna);
    }

    public int Encendidas
    {
        get
        {
            int n = 0;
            foreach (var a in antorchas) if (a && a.EstaEncendida) n++;
            return n;
        }
    }

    bool OtrasEncendidas()
    {
        foreach (var a in antorchas)
            if (a && a != antorchaFinal && !a.EstaEncendida) return false;
        return true;
    }

    void AlEncenderUna()
    {
        if (completado) return;

        // ¿Ya están todas menos la final? → desbloquearla
        if (antorchaFinal && antorchaFinal.bloqueada && OtrasEncendidas())
        {
            antorchaFinal.bloqueada = false;
            if (!string.IsNullOrEmpty(mensajeFinalLista))
                InteractionSystem.ShowMessage(mensajeFinalLista, 4f);
            return;
        }

        int n = Encendidas, total = antorchas.Length;
        if (n < total)
        {
            if (!string.IsNullOrEmpty(mensajeProgreso))
                InteractionSystem.ShowMessage(string.Format(mensajeProgreso, n, total));
            return;
        }

        completado = true;
        if (muro) muro.Abrir();

        var s = AudioJuego.Sonidos;
        if (s)
        {
            AudioJuego.Efecto(s.puzleCorrecto);
            if (muro) AudioJuego.Efecto3D(s.accesoRocas, muro.transform.position, 1f, 60f);
        }
        if (!string.IsNullOrEmpty(mensajeCompleto)) InteractionSystem.ShowMessage(mensajeCompleto, 4f);

        alCompletar.Invoke();
    }
}
