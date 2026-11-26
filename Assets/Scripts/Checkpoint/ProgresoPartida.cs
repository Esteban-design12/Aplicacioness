using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Progreso de la partida actual. Sobrevive al recargar la escena (por ejemplo al
/// darle "Reintentar" en Lose), pero se borra al empezar una partida nueva desde el menú
/// o al ganar.
///  - Último checkpoint tocado.
///  - Antorchas encendidas.
///  - Recuerdos recogidos.
/// </summary>
public static class ProgresoPartida
{
    public static bool HayCheckpoint { get; private set; }
    public static Vector3 PosicionCheckpoint { get; private set; }
    public static Quaternion RotacionCheckpoint { get; private set; }

    static readonly HashSet<string> antorchas = new HashSet<string>();
    static readonly List<MemoryData> recuerdos = new List<MemoryData>();

    public static IReadOnlyList<MemoryData> Recuerdos => recuerdos;

    // Al darle Play en el editor siempre se empieza de cero
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void AlIniciarJuego() => Reiniciar();

    /// <summary>Borra todo (partida nueva).</summary>
    public static void Reiniciar()
    {
        HayCheckpoint = false;
        antorchas.Clear();
        recuerdos.Clear();
    }

    public static void GuardarCheckpoint(Vector3 posicion, Quaternion rotacion)
    {
        HayCheckpoint = true;
        PosicionCheckpoint = posicion;
        RotacionCheckpoint = rotacion;
    }

    // ---------- Antorchas ----------
    public static string IdDe(Component c)
    {
        Vector3 p = c.transform.position;
        return $"{c.gameObject.scene.name}/{c.name}@{Mathf.RoundToInt(p.x)},{Mathf.RoundToInt(p.z)}";
    }

    public static void MarcarAntorcha(Component antorcha) => antorchas.Add(IdDe(antorcha));
    public static bool AntorchaEncendida(Component antorcha) => antorchas.Contains(IdDe(antorcha));

    // ---------- Recuerdos ----------
    public static void AgregarRecuerdo(MemoryData m)
    {
        if (m != null && !recuerdos.Contains(m)) recuerdos.Add(m);
    }

    public static bool TieneRecuerdo(MemoryData m) => m != null && recuerdos.Contains(m);
}
