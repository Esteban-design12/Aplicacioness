using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menú "Candileja > Construir para Windows": genera el juego para PC (.exe)
/// en la carpeta Builds/Windows del proyecto, con todas las escenas activas de Build Settings.
/// </summary>
[InitializeOnLoad]
public static class ConstruirJuego
{
    const string Carpeta = "Builds/Windows";
    const string Ejecutable = "LaCandileja.exe";
    const string Aviso = "Builds/construir.txt"; // si existe, se compila solo al recargar Unity

    static ConstruirJuego()
    {
        if (!File.Exists(Aviso)) return;
        File.Delete(Aviso);
        EditorApplication.delayCall += ConstruirWindows;
    }

    [MenuItem("Candileja/Construir para Windows")]
    public static void ConstruirWindows()
    {
        var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (escenas.Length == 0)
        {
            EditorUtility.DisplayDialog("Construir", "No hay escenas activas en Build Settings.", "OK");
            return;
        }

        Directory.CreateDirectory(Carpeta);
        var opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = Path.Combine(Carpeta, Ejecutable),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        Debug.Log("[Construir] Escenas: " + string.Join(", ", escenas));
        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        var r = reporte.summary;

        if (r.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Construir] LISTO: {Path.GetFullPath(opciones.locationPathName)} " +
                      $"({r.totalSize / (1024 * 1024)} MB, {r.totalTime.TotalSeconds:0} s)");
            if (!Application.isBatchMode && !SessionState.GetBool("CandilejaSinExplorador", false))
                EditorUtility.RevealInFinder(Path.GetFullPath(opciones.locationPathName));
        }
        else
        {
            Debug.LogError($"[Construir] FALLÓ: {r.result}, {r.totalErrors} errores. Revisa la Console.");
        }
    }
}
