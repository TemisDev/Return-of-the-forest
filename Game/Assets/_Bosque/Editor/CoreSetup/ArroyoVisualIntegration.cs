using System;
using System.Collections.Generic;
using Bosque.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bosque.CoreSetup
{
    public static class ArroyoVisualIntegration
    {
        private const string Art = "Assets/_Bosque/Art/";
        [MenuItem("Tools/Bosque/Prototipo/Integrar arte V2 en arroyo")]
        public static void FromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Integrate();
        }
        public static void IntegrateBatch()
        {
            try { Integrate(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void FinishLandscapeBatch()
        {
            try
            {
                ArroyoPlayableSetup.CreateOrOpen();
                var landscape = GameObject.Find("Gameplay/VisualIntegrationV3").transform;
                if (landscape.Find("ValleyBackdrop") == null)
                {
                    var root = GameObject.Find("ArroyoGreybox").transform;
                    ArroyoGameplaySetup.Find(root, "WestWall").GetComponent<Renderer>().enabled = false;
                    var backdrop = ArroyoGameplaySetup.Child(landscape, "ValleyBackdrop");
                    var rock = Model(backdrop, "Rock_01", Vector3.zero, Vector3.one, "Rock");
                    Fit(rock, new Bounds(new Vector3(24, -10, 18), new Vector3(25, 16, 65)));
                    var distant = Model(backdrop, "Rock_01", Vector3.zero, Vector3.one, "Rock");
                    Fit(distant, new Bounds(new Vector3(36, -7, 38), new Vector3(24, 18, 48)));
                    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                Debug.Log("[ArteV3] Acabado de paisaje aplicado sin alterar colisiones.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void Integrate()
        {
            ArroyoPlayableSetup.CreateOrOpen();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var root = GameObject.Find("ArroyoGreybox").transform;
            var gameplay = GameObject.Find("Gameplay").transform;
            if (gameplay.Find("VisualIntegrationV3") != null)
            {
                Debug.Log("[ArteV3] Integración existente conservada; no se sobrescribe trabajo visual.");
                return;
            }
            var physics = Snapshot<Collider>();
            var scripts = Snapshot<MonoBehaviour>();
            var guids = new Dictionary<string, string>();
            foreach (var path in AssetDatabase.FindAssets("", new[] { Art.TrimEnd('/') }))
            {
                string asset = AssetDatabase.GUIDToAssetPath(path);
                if (asset.EndsWith(".mat") || asset.EndsWith(".prefab")) guids[asset] = path;
            }
            ArroyoArtKitBuilder.Generate(); // Explicitly authorized generator; retains existing prefab GUIDs.
            foreach (var item in guids)
                if (AssetDatabase.AssetPathToGUID(item.Key) != item.Value) throw new InvalidOperationException("GUID artístico modificado: " + item.Key);
            ValidateKit();
            var degraded = ArroyoGameplaySetup.Find(root, "DegradedVisuals");
            var restored = ArroyoGameplaySetup.Find(root, "RestoredVisuals");
            HideRenderers(degraded); HideRenderers(restored);
            var legacy = gameplay.Find("ArroyoGameplayV2/KitVisualMount");
            if (legacy != null) HideRenderers(legacy);
            // Only renderer state changes. Roots, transforms, colliders and gameplay scripts remain intact.
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                if (item.name.StartsWith("Tree_") || item.name.StartsWith("Trunk_")) HideRenderers(item);
            var permanent = ArroyoGameplaySetup.Child(gameplay, "VisualIntegrationV3");
            var dry = ArroyoGameplaySetup.Child(degraded, "ArtV3");
            var alive = ArroyoGameplaySetup.Child(restored, "ArtV3");
            ArroyoGameplaySetup.Find(root, "Bank").GetComponent<Renderer>().enabled = false;
            Surface(dry, "Orilla seca", new Vector3(2.5f, -.018f, 18), new Vector3(7, .035f, 44), "Soil_Degraded");
            Surface(alive, "Orilla recuperada", new Vector3(2.5f, -.018f, 18), new Vector3(7, .035f, 44), "Soil_Restored");
            var floor = ArroyoGameplaySetup.Find(root, "ChannelFloor").GetComponent<Renderer>();
            floor.sharedMaterial = Mat("Water_Degraded");
            var water = ArroyoGameplaySetup.Find(root, "Water").GetComponent<Renderer>();
            water.sharedMaterial = Mat("Water_Restored"); water.enabled = true;
            foreach (var name in new[] { "WestWall", "NorthWall", "SouthWall" })
                ArroyoGameplaySetup.Find(root, name).GetComponent<Renderer>().sharedMaterial = Mat("Rock");
            PlantBanks(permanent, dry, alive);
            ReplaceObstacles(root);
            DressGate(gameplay);
            LightScene();
            AssertSnapshot(physics); AssertSnapshot(scripts);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[ArteV3] Integración guardada; colliders y componentes de gameplay idénticos.");
        }
        private static void PlantBanks(Transform permanent, Transform dry, Transform alive)
        {
            for (int i = 0; i < 8; i++)
            {
                float z = -2 + i * 5.7f;
                var trunkPosition = new Vector3(-5.2f - (i % 2) * .5f, 2.4f, z);
                Model(permanent, "TreeTrunk_01", trunkPosition, Vector3.one * 1.5f, "Wood");
                Model(dry, "PineCanopy_01", trunkPosition + Vector3.up * 2.1f, new Vector3(1.25f, .95f, 1.25f), "Leaf_Degraded");
                Model(alive, "PineCanopy_01", trunkPosition + Vector3.up * 2.1f, Vector3.one * 1.65f, "Leaf_Restored");
                // Faceted rock cover stays at the existing cliff, away from the walkable bank.
                Fit(Model(permanent, "Rock_01", Vector3.zero, Vector3.one, "Rock"),
                    new Bounds(new Vector3(-4.7f, 1f, z), new Vector3(1.45f, 4.2f, 6.5f)));
            }
            for (int i = 0; i < 12; i++)
            {
                float z = -1 + i * 3.4f;
                var farBank = new Vector3(-3.55f, -.5f, z);
                Model(dry, "RiverReeds_01", farBank, Vector3.one * .55f, "Leaf_Degraded");
                Model(alive, "RiverReeds_01", farBank, Vector3.one * .9f, "Leaf_Restored");
                var shrub = new Vector3(5.4f, 0, z);
                Model(dry, "ShrubCluster_01", shrub, Vector3.one * .45f, "Leaf_Degraded");
                Model(alive, "ShrubCluster_01", shrub, Vector3.one * .7f, "Leaf_Restored");
                Model(alive, "GrassClump_01", new Vector3(-.75f, 0, z + 1.5f), Vector3.one * .65f, "Leaf_Restored");
            }
        }
        private static void ReplaceObstacles(Transform level)
        {
            for (int i = 1; i <= 3; i++)
            {
                var obstacle = ArroyoGameplaySetup.Find(level, "Obstacle_0" + i);
                HideRenderers(obstacle);
                var mount = ArroyoGameplaySetup.Child(obstacle, "ArtV3VisualMount");
                if (i == 2)
                {
                    for (int n = 0; n < 3; n++)
                        Model(mount, "Rock_01", obstacle.position + new Vector3(n - 1, .15f * (n % 2), .15f * n), Vector3.one, "Rock");
                }
                else
                {
                    int count = i == 1 ? 1 : 3;
                    for (int n = 0; n < count; n++)
                    {
                        var log = Model(mount, "TreeTrunk_01", obstacle.position + new Vector3(0, .12f * n, .25f * n), Vector3.one, "Wood");
                        log.localRotation = Quaternion.Euler(0, n * 13, 90);
                    }
                }
                var bounds = obstacle.GetComponent<Collider>().bounds;
                bounds.size -= Vector3.one * .05f;
                Fit(mount, bounds);
            }
        }
        private static void DressGate(Transform gameplay)
        {
            var gate = gameplay.Find("ArroyoGameplayV2/ExitBlocker");
            HideRenderers(gate);
            var mount = ArroyoGameplaySetup.Child(gate, "ArtV3Fence");
            for (int i = 0; i < 6; i++)
                Model(mount, "TreeTrunk_01", new Vector3(-4.5f + i * 2.15f, i < 2 ? -.6f : 0, 35), new Vector3(.8f, 1.3f, .8f), "Wood");
            for (int i = 0; i < 2; i++)
            {
                var rail = Model(mount, "TreeTrunk_01", Vector3.zero, Vector3.one, "Wood");
                rail.localRotation = Quaternion.Euler(0, 0, 90);
                Fit(rail, new Bounds(new Vector3(.75f, .7f + i * .8f, 35), new Vector3(11.3f, .23f, .23f)));
            }
        }
        private static void LightScene()
        {
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) { light.color = new Color(1f, .94f, .82f); light.intensity = 1.25f; }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.63f, .71f, .73f);
            RenderSettings.ambientEquatorColor = new Color(.43f, .49f, .4f);
            RenderSettings.ambientGroundColor = new Color(.28f, .29f, .22f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.58f, .65f, .6f);
            RenderSettings.fogStartDistance = 28; RenderSettings.fogEndDistance = 100;
        }
        private static void HideRenderers(Transform root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
        private static Material Mat(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "Materials/MAT_" + name + ".mat");
            if (material == null) throw new InvalidOperationException("Material ausente: " + name);
            return material;
        }
        private static Transform Model(Transform parent, string name, Vector3 position, Vector3 scale, string material)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Prefabs/VIS_" + name + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = scale;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = Mat(material);
                if (name.Contains("Reeds") || name.Contains("Grass") || name.Contains("Shrub"))
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            return go.transform;
        }
        private static void Fit(Transform visual, Bounds target)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.localScale = Vector3.Scale(visual.localScale, new Vector3(target.size.x / bounds.size.x,
                target.size.y / bounds.size.y, target.size.z / bounds.size.z));
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.position += target.center - bounds.center;
        }
        private static void Surface(Transform parent, string name, Vector3 position, Vector3 size, string material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
        }
        internal static void ValidateKit()
        {
            string[] names = { "Rock_01", "TreeTrunk_01", "Stump_01", "PineCanopy_01", "GrassClump_01", "RiverReeds_01", "ShrubCluster_01" };
            foreach (var name in names)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Prefabs/VIS_" + name + ".prefab");
                if (prefab == null || prefab.GetComponentsInChildren<Collider>(true).Length != 0 ||
                    prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Prefab visual inválido: " + name);
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh == null) throw new InvalidOperationException("Mesh ausente: " + name);
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || material.shader.name != "Universal Render Pipeline/Lit")
                            throw new InvalidOperationException("Material no URP en " + name);
            }
        }
        private static Dictionary<UnityEngine.Object, string> Snapshot<T>() where T : UnityEngine.Object
        {
            var result = new Dictionary<UnityEngine.Object, string>();
            foreach (var item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                result[item] = EditorJsonUtility.ToJson(item);
            return result;
        }
        private static void AssertSnapshot(Dictionary<UnityEngine.Object, string> snapshot)
        {
            foreach (var item in snapshot)
                if (item.Key == null || EditorJsonUtility.ToJson(item.Key) != item.Value)
                    throw new InvalidOperationException("Cambió un componente protegido: " + item.Key);
        }
    }
}
