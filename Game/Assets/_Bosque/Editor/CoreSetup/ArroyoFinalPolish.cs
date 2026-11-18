using System;
using Bosque.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bosque.CoreSetup
{
    public static class ArroyoFinalPolish
    {
        public const string Output = "Assets/_Bosque/Scenes/Integration/ArtV4";
        private const string Art = "Assets/_Bosque/Art/V4/";
        public static void RefineFoamBatch()
        {
            try
            {
                ArroyoPlayableSetup.CreateOrOpen();
                var material = Required<Material>(Output + "/Foam.mat");
                material.shader = Shader.Find("Universal Render Pipeline/Unlit");
                material.SetColor("_BaseColor", Color.white);
                Transparent(material, .8f);
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void IntegrateBatch()
        {
            try { Integrate(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        [MenuItem("Tools/Bosque/Prototipo/Integrar acabado V4")]
        public static void FromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Integrate();
        }
        public static void Integrate()
        {
            ArroyoPlayableSetup.CreateOrOpen();
            ArroyoArtV4Builder.Generate();
            ValidateKit();
            if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets/_Bosque/Scenes/Integration", "ArtV4");
            var level = GameObject.Find("ArroyoGreybox").transform;
            var gameplay = GameObject.Find("Gameplay").transform;
            if (gameplay.Find("ArtV4Integration") != null)
            { Debug.Log("[V4] Acabado existente conservado, sin regenerar."); return; }
            var permanent = ArroyoGameplaySetup.Child(gameplay, "ArtV4Integration");
            var dry = level.Find("DegradedVisuals/ArtV3");
            var wet = level.Find("RestoredVisuals/ArtV3");
            var dryMount = ArroyoGameplaySetup.Child(dry, "ArtV4");
            var wetMount = ArroyoGameplaySetup.Child(wet, "ArtV4");
            Surface(dry.Find("Orilla seca"), "BankDry", "bank_dry_detail_v4", new Vector2(3.5f, 22));
            Surface(wet.Find("Orilla recuperada"), "BankWet", "bank_wet_detail_v4", new Vector2(3.5f, 22));
            Surface(dry.Find("CliffDry"), "CliffDry", "rock_matte_detail_v4", new Vector2(22, 2));
            Surface(wet.Find("CliffMoss"), "CliffMoss", "moss_matte_detail_v4", new Vector2(22, 2));
            foreach (var parent in new[] { dry, wet })
                foreach (var renderer in parent.GetComponentsInChildren<Renderer>(true))
                    if (renderer.name.Contains("RiverReeds")) renderer.enabled = false;
            var oldPermanent = gameplay.Find("VisualIntegrationV3");
            foreach (var renderer in oldPermanent.GetComponentsInChildren<Renderer>(true))
                if (renderer.name.Contains("Rock") && renderer.bounds.center.x < -3.8f) renderer.enabled = false;
            for (int i = 0; i < 4; i++)
            {
                var rocks = Spawn(permanent, "RiverStones", new Vector3(-4.7f, 1, 1 + i * 11), Vector3.one, Flat("Rock"));
                ArroyoVisualIntegration.Fit(rocks, new Bounds(rocks.position, new Vector3(1.45f, 4.2f, 12)));
            }
            var dryReeds = Flat("ReedsDry", "Reeds"); dryReeds.SetColor("_BaseColor", new Color(.47f, .48f, .31f));
            EditorUtility.SetDirty(dryReeds);
            for (int i = 0; i < 6; i++)
            {
                var position = new Vector3(-3.55f, -.5f, 3 + i * 6);
                Spawn(dryMount, "ReedFan", position, Vector3.one * .5f, dryReeds);
                Spawn(wetMount, "ReedFan", position, Vector3.one * .85f, Flat("Reeds"));
            }
            for (int i = 1; i <= 3; i++)
            {
                var obstacle = ArroyoGameplaySetup.Find(level, "Obstacle_0" + i);
                foreach (var renderer in obstacle.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                var visual = Spawn(obstacle, i == 2 ? "RiverStones" : "MossyBranch", obstacle.position, Vector3.one,
                    Flat(i == 2 ? "Rock" : "Branch"));
                var bounds = obstacle.GetComponent<Collider>().bounds; bounds.size -= Vector3.one * .04f;
                ArroyoVisualIntegration.Fit(visual, bounds);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            var foamMaterial = CopyMaterial("Foam", Required<Material>(Art + "Generated/Materials/MAT_V4_Foam.mat"));
            foamMaterial.shader = Shader.Find("Universal Render Pipeline/Unlit");
            foamMaterial.SetColor("_BaseColor", Color.white); Transparent(foamMaterial, .8f);
            float waterTop = ArroyoGameplaySetup.Find(level, "Water").position.y + .15f;
            for (int i = 0; i < 4; i++)
            {
                var foam = Spawn(wetMount, "FoamRibbon", new Vector3(-2.5f, waterTop + .02f, 5 + i * 9),
                    new Vector3(1.8f, 1, 1.5f), foamMaterial);
                foam.name = "V4Foam_" + i; foam.rotation = Quaternion.Euler(0, 90, 0);
            }
            var wood = CopyMaterial("ExitWood", Required<Material>(Art + "Generated/Materials/MAT_V4_Branch.mat"));
            foreach (var name in new[] { "GatePillar_W", "GatePillar_E", "GateLintel" })
                ArroyoGameplaySetup.Find(level, name).GetComponent<Renderer>().sharedMaterial = wood;
            ExitSign(wetMount);
            var hud = UnityEngine.Object.FindFirstObjectByType<ArroyoPrototypeHud>();
            hud.ConfigureArt(Texture("UI/interact_focus_v4"), Texture("UI/progress_leaf_v4"),
                Required<Texture2D>("Assets/_Bosque/Art/UI/Icons/restoration_v3.png"), Texture("UI/exit_ready_v4"));
            EditorUtility.SetDirty(hud);
            var feedback = UnityEngine.Object.FindFirstObjectByType<RestorationFeedback>();
            var particles = feedback.GetComponent<ParticleSystem>();
            var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            var soft = CopyMaterial("SoftRestoration", particleRenderer.sharedMaterial);
            soft.SetTexture("_BaseMap", Texture("VFX/restoration_soft_particle_v4")); EditorUtility.SetDirty(soft);
            particleRenderer.sharedMaterial = soft;
            var main = particles.main; main.startSize = .14f; main.startColor = new Color(.8f, .94f, .66f, .5f);
            EditorUtility.SetDirty(particles); EditorUtility.SetDirty(particleRenderer);
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional)
                { light.shadows = LightShadows.Soft; light.shadowStrength = .7f; EditorUtility.SetDirty(light); }
            foreach (var renderer in gameplay.GetComponentsInChildren<Renderer>(true))
                if (renderer.transform.IsChildOf(oldPermanent.Find("ValleyBackdrop"))) renderer.shadowCastingMode = ShadowCastingMode.Off;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[V4] Acabado integrado, sin cambios de física ni progreso.");
        }
        private static void Surface(Transform target, string name, string texture, Vector2 tiling)
        {
            var renderer = target.GetComponent<Renderer>();
            var material = CopyMaterial(name, renderer.sharedMaterial);
            material.SetTexture("_BaseMap", Texture("Textures/" + texture));
            material.SetTextureScale("_BaseMap", tiling); material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .05f); EditorUtility.SetDirty(material);
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        private static Material Flat(string name, string template = null)
        {
            var material = CopyMaterial(name, Required<Material>(Art + "Generated/Materials/MAT_V4_" + (template ?? name) + ".mat"));
            material.SetTexture("_BaseMap", null); EditorUtility.SetDirty(material);
            return material; // The provided meshes have no UV. Keep the originals intact.
        }
        private static Material CopyMaterial(string name, Material source)
        {
            string path = Output + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(source) { name = name, enableInstancing = true };
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Transparent(Material material, float opacity)
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (material.HasProperty("_BlendModePreserveSpecular")) material.SetFloat("_BlendModePreserveSpecular", 0);
            var color = material.GetColor("_BaseColor"); color.a = opacity; material.SetColor("_BaseColor", color);
            material.renderQueue = 3000; material.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(material);
        }
        private static void ExitSign(Transform parent)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "V4ExitSign";
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false); quad.transform.position = new Vector3(3, 2.15f, 38.04f);
            quad.transform.localScale = new Vector3(.65f, .65f, 1);
            string path = Output + "/ExitSign.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetTexture("_BaseMap", Texture("UI/exit_ready_v4"));
                AssetDatabase.CreateAsset(material, path);
            }
            Transparent(material, 1); quad.GetComponent<Renderer>().sharedMaterial = material;
            quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        private static Transform Spawn(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var prefab = Required<GameObject>(Art + "Generated/Prefabs/VIS_V4_" + name + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = scale;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            { renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; }
            return go.transform;
        }
        private static Texture2D Texture(string path) => Required<Texture2D>(Art + path + ".png");
        private static T Required<T>(string path) where T : UnityEngine.Object
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) throw new InvalidOperationException("Falta recurso V4: " + path);
            return value;
        }
        private static void ValidateKit()
        {
            foreach (var name in new[] { "RiverStones", "ReedFan", "MossyBranch", "FoamRibbon" })
            {
                var prefab = Required<GameObject>(Art + "Generated/Prefabs/VIS_V4_" + name + ".prefab");
                if (prefab.GetComponentsInChildren<Collider>(true).Length != 0 || prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Prefab V4 debe ser exclusivamente visual: " + name);
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh == null) throw new InvalidOperationException("Malla V4 ausente.");
            }
        }
    }
}
