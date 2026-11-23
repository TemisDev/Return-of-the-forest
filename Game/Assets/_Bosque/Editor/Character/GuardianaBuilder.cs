using System;
using System.Collections.Generic;
using System.Linq;
using Bosque.Audio;
using Bosque.Character;
using Bosque.Player;
using Bosque.Session;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bosque.CharacterEditor
{
    /// <summary>
    /// Construye a la Guardiana (personaje jugable) y el audio de la zona a partir de recursos CC0:
    /// humana realista generada con MPFB/MakeHuman (Docs/Character/pipeline/generar_guardiana.py), animaciones de la
    /// Universal Animation Library (Quaternius),
    /// materiales URP, Animator Controller con mezcla de locomoción y capa de gestos, prefab reutilizable,
    /// biblioteca de efectos y la integración en ArroyoPlayable. Idempotente: puede ejecutarse varias veces.
    /// </summary>
    public static class GuardianaBuilder
    {
        public const string Root = "Assets/_Bosque/Characters/Guardiana";
        // V14: modelo Microsoft Rocketbox "Female_Adult_07" (MIT), preparado por Docs/Character/pipeline/rocketbox_guardiana.py.
        // El modelo MPFB anterior sigue en Source/MPFB y Textures/MPFB.
        public const string ModelFbx = Root + "/Source/Rocketbox/SK_Guardiana_RB.fbx";
        public const string UrpManifest = Root + "/Source/Rocketbox/guardiana-urp.json";
        public const string TexturesDir = Root + "/Textures/Rocketbox";
        public const string AnimFbx = Root + "/Source/Animations/UAL1_Standard.fbx";
        public const string ControllerPath = Root + "/Animation/AC_Guardiana.controller";
        public const string MaskPath = Root + "/Animation/AM_Guardiana_TorsoBrazos.mask";
        public const string PrefabPath = Root + "/Prefabs/PF_Guardiana.prefab";
        public const string AudioRoot = "Assets/_Bosque/Audio";
        public const string SfxLibraryPath = AudioRoot + "/SFX_Biblioteca.asset";
        public const string ScenePath = "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity";

        // Velocidad propia de cada clip (m/s), medida en la versión con root motion de la librería.
        public const float WalkClipSpeed = .97f, JogClipSpeed = 5.36f, SprintClipSpeed = 8.25f;

        /// <summary>
        /// V15: locomoción de captura de movimiento real (Microsoft Rocketbox, MIT), mismo esqueleto Biped que la
        /// Guardiana. Clip del controlador → (archivo, velocidad natural en m/s medida en Blender: avance de la pelvis
        /// por ciclo / duración). Saltos y gestos siguen siendo de la biblioteca Quaternius (Rocketbox no tiene saltos).
        /// </summary>
        public const string RbAnimDir = Root + "/Source/Animations/Rocketbox";
        private static readonly Dictionary<string, (string file, float speed)> RbLocomotion = new Dictionary<string, (string, float)>
        {
            ["Idle_Loop"] = ("RB_f_idle_breathe_01", 0f),   // corto (2,7 s): no ralentiza la caminata al arrancar
            ["Walk_Loop"] = ("RB_f_walk_neutral", 1.30f),
            ["Jog_Fwd_Loop"] = ("RB_f_run_neutral", 3.16f),
            ["Sprint_Loop"] = ("RB_f_run_fast_01", 5.39f),
        };
        /// <summary>Desfase de giro (grados) por clip Rocketbox, medido con la prueba PostureFacesMovementWithoutSideLean.</summary>
        private static readonly Dictionary<string, float> RbYaw = new Dictionary<string, float> { ["RB_f_walk_neutral"] = -6.2f, ["RB_f_run_neutral"] = 4f, ["RB_f_run_fast_01"] = 4f };
        private static float ClipSpeed(string name, float fallback) => RbLocomotion.TryGetValue(name, out var e) ? e.speed : fallback;

        /// <summary>Desfase de giro por clip (grados) medido en modo juego: pecho y cadera respecto a la dirección de avance.</summary>
        private static readonly Dictionary<string, float> ClipYawCorrection = new Dictionary<string, float>
        {
            ["Jog_Fwd_Loop"] = 32f,
            ["Walk_Loop"] = 8f,
        };

        [MenuItem("Tools/Bosque/Personaje/Construir Guardiana y audio, e integrar en ArroyoPlayable")]
        public static void BuildAll()
        {
            ConfigureTextures();
            var mats = Materials();
            ConfigureModels(mats);
            var mask = UpperBodyMask();
            var controller = Controller(mask);
            var prefab = Prefab(controller);
            ConfigureAudioImports();
            var library = SfxLibraryAsset();
            Integrate(prefab, library);
            AssetDatabase.SaveAssets();
            Debug.Log("[Guardiana] Personaje, animación y audio listos.");
        }

        public static void BuildAllBatch()
        {
            int code = 0;
            try { BuildAll(); }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        // ------------------------------------------------------------------ texturas y materiales
        [Serializable] private sealed class UrpEntry { public string material, parte, @base, normal, smoothMap; public float smoothness, alphaClip; public bool doubleSided; }
        [Serializable] private sealed class UrpManifestData { public List<UrpEntry> materiales = new List<UrpEntry>(); }

        private static UrpManifestData Manifest() =>
            JsonUtility.FromJson<UrpManifestData>(System.IO.File.ReadAllText(UrpManifest));

        private static void ConfigureTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.Contains("_Normal"), linear = path.Contains("_MetalSmooth");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal && !linear; ti.maxTextureSize = 2048;
                ti.alphaIsTransparency = !normal && ti.DoesSourceTextureHaveAlpha();
                ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
        }

        private static Texture2D Tex(string file) =>
            string.IsNullOrEmpty(file) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/{file}");

        private static Material Mat(UrpEntry e)
        {
            string path = $"{Root}/Materials/M_Guardiana_{e.parte}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                System.IO.Directory.CreateDirectory(Root + "/Materials");
                m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_Guardiana_" + e.parte };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", Tex(e.@base)); m.SetColor("_BaseColor", Color.white);
            var n = Tex(e.normal);
            if (n != null) { m.SetTexture("_BumpMap", n); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); }
            else { m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP"); }
            m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", e.smoothness);
            // Suavidad por píxel (piel, tela y cuero distintos): alfa del mapa MetalSmooth, escalada por "smoothness".
            var sm = Tex(e.smoothMap);
            m.SetTexture("_MetallicGlossMap", sm); m.SetFloat("_SmoothnessTextureChannel", 0f);
            if (sm != null) m.EnableKeyword("_METALLICSPECGLOSSMAP"); else m.DisableKeyword("_METALLICSPECGLOSSMAP");
            bool clip = e.alphaClip > 0f;
            m.SetFloat("_AlphaClip", clip ? 1f : 0f); m.SetFloat("_Cutoff", clip ? e.alphaClip : .5f);
            if (clip) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", e.doubleSided ? 0f : 2f);
            m.renderQueue = clip ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material de Blender (por nombre, p. ej. "Human.body") -> material URP.</summary>
        private static Dictionary<string, Material> Materials() => Manifest().materiales.ToDictionary(e => e.material, Mat);

        // ------------------------------------------------------------------ modelos y animaciones (Humanoid)
        private static void ConfigureModels(Dictionary<string, Material> mats)
        {
            foreach (var path in new[] { ModelFbx })
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = false; mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                foreach (var kv in mats) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                mi.SaveAndReimport();
                // El reposo de la biblioteca abre mucho las piernas (postura de pelea). Se estrecha el rango
                // "In-Out" de los muslos del avatar: la misma animación, con los pies más juntos y natural.
                var hd = mi.humanDescription; var human = hd.human;
                for (int i = 0; i < human.Length; i++)
                {
                    var n = human[i].humanName;
                    if ((n.Contains("Proximal") || n.Contains("Intermediate") || n.Contains("Distal")) && !n.Contains("Thumb"))
                    {
                        // Los puños del reposo quedan cerrados: con menos recorrido de cierre, la mano queda relajada.
                        var f = human[i].limit;
                        f.useDefaultValues = false; f.min = new Vector3(0f, -12f, -22f); f.max = new Vector3(0f, 12f, 22f);
                        human[i].limit = f; continue;
                    }
                    if (n != "LeftUpperLeg" && n != "RightUpperLeg") continue;
                    var l = human[i].limit;
                    l.useDefaultValues = false; l.min = new Vector3(-60f, -90f, -24f); l.max = new Vector3(60f, 50f, 24f);
                    human[i].limit = l;
                }
                hd.human = human; mi.humanDescription = hd;
                mi.SaveAndReimport();
            }
            var ai = (ModelImporter)AssetImporter.GetAtPath(AnimFbx);
            ai.animationType = ModelImporterAnimationType.Human;
            ai.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            ai.importAnimation = true; ai.materialImportMode = ModelImporterMaterialImportMode.None;
            ai.SaveAndReimport();
            var clips = ai.defaultClipAnimations.Select(c =>
            {
                c.name = c.takeName.Split('|').Last();
                bool loop = c.name.EndsWith("_Loop");
                c.loopTime = loop; c.loopPose = loop;
                // Sin root motion: el CharacterController mueve al personaje; la raíz se queda en la pose.
                // Orientación del cuerpo ("Body Orientation"); el trote de la librería queda desviado ~19° a la derecha con
                // ese criterio, así que se compensa con un desfase medido (prueba PostureFacesMovementWithoutSideLean).
                c.lockRootRotation = true; c.keepOriginalOrientation = false;
                c.rotationOffset = ClipYawCorrection.TryGetValue(c.name, out float yaw) ? yaw : 0f;
                c.lockRootHeightY = true; c.keepOriginalPositionY = true; c.heightFromFeet = false;
                c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
                return c;
            }).ToArray();
            ai.clipAnimations = clips;
            ai.SaveAndReimport();
            // Locomoción Rocketbox: un clip por archivo, en el sitio (la raíz se hornea en la pose), en bucle.
            foreach (var (file, _) in RbLocomotion.Values)
            {
                string path = $"{RbAnimDir}/{file}.fbx";
                var ri = (ModelImporter)AssetImporter.GetAtPath(path) ?? throw new Exception("Falta " + path);
                // Mismo esqueleto Biped que la Guardiana: se usa su avatar (misma pose de referencia, sin desajustes de pies).
                ri.animationType = ModelImporterAnimationType.Human; ri.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                ri.sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(ModelFbx).OfType<Avatar>().First();
                ri.importAnimation = true; ri.materialImportMode = ModelImporterMaterialImportMode.None; ri.importBlendShapes = false;
                ri.SaveAndReimport();
                ri.clipAnimations = ri.defaultClipAnimations.Take(1).Select(c =>
                {
                    c.name = file; c.loopTime = true; c.loopPose = true;
                    c.lockRootRotation = true; c.keepOriginalOrientation = false;
                    c.rotationOffset = RbYaw.TryGetValue(file, out float yaw) ? yaw : 0f;
                    c.lockRootHeightY = true; c.keepOriginalPositionY = false; c.heightFromFeet = true;
                    // Estos clips traen el avance real del actor (3 m por ciclo al correr). Si se hornea en la pose, el
                    // cuerpo se separa de la cápsula y vuelve de golpe en cada ciclo. Sin hornear, ese avance pasa a ser
                    // movimiento de raíz, que el Animator descarta (applyRootMotion = false): el cuerpo queda en su sitio.
                    c.lockRootPositionXZ = false;
                    return c;
                }).ToArray();
                ri.SaveAndReimport();
            }
            foreach (var path in new[] { ModelFbx, AnimFbx }.Concat(RbLocomotion.Values.Select(v => $"{RbAnimDir}/{v.file}.fbx")))
            {
                if (path.StartsWith(RbAnimDir)) continue;   // usan el avatar de la Guardiana
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new Exception("Avatar humanoide no válido en " + path);
            }
        }

        private static AnimationClip Clip(string name)
        {
            if (RbLocomotion.TryGetValue(name, out var rb))
                return AssetDatabase.LoadAllAssetsAtPath($"{RbAnimDir}/{rb.file}.fbx").OfType<AnimationClip>().FirstOrDefault(c => c.name == rb.file)
                       ?? throw new Exception("Falta el clip " + rb.file);
            return AssetDatabase.LoadAllAssetsAtPath(AnimFbx).OfType<AnimationClip>().FirstOrDefault(c => c.name == name)
                   ?? throw new Exception("Falta el clip " + name);
        }

        // ------------------------------------------------------------------ máscara y controlador
        private static AvatarMask UpperBodyMask()
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (mask == null)
            {
                System.IO.Directory.CreateDirectory(Root + "/Animation");
                mask = new AvatarMask(); AssetDatabase.CreateAsset(mask, MaskPath);
            }
            for (var p = AvatarMaskBodyPart.Root; p < AvatarMaskBodyPart.LastBodyPart; p++)
                mask.SetHumanoidBodyPartActive(p, p != AvatarMaskBodyPart.Root && p != AvatarMaskBodyPart.LeftLeg &&
                                                  p != AvatarMaskBodyPart.RightLeg && p != AvatarMaskBodyPart.LeftFootIK && p != AvatarMaskBodyPart.RightFootIK);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimatorController Controller(AvatarMask mask)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter(new AnimatorControllerParameter { name = "Grounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
            ac.AddParameter("Falling", AnimatorControllerParameterType.Bool);
            ac.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Restore", AnimatorControllerParameterType.Trigger);

            var sm = ac.layers[0].stateMachine;
            var tree = new BlendTree { name = "Locomocion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(Clip("Idle_Loop"), 0f);
            // Umbral = velocidad natural del clip: a la velocidad del motor los pies no patinan.
            tree.AddChild(Clip("Walk_Loop"), ClipSpeed("Walk_Loop", WalkClipSpeed));
            tree.AddChild(Clip("Jog_Fwd_Loop"), ClipSpeed("Jog_Fwd_Loop", JogClipSpeed));
            tree.AddChild(Clip("Sprint_Loop"), ClipSpeed("Sprint_Loop", SprintClipSpeed));
            var loco = sm.AddState("Locomocion", new Vector3(300, 0)); loco.motion = tree;
            var jump = sm.AddState("SaltoInicio", new Vector3(300, 120)); jump.motion = Clip("Jump_Start"); jump.speed = 1.6f;
            var air = sm.AddState("Aire", new Vector3(550, 120)); air.motion = Clip("Jump_Loop");
            var land = sm.AddState("Aterrizaje", new Vector3(550, 0)); land.motion = Clip("Jump_Land"); land.speed = 1.5f;
            sm.defaultState = loco;

            AnimatorStateTransition T(AnimatorState from, AnimatorState to, float duration, float? exit = null, float offset = 0f)
            {
                var t = from.AddTransition(to); t.duration = duration; t.offset = offset;
                t.hasExitTime = exit.HasValue; if (exit.HasValue) t.exitTime = exit.Value;
                t.interruptionSource = TransitionInterruptionSource.None;
                return t;
            }
            // El impulso del motor es inmediato: se salta la anticipación agachada del clip.
            T(loco, jump, .06f, null, .3f).AddCondition(AnimatorConditionMode.If, 0, "Jump");
            T(loco, air, .2f).AddCondition(AnimatorConditionMode.If, 0, "Falling");
            var j2a = T(jump, air, .2f, .8f);
            var j2l = T(jump, land, .1f); j2l.AddCondition(AnimatorConditionMode.If, 0, "Grounded"); j2l.AddCondition(AnimatorConditionMode.Less, 0f, "VerticalSpeed");
            T(air, land, .08f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            T(land, loco, .25f, .45f);
            var l2m = T(land, loco, .15f); l2m.AddCondition(AnimatorConditionMode.Greater, 1.2f, "Speed");
            T(land, jump, .05f, null, .3f).AddCondition(AnimatorConditionMode.If, 0, "Jump");

            // Capa de gestos: solo torso, brazos y cabeza; las piernas siguen la locomoción (nunca bloquea el movimiento).
            ac.AddLayer("Gestos");
            var layers = ac.layers; layers[1].avatarMask = mask; layers[1].defaultWeight = 1f; layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            ac.layers = layers;
            var g = ac.layers[1].stateMachine;
            var empty = g.AddState("Vacio", new Vector3(300, 0)); g.defaultState = empty;
            var interact = g.AddState("Interactuar", new Vector3(300, 120)); interact.motion = Clip("Interact"); interact.speed = 1.5f;
            var restore = g.AddState("Restaurar", new Vector3(550, 120)); restore.motion = Clip("Spell_Simple_Shoot"); restore.speed = .55f;
            T(empty, interact, .12f).AddCondition(AnimatorConditionMode.If, 0, "Interact");
            T(interact, empty, .3f, .85f);
            T(empty, restore, .15f).AddCondition(AnimatorConditionMode.If, 0, "Restore");
            T(interact, restore, .15f).AddCondition(AnimatorConditionMode.If, 0, "Restore");
            T(restore, empty, .35f, .9f);
            EditorUtility.SetDirty(ac);
            return ac;
        }

        // ------------------------------------------------------------------ prefab
        private static GameObject Prefab(AnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbx);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "PF_Guardiana";
            try
            {
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    r.updateWhenOffscreen = false; r.skinnedMotionVectors = false;
                    bool small = r.name.Contains("eyebrow") || r.name.Contains("eyelash") || r.name.Contains("high-poly");
                    r.shadowCastingMode = small ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                }
                var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelFbx).OfType<Avatar>().First();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // un solo personaje: los pasos y la cámara dependen de sus huesos
                if (go.GetComponent<CharacterAnimationDriver>() == null) go.AddComponent<CharacterAnimationDriver>();
                System.IO.Directory.CreateDirectory(Root + "/Prefabs");
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
                int tris = smrs.Sum(r => r.sharedMesh.triangles.Length / 3);
                var b0 = smrs.Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
                Debug.Log($"[Guardiana] Prefab Rocketbox: {smrs.Length} mallas, {tris} triángulos, altura {b0.size.y:F2} m, " +
                          $"materiales {smrs.SelectMany(r => r.sharedMaterials).Distinct().Count()}, huesos {smrs.SelectMany(r => r.bones).Distinct().Count()}.");
                return prefab;
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ------------------------------------------------------------------ audio
        private static void ConfigureAudioImports()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ai = (AudioImporter)AssetImporter.GetAtPath(path);
                bool longClip = path.Contains("/Music/") || path.Contains("AMB_Viento") || path.Contains("AMB_Rio") || path.Contains("AMB_Aves.") || path.Contains("AMB_Brisa");
                var s = ai.defaultSampleSettings;
                s.loadType = longClip ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = longClip ? .5f : .7f;
                s.preloadAudioData = !longClip;
                ai.defaultSampleSettings = s; ai.forceToMono = path.Contains("/Sfx/") || path.Contains("_3D") || path.Contains("AMB_Ave_") || path.Contains("Crujido") || path.Contains("Rama_Seca");
                ai.loadInBackground = longClip;
                ai.SaveAndReimport();
            }
        }

        private static AudioClip[] Clips(string folder, string prefix) =>
            AssetDatabase.FindAssets("t:AudioClip", new[] { $"{AudioRoot}/{folder}" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => System.IO.Path.GetFileName(p).StartsWith(prefix)).OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();

        private static SfxLibrary SfxLibraryAsset()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SfxLibrary>(SfxLibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<SfxLibrary>(); AssetDatabase.CreateAsset(lib, SfxLibraryPath); }
            SfxCue Cue(string id, string prefix, float vol, float blend, float min, float max, float cooldown, Vector2 pitch) =>
                new SfxCue { id = id, clips = Clips("Sfx", prefix), volume = vol, spatialBlend = blend, minDistance = min, maxDistance = max, cooldown = cooldown, pitch = pitch };
            lib.cues = new List<SfxCue>
            {
                Cue("footstep", "SFX_Paso_Tierra", .5f, .8f, 2f, 18f, .14f, new Vector2(.88f, 1.1f)),
                Cue("jump", "SFX_Salto", .55f, .8f, 2f, 18f, .3f, new Vector2(.95f, 1.08f)),
                Cue("land", "SFX_Aterrizaje", .7f, .8f, 2f, 18f, .25f, new Vector2(.85f, 1f)),
                Cue("interact", "SFX_Interaccion", .6f, .6f, 2f, 15f, .3f, new Vector2(.95f, 1.05f)),
                Cue("remove.log", "SFX_Retirar_Tronco", .9f, 1f, 3f, 40f, .5f, new Vector2(.95f, 1.05f)),
                Cue("remove.rocks", "SFX_Retirar_Rocas", 1f, 1f, 3f, 40f, .5f, new Vector2(.9f, 1.05f)),
                Cue("remove.branches", "SFX_Retirar_Ramas", .9f, 1f, 3f, 40f, .5f, new Vector2(.95f, 1.08f)),
                Cue("restore", "SFX_Restauracion", .85f, 0f, 1f, 50f, 2f, new Vector2(1f, 1f)),
                Cue("exit.unlock", "SFX_Salida_Desbloqueo", .8f, 1f, 4f, 60f, 2f, new Vector2(.9f, .95f)),
                Cue("complete", "SFX_Nivel_Completado", .8f, 0f, 1f, 50f, 2f, new Vector2(1f, 1f)),
                Cue("grow", "SFX_Brote_0", .85f, 1f, 2f, 30f, .4f, new Vector2(.97f, 1.04f)),
                Cue("grow.fail", "SFX_Brote_Fallido", .7f, 1f, 2f, 20f, .5f, new Vector2(.92f, 1f)),
                Cue("bridge", "SFX_Puente_Raices", .9f, .6f, 4f, 50f, 2f, new Vector2(1f, 1f)),
            };
            foreach (var c in lib.cues) if (c.clips.Length == 0) throw new Exception("Efecto sin clips: " + c.id);
            EditorUtility.SetDirty(lib);
            return lib;
        }

        // ------------------------------------------------------------------ integración en la escena
        private static void Integrate(GameObject prefab, SfxLibrary library)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            var player = all.First(t => t.name == "Player");
            foreach (var old in all.Where(t => t != null && (t.name == "Guardiana" || t.name == "Audio_Arroyo")).ToList()) Object.DestroyImmediate(old.gameObject);
            var body = player.Find("ProvisionalBody");
            if (body != null && body.TryGetComponent<MeshRenderer>(out var capsule)) capsule.enabled = false; // el objeto y su escala se conservan
            var cc = player.GetComponent<CharacterController>();
            var character = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player);
            character.name = "Guardiana";
            character.transform.localPosition = new Vector3(0, -cc.skinWidth, 0);
            character.transform.localRotation = Quaternion.identity;
            var driver = character.GetComponent<CharacterAnimationDriver>();

            var cam = Object.FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null)
            {
                var so = new SerializedObject(cam);
                so.FindProperty("focusHeight").floatValue = 1.45f; // altura de los hombros de la Guardiana (1,75 m)
                so.FindProperty("distance").floatValue = 4.2f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var audio = new GameObject("Audio_Arroyo");
            var sfx = audio.AddComponent<SfxPlayer>(); sfx.Configure(library);
            var ambGo = new GameObject("Ambiente"); ambGo.transform.SetParent(audio.transform, false);
            var amb = ambGo.AddComponent<AmbienceController>();
            AudioClip A(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/Ambience/{n}.ogg");
            Transform Anchor(string n, Vector3 p) { var t = new GameObject(n).transform; t.SetParent(ambGo.transform, false); t.position = p; return t; }
            amb.loops = new List<AmbienceController.LoopLayer>
            {
                new AmbienceController.LoopLayer { name = "Viento seco", clip = A("AMB_Viento_Seco"), volume = .55f, weightDegradedRestored = new Vector2(1f, .15f) },
                new AmbienceController.LoopLayer { name = "Brisa en hojas", clip = A("AMB_Brisa_Hojas"), volume = .5f, weightDegradedRestored = new Vector2(.1f, 1f) },
                new AmbienceController.LoopLayer { name = "Río", clip = A("AMB_Rio"), volume = .35f, weightDegradedRestored = new Vector2(0f, 1f) },
                new AmbienceController.LoopLayer { name = "Aves", clip = A("AMB_Aves"), volume = .45f, weightDegradedRestored = new Vector2(.05f, 1f) },
                new AmbienceController.LoopLayer { name = "Cauce sur", clip = A("AMB_Corriente_3D"), volume = .7f, weightDegradedRestored = new Vector2(0f, 1f), anchor = Anchor("Cauce sur", new Vector3(-2.5f, -.2f, 6f)), minDistance = 2f, maxDistance = 16f },
                new AmbienceController.LoopLayer { name = "Cauce norte", clip = A("AMB_Corriente_3D"), volume = .7f, weightDegradedRestored = new Vector2(0f, 1f), anchor = Anchor("Cauce norte", new Vector3(-2.5f, -.2f, 27f)), minDistance = 2f, maxDistance = 16f },
            };
            amb.oneShots = new List<AmbienceController.RandomOneShots>
            {
                new AmbienceController.RandomOneShots { name = "Crujidos", clips = new[] { A("AMB_Crujido_01"), A("AMB_Crujido_02"), A("AMB_Crujido_03") }, volume = .5f, weightDegradedRestored = new Vector2(1f, 0f), intervalSeconds = new Vector2(5f, 12f) },
                new AmbienceController.RandomOneShots { name = "Ramas secas", clips = new[] { A("AMB_Rama_Seca_01"), A("AMB_Rama_Seca_02"), A("AMB_Rama_Seca_03") }, volume = .45f, weightDegradedRestored = new Vector2(1f, 0f), intervalSeconds = new Vector2(7f, 16f) },
                new AmbienceController.RandomOneShots { name = "Ave lejana", clips = new[] { A("AMB_Ave_02") }, volume = .25f, weightDegradedRestored = new Vector2(.4f, 0f), intervalSeconds = new Vector2(18f, 35f), radius = new Vector2(20f, 30f) },
                new AmbienceController.RandomOneShots { name = "Aves cercanas", clips = new[] { A("AMB_Ave_01"), A("AMB_Ave_03"), A("AMB_Ave_04"), A("AMB_Ave_05"), A("AMB_Ave_06") }, volume = .55f, weightDegradedRestored = new Vector2(0f, 1f), intervalSeconds = new Vector2(2.5f, 7f) },
            };
            var musGo = new GameObject("Musica"); musGo.transform.SetParent(audio.transform, false);
            var src = musGo.AddComponent<AudioSource>(); src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/Music/MUS_Bosque_Hermoso.ogg");
            var music = musGo.AddComponent<MusicPlayer>();
            var bridge = audio.AddComponent<ArroyoAudioBridge>();
            var session = Object.FindFirstObjectByType<ArroyoSession>();
            var exit = all.FirstOrDefault(t => t.name == "ExitMarker") ?? all.First(t => t.name == "ExitBlocker");
            bridge.Configure(session, sfx, amb, music, driver, exit);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("No se pudo guardar " + ScenePath);
            Debug.Log("[Guardiana] Integrada en " + ScenePath + " con audio de zona.");
        }
    }
}
