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
    /// traje Ranger + cabeza del personaje base (Quaternius), animaciones de la Universal Animation Library,
    /// materiales URP, Animator Controller con mezcla de locomoción y capa de gestos, prefab reutilizable,
    /// biblioteca de efectos y la integración en ArroyoPlayable. Idempotente: puede ejecutarse varias veces.
    /// </summary>
    public static class GuardianaBuilder
    {
        public const string Root = "Assets/_Bosque/Characters/Guardiana";
        public const string RangerFbx = Root + "/Source/Models/Female_Ranger.fbx";
        public const string BaseFbx = Root + "/Source/Models/Superhero_Female_FullBody.fbx";
        public const string AnimFbx = Root + "/Source/Animations/UAL1_Standard.fbx";
        public const string ControllerPath = Root + "/Animation/AC_Guardiana.controller";
        public const string MaskPath = Root + "/Animation/AM_Guardiana_TorsoBrazos.mask";
        public const string HeadMeshPath = Root + "/Meshes/SM_Guardiana_Cabeza.asset";
        public const string PrefabPath = Root + "/Prefabs/PF_Guardiana.prefab";
        public const string AudioRoot = "Assets/_Bosque/Audio";
        public const string SfxLibraryPath = AudioRoot + "/SFX_Biblioteca.asset";
        public const string ScenePath = "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity";

        // Velocidad propia de cada clip (m/s), medida en la versión con root motion de la librería.
        public const float WalkClipSpeed = .97f, JogClipSpeed = 5.36f, SprintClipSpeed = 8.25f;

        [MenuItem("Tools/Bosque/Personaje/Construir Guardiana y audio, e integrar en ArroyoPlayable")]
        public static void BuildAll()
        {
            ConfigureTextures();
            var mats = Materials();
            ConfigureModels(mats);
            var head = HeadMesh();
            var mask = UpperBodyMask();
            var controller = Controller(mask);
            var prefab = Prefab(head, controller);
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
        private static void ConfigureTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Textures" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.Contains("_Normal"), data = path.Contains("_MetalSmooth") || path.Contains("_Occlusion");
                var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (ti.textureType == type && ti.sRGBTexture == !(normal || data) && ti.maxTextureSize == 2048) continue;
                ti.textureType = type; ti.sRGBTexture = !(normal || data); ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
        }

        private static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/{name}.png");

        private static Material Mat(string name, string baseMap, string normal, string metalSmooth, string occlusion, float smoothness = 1f)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                System.IO.Directory.CreateDirectory(Root + "/Materials");
                m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", Tex(baseMap)); m.SetColor("_BaseColor", Color.white);
            if (normal != null) { m.SetTexture("_BumpMap", Tex(normal)); m.EnableKeyword("_NORMALMAP"); }
            if (metalSmooth != null)
            {
                m.SetTexture("_MetallicGlossMap", Tex(metalSmooth)); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Smoothness", smoothness); m.SetFloat("_SmoothnessTextureChannel", 0);
            }
            else { m.SetFloat("_Metallic", 0); m.SetFloat("_Smoothness", .35f); }
            if (occlusion != null) { m.SetTexture("_OcclusionMap", Tex(occlusion)); m.EnableKeyword("_OCCLUSIONMAP"); m.SetFloat("_OcclusionStrength", 1); }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Dictionary<string, Material> Materials() => new Dictionary<string, Material>
        {
            ["MI_Ranger"] = Mat("M_Guardiana_Traje", "T_Guardiana_Outfit_BaseColor", "T_Guardiana_Outfit_Normal", "T_Guardiana_Outfit_MetalSmooth", "T_Guardiana_Outfit_Occlusion"),
            ["MI_Regular_Female"] = Mat("M_Guardiana_PielCuerpo", "T_Guardiana_SkinBody_BaseColor", "T_Guardiana_SkinBody_Normal", "T_Guardiana_SkinBody_MetalSmooth", null, .8f),
            ["MI_Superhero_Female"] = Mat("M_Guardiana_PielCabeza", "T_Guardiana_SkinHead_BaseColor", "T_Guardiana_SkinHead_Normal", "T_Guardiana_SkinHead_MetalSmooth", null, .8f),
            ["MI_Eyes"] = Mat("M_Guardiana_Ojos", "T_Guardiana_Eye_BaseColor", null, null, null),
            ["MI_Hair_2"] = Mat("M_Guardiana_Pelo", "T_Guardiana_Hair_BaseColor", null, null, null),
        };

        // ------------------------------------------------------------------ modelos y animaciones (Humanoid)
        private static void ConfigureModels(Dictionary<string, Material> mats)
        {
            foreach (var path in new[] { RangerFbx, BaseFbx })
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = false; mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                foreach (var kv in mats) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
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
                c.lockRootRotation = true; c.keepOriginalOrientation = false; // orientación del cuerpo: la librería mira a -Z
                c.lockRootHeightY = true; c.keepOriginalPositionY = true; c.heightFromFeet = false;
                c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
                return c;
            }).ToArray();
            ai.clipAnimations = clips;
            ai.SaveAndReimport();
            foreach (var path in new[] { RangerFbx, BaseFbx, AnimFbx })
            {
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new Exception("Avatar humanoide no válido en " + path);
            }
        }

        private static AnimationClip Clip(string name) =>
            AssetDatabase.LoadAllAssetsAtPath(AnimFbx).OfType<AnimationClip>().FirstOrDefault(c => c.name == name)
            ?? throw new Exception("Falta el clip " + name);

        // ------------------------------------------------------------------ cabeza (del personaje base)
        private static Mesh HeadMesh()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseFbx);
            var inst = Object.Instantiate(model);
            try
            {
                var smr = inst.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Superhero_Female");
                var neck = inst.GetComponentsInChildren<Transform>().First(t => t.name == "neck_01");
                var baked = new Mesh(); smr.BakeMesh(baked, true);
                var world = baked.vertices.Select(v => smr.transform.TransformPoint(v)).ToArray();
                float cut = neck.position.y - .05f;
                var src = smr.sharedMesh; var tris = src.triangles;
                var keepTris = new List<int>();
                for (int i = 0; i < tris.Length; i += 3)
                    if (world[tris[i]].y > cut && world[tris[i + 1]].y > cut && world[tris[i + 2]].y > cut)
                        keepTris.AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
                var remap = new Dictionary<int, int>(); var order = new List<int>();
                foreach (int v in keepTris) if (!remap.ContainsKey(v)) { remap[v] = order.Count; order.Add(v); }
                var mesh = new Mesh { name = "SM_Guardiana_Cabeza" };
                T[] Pick<T>(T[] a) => a.Length == 0 ? a : order.Select(i => a[i]).ToArray();
                mesh.vertices = Pick(src.vertices); mesh.normals = Pick(src.normals); mesh.tangents = Pick(src.tangents);
                mesh.uv = Pick(src.uv); mesh.boneWeights = Pick(src.boneWeights); mesh.bindposes = src.bindposes;
                mesh.triangles = keepTris.Select(v => remap[v]).ToArray();
                mesh.RecalculateBounds();
                System.IO.Directory.CreateDirectory(Root + "/Meshes");
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(HeadMeshPath);
                if (existing != null) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
                else AssetDatabase.CreateAsset(mesh, HeadMeshPath);
                Debug.Log($"[Guardiana] Cabeza: {mesh.vertexCount} vértices, {mesh.triangles.Length / 3} triángulos (corte en y={cut:F2}).");
                return mesh;
            }
            finally { Object.DestroyImmediate(inst); }
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
            tree.AddChild(Clip("Walk_Loop"), WalkClipSpeed);
            tree.AddChild(Clip("Jog_Fwd_Loop"), JogClipSpeed);
            tree.AddChild(Clip("Sprint_Loop"), SprintClipSpeed);
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
        private static GameObject Prefab(Mesh head, AnimatorController controller)
        {
            var rangerModel = AssetDatabase.LoadAssetAtPath<GameObject>(RangerFbx);
            var baseModel = AssetDatabase.LoadAssetAtPath<GameObject>(BaseFbx);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(rangerModel);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "PF_Guardiana";
            try
            {
                // Explorador, no guerrero: sin hombreras y con un solo cinturón.
                foreach (var t in go.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("Pauldron") || t.name.EndsWith("Belt_2")).ToList())
                    Object.DestroyImmediate(t.gameObject);
                var bones = go.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
                var baseInst = Object.Instantiate(baseModel);
                try
                {
                    foreach (var src in baseInst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        var part = new GameObject(src.name == "Superhero_Female" ? "Guardiana_Cabeza" : "Guardiana_" + src.name);
                        part.transform.SetParent(go.transform, false);
                        var smr = part.AddComponent<SkinnedMeshRenderer>();
                        smr.sharedMesh = src.name == "Superhero_Female" ? head : src.sharedMesh;
                        smr.bones = src.bones.Select(b => bones[b.name]).ToArray();
                        smr.rootBone = bones[src.rootBone.name];
                        smr.sharedMaterials = src.sharedMaterials;
                        smr.localBounds = src.localBounds;
                    }
                }
                finally { Object.DestroyImmediate(baseInst); }
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    r.updateWhenOffscreen = false; r.skinnedMotionVectors = false;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    if (r.name.Contains("Eyes") || r.name.Contains("Eyebrows")) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath(RangerFbx).OfType<Avatar>().First();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // un solo personaje: los pasos y la cámara dependen de sus huesos
                if (go.GetComponent<CharacterAnimationDriver>() == null) go.AddComponent<CharacterAnimationDriver>();
                System.IO.Directory.CreateDirectory(Root + "/Prefabs");
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                int tris = go.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r => r.sharedMesh.triangles.Length / 3);
                var b0 = go.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
                Debug.Log($"[Guardiana] Prefab: {go.GetComponentsInChildren<SkinnedMeshRenderer>().Length} mallas, {tris} triángulos, altura {b0.size.y:F2} m, " +
                          $"materiales {go.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r => r.sharedMaterials).Distinct().Count()}.");
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
