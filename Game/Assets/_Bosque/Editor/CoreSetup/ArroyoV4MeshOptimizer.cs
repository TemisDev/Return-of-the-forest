using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.CoreSetup
{
    public static class ArroyoV4MeshOptimizer
    {
        public static void OptimizeBatch()
        {
            try
            {
                ArroyoPlayableSetup.CreateOrOpen();
                foreach (var model in new[] { "ReedFan", "RiverStones" })
                {
                    var optimized = Generate(model);
                    foreach (var root in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (root.name != "VIS_V4_" + model || root.Find("V4CombinedVisual") != null) continue;
                        var renderers = root.GetComponentsInChildren<Renderer>(true);
                        var material = renderers[0].sharedMaterial;
                        var shadows = renderers[0].shadowCastingMode;
                        foreach (var renderer in renderers)
                        {
                            renderer.enabled = false;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        }
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(optimized);
                        go.name = "V4CombinedVisual"; go.transform.SetParent(root, false);
                        var combinedRenderer = go.GetComponent<Renderer>();
                        combinedRenderer.sharedMaterial = material; combinedRenderer.shadowCastingMode = shadows;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(combinedRenderer);
                    }
                }
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("[V4] Renderers fragmentados combinados sin modificar originales.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static GameObject Generate(string model)
        {
            string path = ArroyoFinalPolish.Output + "/VIS_Combined_" + model + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Bosque/Art/V4/Generated/Prefabs/VIS_V4_" + model + ".prefab");
            var filters = source.GetComponentsInChildren<MeshFilter>(true);
            var instances = new List<CombineInstance>();
            uint expectedIndices = 0;
            foreach (var filter in filters)
            {
                for (int submesh = 0; submesh < filter.sharedMesh.subMeshCount; submesh++)
                {
                    instances.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = submesh,
                        transform = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                    expectedIndices += filter.sharedMesh.GetIndexCount(submesh);
                }
            }
            var mesh = new Mesh { name = "CombinedV4_" + model };
            mesh.CombineMeshes(instances.ToArray(), true, true);
            if (mesh.GetIndexCount(0) != expectedIndices) throw new InvalidOperationException("Se perdió geometría al combinar " + model);
            AssetDatabase.CreateAsset(mesh, ArroyoFinalPolish.Output + "/Combined_" + model + ".asset");
            var go = new GameObject("VIS_Combined_" + model);
            try
            {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = source.GetComponentInChildren<Renderer>(true).sharedMaterial;
                return PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
