using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Hace que el botón Play del editor SIEMPRE arranque en la primera escena
/// ACTIVA (con check) de Build Settings, sin importar qué escena estés editando.
/// Debe estar dentro de una carpeta llamada "Editor". No se pone en ningún objeto.
/// </summary>
[InitializeOnLoad]
public static class IniciarMenu
{
    static IniciarMenu()
    {
        foreach (var escena in EditorBuildSettings.scenes)
        {
            if (!escena.enabled) continue; // salta escenas desmarcadas (ej. SampleScene)

            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(escena.path);
            if (asset == null) continue;   // salta escenas que ya no existen

            EditorSceneManager.playModeStartScene = asset;
            Debug.Log("IniciarMenu: Play arrancará en " + escena.path);
            return;
        }

        EditorSceneManager.playModeStartScene = null;
        Debug.LogWarning("IniciarMenu: no hay escenas activas en Build Settings.");
    }
}
