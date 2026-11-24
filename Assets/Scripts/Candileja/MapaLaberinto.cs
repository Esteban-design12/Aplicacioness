using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mapa en cuadrícula del laberinto, calculado al iniciar el juego a partir de los
/// muros (hijos del objeto "Laberinto"). Sirve para buscar caminos por los pasillos (A*)
/// sin necesidad de hornear un NavMesh.
/// </summary>
public class MapaLaberinto
{
    static MapaLaberinto cache;
    static Transform cacheRaiz;

    public readonly Transform raiz;
    public readonly float tam;
    public readonly int ancho, alto;
    public readonly Vector3 origen;   // esquina mínima (x, y del suelo, z)
    readonly bool[] libre;

    /// <summary>Devuelve el mapa (se calcula una sola vez y lo comparten todas las Candilejas).</summary>
    public static MapaLaberinto Obtener(Transform raiz, float tamCelda, float radio)
    {
        if (!raiz) return null;
        if (cache != null && cacheRaiz == raiz) return cache;
        cache = new MapaLaberinto(raiz, tamCelda, radio);
        cacheRaiz = raiz;
        return cache.ancho > 0 ? cache : null;
    }

    MapaLaberinto(Transform raiz, float tamCelda, float radio)
    {
        this.raiz = raiz;
        tam = Mathf.Max(0.25f, tamCelda);

        // Límites del laberinto = todos sus muros
        bool hay = false;
        Bounds b = default;
        foreach (var c in raiz.GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger || c.bounds.size.y < 0.3f) continue;
            if (!hay) { b = c.bounds; hay = true; } else b.Encapsulate(c.bounds);
        }
        if (!hay) { Debug.LogWarning("MapaLaberinto: el objeto Laberinto no tiene muros con collider."); return; }

        origen = new Vector3(b.min.x, raiz.position.y, b.min.z);
        ancho = Mathf.CeilToInt(b.size.x / tam);
        alto = Mathf.CeilToInt(b.size.z / tam);
        libre = new bool[ancho * alto];

        var buffer = new Collider[32];
        var mitad = new Vector3(radio, 0.8f, radio);
        int libres = 0;
        for (int z = 0; z < alto; z++)
        for (int x = 0; x < ancho; x++)
        {
            Vector3 c = Centro(x, z) + Vector3.up * 1.1f; // de 0.3 m a 1.9 m sobre el suelo
            int n = Physics.OverlapBoxNonAlloc(c, mitad, buffer, Quaternion.identity, ~0,
                                               QueryTriggerInteraction.Ignore);
            bool ok = true;
            for (int i = 0; i < n; i++)
                if (buffer[i].transform.IsChildOf(raiz)) { ok = false; break; }
            libre[x + z * ancho] = ok;
            if (ok) libres++;
        }
        Debug.Log($"MapaLaberinto: {ancho}x{alto} celdas de {tam} m, {libres} libres.");
    }

    // ---------------- Consultas ----------------
    public Vector3 Centro(int x, int z) =>
        new Vector3(origen.x + (x + 0.5f) * tam, origen.y, origen.z + (z + 0.5f) * tam);

    public bool EnRango(int x, int z) => x >= 0 && z >= 0 && x < ancho && z < alto;
    public bool Libre(int x, int z) => EnRango(x, z) && libre[x + z * ancho];

    public void Celda(Vector3 p, out int x, out int z)
    {
        x = Mathf.FloorToInt((p.x - origen.x) / tam);
        z = Mathf.FloorToInt((p.z - origen.z) / tam);
    }

    /// <summary>¿El punto está dentro del laberinto (en un pasillo)?</summary>
    public bool Dentro(Vector3 p)
    {
        Celda(p, out int x, out int z);
        if (!EnRango(x, z)) return false;
        return LibreCercana(x, z, 2, out _, out _);
    }

    /// <summary>Celda libre más cercana (busca en anillos hasta 'radio' celdas).</summary>
    public bool LibreCercana(int x, int z, int radio, out int rx, out int rz)
    {
        rx = x; rz = z;
        if (Libre(x, z)) return true;
        for (int r = 1; r <= radio; r++)
            for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;
                if (Libre(x + dx, z + dz)) { rx = x + dx; rz = z + dz; return true; }
            }
        return false;
    }

    public Vector3 PuntoAleatorioLibre()
    {
        for (int i = 0; i < 200; i++)
        {
            int x = Random.Range(0, ancho), z = Random.Range(0, alto);
            if (Libre(x, z)) return Centro(x, z);
        }
        return Centro(ancho / 2, alto / 2);
    }

    // ---------------- A* ----------------
    static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    static readonly int[] DZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

    float[] g; int[] padre; int[] visita; int[] cerrado; int marca;
    readonly List<(float f, int celda)> heap = new List<(float, int)>();

    /// <summary>Camino por los pasillos (lista de puntos). Vacío si no hay camino.</summary>
    public List<Vector3> Camino(Vector3 desde, Vector3 hasta, int maxNodos = 40000)
    {
        var res = new List<Vector3>();
        Celda(desde, out int sx, out int sz);
        Celda(hasta, out int tx, out int tz);
        if (!LibreCercana(sx, sz, 4, out sx, out sz)) return res;
        if (!LibreCercana(tx, tz, 4, out tx, out tz)) return res;

        int total = ancho * alto;
        if (g == null || g.Length != total)
        {
            g = new float[total]; padre = new int[total]; visita = new int[total]; cerrado = new int[total];
        }
        marca++;
        if (marca == int.MaxValue)
        {
            System.Array.Clear(visita, 0, total); System.Array.Clear(cerrado, 0, total); marca = 1;
        }

        int inicio = sx + sz * ancho, meta = tx + tz * ancho;
        heap.Clear();
        Abrir(inicio, 0f, -1, tx, tz);

        int nodos = 0;
        bool encontrado = false;
        while (heap.Count > 0 && nodos++ < maxNodos)
        {
            int actual = Sacar();
            if (cerrado[actual] == marca) continue;   // entrada vieja
            cerrado[actual] = marca;
            if (actual == meta) { encontrado = true; break; }
            int cx = actual % ancho, cz = actual / ancho;

            for (int d = 0; d < 8; d++)
            {
                int nx = cx + DX[d], nz = cz + DZ[d];
                if (!Libre(nx, nz)) continue;
                if (d >= 4 && (!Libre(cx + DX[d], cz) || !Libre(cx, cz + DZ[d]))) continue; // sin cortar esquinas
                int vecino = nx + nz * ancho;
                if (cerrado[vecino] == marca) continue;
                float costo = g[actual] + (d >= 4 ? 1.4142f : 1f);
                if (visita[vecino] == marca && costo >= g[vecino]) continue;
                Abrir(vecino, costo, actual, tx, tz);
            }
        }
        if (!encontrado) return res;

        // Reconstruir y suavizar (quitar puntos innecesarios en línea recta)
        var celdas = new List<int>();
        for (int c = meta; c != -1; c = padre[c]) celdas.Add(c);
        celdas.Reverse();

        int i = 0;
        res.Add(Centro(celdas[0] % ancho, celdas[0] / ancho));
        while (i < celdas.Count - 1)
        {
            int j = Mathf.Min(celdas.Count - 1, i + 40);
            while (j > i + 1 && !LineaLibre(celdas[i], celdas[j])) j--;
            res.Add(Centro(celdas[j] % ancho, celdas[j] / ancho));
            i = j;
        }
        return res;
    }

    void Abrir(int celda, float costo, int de, int tx, int tz)
    {
        g[celda] = costo;
        padre[celda] = de;
        visita[celda] = marca;
        int x = celda % ancho, z = celda / ancho;
        int ax = Mathf.Abs(x - tx), az = Mathf.Abs(z - tz);
        float f = costo + (ax + az) + (1.4142f - 2f) * Mathf.Min(ax, az); // octil
        heap.Add((f, celda));
        Subir(heap.Count - 1);
    }

    int Sacar()
    {
        int top = heap[0].celda;
        var last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        if (heap.Count > 0) { heap[0] = last; Bajar(0); }
        return top;
    }

    void Subir(int i)
    {
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (heap[p].f <= heap[i].f) break;
            (heap[p], heap[i]) = (heap[i], heap[p]);
            i = p;
        }
    }

    void Bajar(int i)
    {
        int n = heap.Count;
        while (true)
        {
            int l = i * 2 + 1, r = l + 1, m = i;
            if (l < n && heap[l].f < heap[m].f) m = l;
            if (r < n && heap[r].f < heap[m].f) m = r;
            if (m == i) break;
            (heap[m], heap[i]) = (heap[i], heap[m]);
            i = m;
        }
    }

    bool LineaLibre(int a, int b)
    {
        Vector3 pa = Centro(a % ancho, a / ancho), pb = Centro(b % ancho, b / ancho);
        float dist = Vector3.Distance(pa, pb);
        int pasos = Mathf.CeilToInt(dist / (tam * 0.25f));
        for (int s = 1; s < pasos; s++)
        {
            Vector3 p = Vector3.Lerp(pa, pb, s / (float)pasos);
            Celda(p, out int x, out int z);
            if (!Libre(x, z)) return false;
        }
        return true;
    }

    /// <summary>¿Hay línea recta libre entre dos puntos del mapa?</summary>
    public bool LineaLibre(Vector3 a, Vector3 b)
    {
        Celda(a, out int ax, out int az);
        Celda(b, out int bx, out int bz);
        if (!EnRango(ax, az) || !EnRango(bx, bz)) return false;
        return LineaLibre(ax + az * ancho, bx + bz * ancho);
    }
}
