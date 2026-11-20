using System;
using UnityEditor;
using UnityEngine;

namespace Bosque.CoreSetup
{
    internal static class ArroyoArtSetup
    {
        private const string Kit = "Assets/_Bosque/Art/PrototypeKit/";
        private const string Output = "Assets/_Bosque/Scenes/Integration/Visuals";
        internal static Material Material(string textureName)
        {
            if (!AssetDatabase.IsValidFolder(Output))
                AssetDatabase.CreateFolder("Assets/_Bosque/Scenes/Integration", "Visuals");
            string path = Output + "/" + textureName + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "Textures/" + textureName + ".png");
            if (texture == null) throw new InvalidOperationException("Falta textura artística: " + textureName);
            result = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            result.SetTexture("_BaseMap", texture); result.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(result, path); return result;
        }
        internal static void Attach(Transform level, Transform gameplay)
        {
            if (gameplay.Find("KitVisualMount") != null) return;
            var permanent = ArroyoGameplaySetup.Child(gameplay, "KitVisualMount");
            var degraded = ArroyoGameplaySetup.Child(ArroyoGameplaySetup.Find(level, "DegradedVisuals"), "KitVisualMount");
            var restored = ArroyoGameplaySetup.Child(ArroyoGameplaySetup.Find(level, "RestoredVisuals"), "KitVisualMount");
            for (int i = 0; i < 6; i++)
            {
                var position = new Vector3(i % 2 == 0 ? 5.2f : -.4f, 0, 4 + i * 5);
                Model(permanent, "TreeTrunk_01", position, Vector3.one, Material("wood"));
                Model(degraded, "Stump_01", position + Vector3.up * 1.6f, Vector3.one * .6f, Material("leaf_degraded"));
                Model(restored, "PineCanopy_01", position + Vector3.up * 1.4f, Vector3.one * 1.1f, Material("leaf_restored"));
                Model(restored, "GrassClump_01", position + Vector3.right * .5f, Vector3.one, Material("leaf_restored"));
            }
            Model(permanent, "Rock_01", new Vector3(5.1f, 0, 10), Vector3.one, Material("rock"));
            ArroyoGameplaySetup.Find(level, "Water").GetComponent<Renderer>().sharedMaterial = Material("water_restored");
        }
        private static void Model(Transform parent, string model, Vector3 position, Vector3 scale, Material material)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Models/" + model + ".obj");
            if (source == null) throw new InvalidOperationException("Falta modelo artístico: " + model);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = model; instance.transform.SetParent(parent, false);
            instance.transform.position = position; instance.transform.localScale = scale;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
        }
    }
}
