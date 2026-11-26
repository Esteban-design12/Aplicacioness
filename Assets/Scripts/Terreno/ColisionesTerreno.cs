using UnityEngine;

/// <summary>
/// Se pone en el objeto "Terrain".
/// Al darle Play crea colisionadores invisibles para los árboles y objetos
/// pintados en el terreno, menos los que tengan en el nombre alguna palabra
/// de la lista "sinChoque" (arbustos, flores...).
/// </summary>
[DefaultExecutionOrder(-100)] // se ejecuta antes que los demás scripts (ej. el mapa)
[RequireComponent(typeof(Terrain))]
public class ColisionesTerreno : MonoBehaviour
{
    [Tooltip("Si el nombre del objeto contiene alguna de estas palabras, se puede atravesar.")]
    public string[] sinChoque = { "bush", "shrub", "arbusto", "flower", "flor", "poppy", "grass", "fern", "plant" };

    [Tooltip("Grosor del choque en los árboles (radio del tronco).")]
    public float radioTronco = 0.4f;

    [Tooltip("Los objetos más bajos que esto (rocas, troncos caídos) usan una caja de su tamaño.")]
    public float alturaObjetoBajo = 2f;

    void Awake()
    {
        Terrain terreno = GetComponent<Terrain>();
        TerrainData datos = terreno.terrainData;
        TreePrototype[] tipos = datos.treePrototypes;

        Bounds[] medidas = new Bounds[tipos.Length];
        bool[] tieneChoque = new bool[tipos.Length];

        for (int i = 0; i < tipos.Length; i++)
        {
            GameObject modelo = tipos[i].prefab;
            if (!modelo) continue;
            tieneChoque[i] = !SeAtraviesa(modelo.name) && Medir(modelo, out medidas[i]);
            Debug.Log("Terreno: " + modelo.name + (tieneChoque[i] ? " -> CON choque" : " -> SIN choque"));
        }

        Transform grupo = new GameObject("ColisionesDelTerreno").transform;
        int creados = 0;

        foreach (TreeInstance arbol in datos.treeInstances)
        {
            int i = arbol.prototypeIndex;
            if (!tieneChoque[i]) continue;

            Transform modelo = tipos[i].prefab.transform;
            GameObject objeto = new GameObject(modelo.name);
            objeto.layer = gameObject.layer;
            objeto.transform.SetParent(grupo);
            objeto.transform.position = Vector3.Scale(arbol.position, datos.size) + terreno.transform.position;
            objeto.transform.rotation = Quaternion.Euler(0f, arbol.rotation * Mathf.Rad2Deg, 0f);

            Vector3 escala = Vector3.Scale(modelo.localScale,
                new Vector3(arbol.widthScale, arbol.heightScale, arbol.widthScale));
            objeto.transform.localScale = escala;

            Bounds b = medidas[i];
            if (b.size.y * escala.y < alturaObjetoBajo)
            {
                BoxCollider caja = objeto.AddComponent<BoxCollider>();
                caja.center = b.center;
                caja.size = b.size;
            }
            else
            {
                // Solo el tronco, para no chocar con las ramas
                float anchoModelo = Mathf.Max(0.001f, Mathf.Abs(modelo.localScale.x));
                float altoEscala = Mathf.Max(0.001f, Mathf.Abs(escala.y));
                CapsuleCollider tronco = objeto.AddComponent<CapsuleCollider>();
                tronco.radius = radioTronco / anchoModelo;
                tronco.height = 4f / altoEscala;
                tronco.center = new Vector3(0f, 1.5f / altoEscala, 0f);
            }
            creados++;
        }

        Debug.Log("Terreno: se crearon " + creados + " colisionadores.");
    }

    bool SeAtraviesa(string nombre)
    {
        string n = nombre.ToLower();
        foreach (string palabra in sinChoque)
            if (!string.IsNullOrEmpty(palabra) && n.Contains(palabra.ToLower())) return true;
        return false;
    }

    /// <summary>Calcula el tamaño del modelo juntando todas sus piezas.</summary>
    bool Medir(GameObject modelo, out Bounds total)
    {
        total = default;
        bool hay = false;
        foreach (MeshFilter pieza in modelo.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!pieza.sharedMesh) continue;
            Matrix4x4 m = modelo.transform.worldToLocalMatrix * pieza.transform.localToWorldMatrix;
            Bounds b = pieza.sharedMesh.bounds;

            for (int e = 0; e < 8; e++) // las 8 esquinas de la pieza
            {
                Vector3 signo = new Vector3((e & 1) == 0 ? -1 : 1, (e & 2) == 0 ? -1 : 1, (e & 4) == 0 ? -1 : 1);
                Vector3 punto = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, signo));
                if (!hay) { total = new Bounds(punto, Vector3.zero); hay = true; }
                else total.Encapsulate(punto);
            }
        }
        return hay;
    }
}
