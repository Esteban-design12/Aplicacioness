using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inventario básico (máximo 3 recuerdos). Ponlo en un objeto vacío "GameManager".
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public int maxSlots = 3;
    public IReadOnlyList<MemoryData> Items => items;
    public event Action Changed;

    readonly List<MemoryData> items = new List<MemoryData>();

    void Awake()
    {
        Instance = this;
        // Recuerdos ya recogidos en esta partida (antes de perder)
        foreach (var m in ProgresoPartida.Recuerdos)
            if (m != null && items.Count < maxSlots && !items.Contains(m)) items.Add(m);
    }

    public bool Add(MemoryData memory)
    {
        if (memory == null || items.Count >= maxSlots || Has(memory)) return false;
        items.Add(memory);
        ProgresoPartida.AgregarRecuerdo(memory);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(MemoryData memory)
    {
        bool removed = items.Remove(memory);
        if (removed) Changed?.Invoke();
        return removed;
    }

    public bool Has(MemoryData memory) => memory != null && items.Contains(memory);
}
