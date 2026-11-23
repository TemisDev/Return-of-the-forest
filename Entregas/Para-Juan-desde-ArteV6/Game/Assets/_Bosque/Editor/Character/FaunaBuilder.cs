using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bosque.CharacterEditor
{
    /// <summary>
    /// Construye el "zorro de neblina" (especie ficticia inspirada en el zorro andino o culpeo) a partir de
    /// Docs/Fauna/pipeline/preparar_zorro.py: importación Generic a escala real, material URP, controlador con
    /// reposo/paso/carrera por velocidad y prefab. Idempotente.
    /// </summary>
    public static class FaunaBuilder
    {
        public const string Root = "Assets/_Bosque/Characters/Fauna/ZorroNeblina";
        public const string ModelFbx = Root + "/Source/SK_ZorroNeblina.fbx";
        public const string Albedo = Root + "/Textures/T_ZorroNeblina_BaseColor.png";
        public const string MaterialPath = Root + "/Materials/M_ZorroNeblina.mat";
        public const string ControllerPath = Root + "/Animation/AC_ZorroNeblina.controller";
        public const string PrefabPath = Root + "/Prefabs/PF_ZorroNeblina.prefab";
        /// <summary>Largo del hocico a la punta de la cola (m): animal pequeño, algo menor que un culpeo.</summary>
        public const float TargetLength = .95f;
        /// <summary>Velocidad de avance que corresponde al ciclo de paso a esta escala (m/s).</summary>
        public const float WalkClipSpeed = 1.35f, RunClipSpeed = 3.6f;

        [MenuItem("Tools/Bosque/Fauna/Construir zorro de neblina")]
        public static GameObject Build()
        {
            foreach (var d in new[] { "Materials", "Animation", "Prefabs" }) System.IO.Directory.CreateDirectory($"{Root}/{d}");
            var ti = (TextureImporter)AssetImporter.GetAtPath(Albedo);
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.mipmapEnabled = true;
            ti.SaveAndReimport();

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_ZorroNeblina" }; AssetDatabase.CreateAsset(mat, MaterialPath); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Albedo)); mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Smoothness", .12f); mat.SetFloat("_EnvironmentReflections", 0f);
            EditorUtility.SetDirty(mat);

            var mi = (ModelImporter)AssetImporter.GetAtPath(ModelFbx);
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = true; mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_ZorroNeblina"), mat);
            mi.globalScale = 1f; mi.useFileScale = true;
            mi.SaveAndReimport();
            // Escala real: se mide el modelo importado y se ajusta a TargetLength.
            float length = MeasureLength();
            if (length <= 0f) throw new Exception("No se pudo medir el zorro.");
            mi.globalScale = TargetLength / length;
            mi.clipAnimations = mi.defaultClipAnimations.Select(c =>
            {
                c.name = c.takeName.Split('|').Last();
                c.loopTime = true; c.loopPose = false;
                c.lockRootRotation = true; c.lockRootHeightY = false; c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true; c.keepOriginalPositionXZ = true; c.keepOriginalPositionY = true;
                return c;
            }).ToArray();
            mi.SaveAndReimport();
            Debug.Log($"[Fauna] Zorro importado: largo original {length:F2}, escala {mi.globalScale:F5}, clips {string.Join(", ", mi.clipAnimations.Select(c => c.name))}.");

            var ctrl = Controller();
            return Prefab(ctrl);
        }

        private static float MeasureLength()
        {
            var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbx));
            try
            {
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr == null) return 0f;
                var b = smr.bounds; return Mathf.Max(b.size.x, b.size.z);
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static AnimationClip Clip(string name) =>
            AssetDatabase.LoadAllAssetsAtPath(ModelFbx).OfType<AnimationClip>().FirstOrDefault(c => c.name == name && !c.name.StartsWith("__preview"))
            ?? throw new Exception("Falta el clip del zorro " + name);

        private static AnimatorController Controller()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var sm = ac.layers[0].stateMachine;
            var tree = new BlendTree { name = "Locomocion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(Clip("Survey"), 0f);
            tree.AddChild(Clip("Walk"), WalkClipSpeed);
            tree.AddChild(Clip("Run"), RunClipSpeed);
            var loco = sm.AddState("Locomocion"); loco.motion = tree; sm.defaultState = loco;
            EditorUtility.SetDirty(ac);
            return ac;
        }

        private static GameObject Prefab(AnimatorController ctrl)
        {
            var root = new GameObject("PF_ZorroNeblina");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbx), root.transform);
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = "Modelo";
                var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
                anim.runtimeAnimatorController = ctrl; anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                // Orientación: el hocico debe mirar a +Z (adelante del prefab) para que la ruta lo gire bien.
                var head = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.Contains("Head"));
                var tail = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.Contains("Tail03"));
                if (head != null && tail != null)
                {
                    var dir = head.position - tail.position; dir.y = 0;
                    if (dir.sqrMagnitude > 1e-6f) model.transform.localRotation = Quaternion.Euler(0, Vector3.SignedAngle(dir, Vector3.forward, Vector3.up), 0) * model.transform.localRotation;
                }
                // Apoyo en el suelo: la parte más baja de la malla en y = 0.
                var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
                smr.updateWhenOffscreen = false; smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                model.transform.localPosition -= new Vector3(0, smr.bounds.min.y - root.transform.position.y, 0);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null) throw new Exception("No se pudo guardar " + PrefabPath);
                return prefab;
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
