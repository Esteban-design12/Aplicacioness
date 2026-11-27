using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 1) Mantiene Build Settings con las escenas del juego en orden.
/// 2) Hace que el botón Play del editor SIEMPRE arranque en el Menú Principal.
/// Debe estar dentro de una carpeta llamada "Editor". No se pone en ningún objeto.
/// </summary>
[InitializeOnLoad]
public static class IniciarMenu
{
    // Orden de Build Settings. La primera es la que arranca el juego.
    static readonly string[] orden =
    {
        "MenuPrincipal", "Previo", "Tutorial", "Candileja",
        "Final", "Opciones", "Creditos", "Lose", "Win"
    };

    static IniciarMenu()
    {
        // Esperar a que Unity termine de cargar los assets
        EditorApplication.delayCall += Configurar;
    }

    static void Configurar()
    {
        var lista = new List<EditorBuildSettingsScene>();

        foreach (var nombre in orden)
        {
            string ruta = "Assets/Scenes/" + nombre + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) != null)
                lista.Add(new EditorBuildSettingsScene(ruta, true));
            else
                Debug.LogWarning("IniciarMenu: no encontré la escena " + ruta);
        }

        // Conservar otras escenas activas que se hayan agregado a mano
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled || lista.Exists(x => x.path == s.path)) continue;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) != null)
                lista.Add(s);
        }

        // Solo reescribir si cambió algo
        var actual = EditorBuildSettings.scenes;
        bool igual = actual.Length == lista.Count;
        for (int i = 0; igual && i < lista.Count; i++)
            igual = actual[i].path == lista[i].path && actual[i].enabled;
        if (!igual)
        {
            EditorBuildSettings.scenes = lista.ToArray();
            Debug.Log("IniciarMenu: Build Settings actualizado (" + lista.Count + " escenas).");
        }

        if (lista.Count > 0)
        {
            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(lista[0].path);
            Debug.Log("IniciarMenu: Play arrancará en " + lista[0].path);
        }
    }
}
