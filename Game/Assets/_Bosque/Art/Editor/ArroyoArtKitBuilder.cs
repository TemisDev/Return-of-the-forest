#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor-only visual-kit builder. Does not open or mutate scenes.
public static class ArroyoArtKitBuilder
{
    private const string Root = "Assets/_Bosque/Art";
    private const string PrototypeModels = Root + "/PrototypeKit/Models";
    private const string NewModels = Root + "/Environment/Models";
    private const string MaterialsPath = Root + "/Materials";
    private const string PrefabsPath = Root + "/Prefabs";

    private struct VisualAsset
    {
        public string name, source, material;
        public VisualAsset(string n, string s, string m) { name=n; source=s; material=m; }
    }

    [MenuItem("Tools/Bosque/Arte/Generar kit visual V2")]
    public static void Generate()
    {
        EnsureFolder(MaterialsPath);
        EnsureFolder(PrefabsPath);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[Bosque Arte V2] No se encontró URP/Lit. Abrir el proyecto Unity URP y revisar paquetes.");
            return;
        }

        var palette = new Dictionary<string, string>
        {
            { "Soil_Degraded", "#786F5C" }, { "Soil_Restored", "#6C8156" },
            { "Leaf_Degraded", "#777A4F" }, { "Leaf_Restored", "#477B58" },
            { "Water_Degraded", "#68736C" }, { "Water_Restored", "#4A93A1" },
            { "Rock", "#8B9089" }, { "Wood", "#826346" }
        };
        foreach (var pair in palette)
        {
            var path = MaterialsPath + "/MAT_" + pair.Key + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            // Only the color and shader of named generated materials are managed by this tool.
            mat.shader = shader;
            if (ColorUtility.TryParseHtmlString(pair.Value, out Color color))
                mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.08f);
            EditorUtility.SetDirty(mat);
        }

        var sources = new VisualAsset[]
        {
            new VisualAsset("Rock_01", PrototypeModels + "/Rock_01.obj", "Rock"),
            new VisualAsset("TreeTrunk_01", PrototypeModels + "/TreeTrunk_01.obj", "Wood"),
            new VisualAsset("Stump_01", PrototypeModels + "/Stump_01.obj", "Wood"),
            new VisualAsset("PineCanopy_01", PrototypeModels + "/PineCanopy_01.obj", "Leaf_Restored"),
            new VisualAsset("GrassClump_01", PrototypeModels + "/GrassClump_01.obj", "Leaf_Restored"),
            new VisualAsset("RiverReeds_01", NewModels + "/RiverReeds_01.obj", "Leaf_Restored"),
            new VisualAsset("ShrubCluster_01", NewModels + "/ShrubCluster_01.obj", "Leaf_Restored")
        };
        int count = 0;
        foreach (var item in sources)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(item.source);
            if (source == null)
            {
                Debug.LogWarning("[Bosque Arte V2] Modelo no disponible: " + item.source);
                continue;
            }
            var prefabPath = PrefabsPath + "/VIS_" + item.name + ".prefab";
            // Avoid destructive updates to prefabs somebody already customized.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log("[Bosque Arte V2] Prefab existente conservado: " + prefabPath);
                continue;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                instance.name = "VIS_" + item.name;
                var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/MAT_" + item.material + ".mat");
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = mat;
                // These prefabs are visual only: never change obstacle gameplay colliders.
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                count++;
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bosque Arte V2] Kit generado. Prefabs nuevos: " + count + ". Comprobar Console y escena de prueba.");
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (int i=1; i<parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
