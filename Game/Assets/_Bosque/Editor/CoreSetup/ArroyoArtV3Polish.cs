using System;
using Bosque.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bosque.CoreSetup
{
    public static class ArroyoArtV3Polish
    {
        public const string Output = "Assets/_Bosque/Scenes/Integration/ArtV3";
        private const string Art = "Assets/_Bosque/Art/";
        public static void IntegrateBatch()
        {
            try { Integrate(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        [MenuItem("Tools/Bosque/Prototipo/Integrar recursos Arte V3 y HUD")]
        public static void FromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Integrate();
        }
        public static void Integrate()
        {
            ArroyoPlayableSetup.CreateOrOpen();
            if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets/_Bosque/Scenes/Integration", "ArtV3");
            GeneratePrefabs();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var root = GameObject.Find("ArroyoGreybox").transform;
            var session = UnityEngine.Object.FindFirstObjectByType<ArroyoSession>();
            var hud = UnityEngine.Object.FindFirstObjectByType<ArroyoPrototypeHud>();
            hud.ConfigureArt(Icon("interact_e"), Icon("progress_leaf"), Icon("restoration"), Icon("exit_unlocked"));
            EditorUtility.SetDirty(hud);
            var permanent = GameObject.Find("Gameplay/VisualIntegrationV3").transform;
            var dry = FindPath(root, "DegradedVisuals/ArtV3");
            var alive = FindPath(root, "RestoredVisuals/ArtV3");
            var water = GetMaterial("WaterRipples", Color.white, "water_ripple_mask_v3");
            water.SetTextureScale("_BaseMap", new Vector2(1.5f, 22)); EditorUtility.SetDirty(water);
            ArroyoGameplaySetup.Find(root, "Water").GetComponent<Renderer>().sharedMaterial = water;
            BackCliff(dry, "CliffDry", "riverbank_dry_v3");
            BackCliff(alive, "CliffMoss", "stream_moss_v3");
            if (permanent.Find("ArtV3Polish") == null)
            {
                var mount = ArroyoGameplaySetup.Child(permanent, "ArtV3Polish");
                var foliage = ArroyoGameplaySetup.Child(alive, "PolishV3Ferns");
                ApplySurface(dry, "Orilla seca", "riverbank_dry_v3", "SoilDry", new Vector2(3.5f, 22));
                ApplySurface(alive, "Orilla recuperada", "riverbank_restored_v3", "SoilRestored", new Vector2(3.5f, 22));
                for (int i = 0; i < 8; i++)
                {
                    Instance(foliage, "FernCluster_V3", new Vector3(5.25f, 0, 1 + i * 4.7f), Vector3.one * .7f);
                    Instance(mount, "RockCluster_V3", new Vector3(-4.5f, 2.6f, 3 + i * 5), new Vector3(1.3f, 1, 1.3f));
                }
                foreach (int i in new[] { 1, 2, 3 })
                {
                    var obstacle = ArroyoGameplaySetup.Find(root, "Obstacle_0" + i);
                    var old = obstacle.Find("ArtV3VisualMount");
                    foreach (var renderer in old.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                    var model = Instance(obstacle, i == 2 ? "RockCluster_V3" : "Driftwood_V3", obstacle.position, Vector3.one);
                    Fit(model, obstacle.GetComponent<Collider>().bounds);
                }
                var feedbackObject = new GameObject("RestorationFeedback"); feedbackObject.transform.SetParent(mount, false);
                var particles = feedbackObject.AddComponent<ParticleSystem>();
                ConfigureParticles(particles);
                feedbackObject.AddComponent<RestorationFeedback>().Configure(session, particles);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[PolishV3] Prefabs, texturas, iconos y restauración visual integrados.");
        }
        private static Transform FindPath(Transform root, string path)
        {
            var found = root.Find(path);
            if (found == null) throw new InvalidOperationException("Falta montaje: " + path);
            return found;
        }
        private static void BackCliff(Transform parent, string name, string texture)
        {
            if (parent.Find(name) != null) return;
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube); backing.name = name;
            UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());
            backing.transform.SetParent(parent, false);
            backing.transform.position = new Vector3(-5.55f, 1, 18);
            backing.transform.localScale = new Vector3(.12f, 4, 44);
            var mat = GetMaterial(name, new Color(.74f, .78f, .72f), texture);
            mat.SetTextureScale("_BaseMap", new Vector2(22, 2)); EditorUtility.SetDirty(mat);
            backing.GetComponent<Renderer>().sharedMaterial = mat;
        }
        private static void ApplySurface(Transform parent, string name, string texture, string material, Vector2 tiling)
        {
            var target = FindPath(parent, name).GetComponent<Renderer>();
            var mat = GetMaterial(material, new Color(.9f, .92f, .87f), texture);
            mat.SetTextureScale("_BaseMap", tiling); EditorUtility.SetDirty(mat);
            target.sharedMaterial = mat;
        }
        private static Texture2D Icon(string name) => Required<Texture2D>(Art + "UI/Icons/" + name + "_v3.png");
        private static T Required<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Falta recurso: " + path);
            return asset;
        }
        private static Material GetMaterial(string name, Color color, string texture = null)
        {
            string path = Output + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .07f);
            if (texture != null) material.SetTexture("_BaseMap", Required<Texture2D>(Art + "Environment/Textures/" + texture + ".png"));
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void GeneratePrefabs()
        {
            foreach (var name in new[] { "RockCluster_V3", "FernCluster_V3", "Driftwood_V3" })
            {
                string path = Output + "/VIS_" + name + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
                var source = Required<GameObject>(Art + "Environment/Models/" + name + ".obj");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                try
                {
                    var color = name.StartsWith("Rock") ? new Color(.46f, .49f, .43f) :
                        name.StartsWith("Fern") ? new Color(.34f, .53f, .29f) : new Color(.44f, .29f, .17f);
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = GetMaterial(name, color);
                    if (instance.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Collider en mesh artístico.");
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
        }
        private static Transform Instance(Transform parent, string model, Vector3 position, Vector3 scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(Output + "/VIS_" + model + ".prefab"));
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = scale;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }
        private static void Fit(Transform visual, Bounds box)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var size = box.size - Vector3.one * .04f;
            visual.localScale = Vector3.Scale(visual.localScale, new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z));
            bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.position += box.center - bounds.center;
        }
        private static void ConfigureParticles(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.playOnAwake = false; main.loop = false; main.duration = .8f;
            main.startLifetime = .85f; main.startSpeed = .45f; main.startSize = .11f; main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.8f, .95f, .55f, .7f);
            var emission = particles.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 20) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1.1f;
            var colors = particles.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(.75f, 0), new GradientAlphaKey(0, 1) });
            colors.color = gradient;
            string path = Output + "/RestorationParticles.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                mat.SetTexture("_BaseMap", Required<Texture2D>(Art + "VFX/Textures/restoration_particles_v3.png"));
                mat.SetFloat("_Surface", 1); mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); mat.SetFloat("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = 3000;
                AssetDatabase.CreateAsset(mat, path);
            }
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        }
    }
}
