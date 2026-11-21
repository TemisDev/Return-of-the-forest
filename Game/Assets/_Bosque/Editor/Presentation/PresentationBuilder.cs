using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bosque.ArtV6;
using Bosque.Audio;
using Bosque.CharacterEditor;
using Bosque.LevelEditor;
using Bosque.Session;
using Bosque.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using B = Bosque.ArtV6.Editor.ArroyoArtV6Builder;
using Object = UnityEngine.Object;

namespace Bosque.PresentationEditor
{
    /// <summary>
    /// Bloque 05, presentación: importa los recursos esenciales de TextMeshPro, crea las tipografías (OFL) y los
    /// fondos de la interfaz, el tema (Resources/BosqueUiTheme) y la escena del menú principal con un bosque
    /// restaurado de fondo. Deja Build Settings como [Menú, Arroyo, Claro, Refugio]. Idempotente.
    /// </summary>
    public static class PresentationBuilder
    {
        public const string UiRoot = "Assets/_Bosque/UI";
        public const string ThemePath = UiRoot + "/Resources/BosqueUiTheme.asset";
        public const string MenuScenePath = "Assets/_Bosque/Scenes/MenuPrincipal.unity";
        private const string Charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "¡¿áéíóúÁÉÍÓÚñÑüÜ·—–…“”‘’«»×°%ºª€";
        private static System.Random rng;
        private static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        [MenuItem("Tools/Bosque/Presentación/Construir menú, tema e interfaz")]
        public static void BuildAll()
        {
            EnsureTmpResources();
            Theme();
            BuildMenuScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true), new EditorBuildSettingsScene(ClaroBuilder.ArroyoScene, true),
                new EditorBuildSettingsScene(ClaroBuilder.ScenePath, true), new EditorBuildSettingsScene(RefugioBuilder.ScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Presentacion] Tema {ThemePath} y menú listos.");
        }

        public static void BuildAllBatch()
        {
            int code = 0;
            try { BuildAll(); } catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        // ------------------------------------------------------------------ TextMeshPro
        private static void EnsureTmpResources()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null) return;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            var pkg = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(pkg)) throw new FileNotFoundException(pkg);
            AssetDatabase.ImportPackage(pkg, false);
            AssetDatabase.Refresh();
            Debug.Log("[Presentacion] Recursos esenciales de TextMeshPro importados.");
        }

        private static TMP_FontAsset FontAsset(string ttf, string name, int size)
        {
            string fontPath = $"{UiRoot}/Fonts/{ttf}.ttf", assetPath = $"{UiRoot}/Fonts/{name}.asset";
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath) ?? throw new FileNotFoundException(fontPath);
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing; // dinámica: no se regenera (mantiene GUID y referencias)
            var fa = TMP_FontAsset.CreateFontAsset(font, size, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = name;
            fa.TryAddCharacters(Charset, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.Log($"[Presentacion] {name}: sin glifos para '{missing}'.");
            AssetDatabase.CreateAsset(fa, assetPath);
            foreach (var tex in fa.atlasTextures.Where(t => t != null)) { tex.name = name + " Atlas"; AssetDatabase.AddObjectToAsset(tex, fa); }
            fa.material.name = name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }

        // ------------------------------------------------------------------ fondos generados
        private static Sprite SpriteAsset(string name, int w, int h, Func<int, int, Color> pixel, int border)
        {
            Directory.CreateDirectory($"{UiRoot}/Sprites");
            string path = $"{UiRoot}/Sprites/{name}.png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = pixel(x, y);
            tex.SetPixels(px); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp;
            ti.spriteBorder = new Vector4(border, border, border, border); ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static float RoundedAlpha(int x, int y, int w, int h, float r)
        {
            float dx = Mathf.Max(r - x - .5f, x + .5f - (w - r), 0), dy = Mathf.Max(r - y - .5f, y + .5f - (h - r), 0);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(r - d + .5f);
        }

        private static UiTheme Theme()
        {
            Directory.CreateDirectory($"{UiRoot}/Resources");
            var hud = ReadArroyoIcons(); // abre una escena: se hace antes de tocar el tema
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null) { theme = ScriptableObject.CreateInstance<UiTheme>(); AssetDatabase.CreateAsset(theme, ThemePath); }
            theme.body = FontAsset("AlegreyaSans-Regular", "TMP_AlegreyaSans", 64);
            theme.bold = FontAsset("AlegreyaSans-Bold", "TMP_AlegreyaSans_Bold", 64);
            theme.title = FontAsset("AlegreyaSans-ExtraBold", "TMP_AlegreyaSans_ExtraBold", 72);
            theme.italic = FontAsset("AlegreyaSans-Italic", "TMP_AlegreyaSans_Italic", 64);
            theme.handwriting = FontAsset("Kalam-Regular", "TMP_Kalam", 64);

            theme.panel = SpriteAsset("UI_Panel", 96, 96, (x, y) => new Color(1, 1, 1, RoundedAlpha(x, y, 96, 96, 22)), 30);
            theme.pill = SpriteAsset("UI_Pastilla", 64, 64, (x, y) => new Color(1, 1, 1, RoundedAlpha(x, y, 64, 64, 32)), 31);
            theme.glow = SpriteAsset("UI_Halo", 256, 256, (x, y) =>
            {
                float d = new Vector2(x - 127.5f, y - 127.5f).magnitude / 128f;
                return new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d));
            }, 0);
            var noise = new System.Random(7);
            var fib = new float[512 * 512];
            for (int i = 0; i < fib.Length; i++) fib[i] = (float)noise.NextDouble();
            theme.paper = SpriteAsset("UI_Papel", 512, 512, (x, y) =>
            {
                float a = RoundedAlpha(x, y, 512, 512, 18);
                float edge = Mathf.Min(Mathf.Min(x, 511 - x), Mathf.Min(y, 511 - y)) / 70f;
                float grain = fib[y * 512 + x] * .05f + fib[(y * 7 % 512) * 512 + x] * .03f;
                float shade = .88f + .12f * Mathf.Clamp01(edge) - grain;
                return new Color(shade, shade * .985f, shade * .95f, a);
            }, 80);

            theme.interactionIcon = hud[0]; theme.progressIcon = hud[1]; theme.restorationIcon = hud[2]; theme.exitIcon = hud[3];
            theme.credits = AssetDatabase.LoadAssetAtPath<TextAsset>($"{UiRoot}/Creditos.txt");
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        private static Texture2D[] ReadArroyoIcons()
        {
            EditorSceneManager.OpenScene(ClaroBuilder.ArroyoScene, OpenSceneMode.Single);
            var hud = Object.FindFirstObjectByType<ArroyoPrototypeHud>();
            var so = new SerializedObject(hud);
            return new[] { "interactionIcon", "progressIcon", "restorationIcon", "exitIcon" }
                .Select(n => so.FindProperty(n).objectReferenceValue as Texture2D).ToArray();
        }

        // ------------------------------------------------------------------ escena del menú
        private static GameObject Put(string prefab, Transform parent, Vector3 pos, float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(B.Prefab(prefab), parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static void BuildMenuScene()
        {
            rng = new System.Random(20261125);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MenuPrincipal").transform;
            var world = new GameObject("BosqueRestaurado").transform; world.SetParent(root, false);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "V6_Suelo";
            ground.transform.SetParent(world, false); ground.transform.position = new Vector3(0, -.5f, 0); ground.transform.localScale = new Vector3(90, 1, 90);
            ground.GetComponent<MeshRenderer>().sharedMaterial = B.M("M_Ground_Bank_Restored");
            // Claro central con la Guardiana y el zorro; el bosque alrededor.
            for (int k = 0; k < 46; k++)
            {
                float a = R(0, Mathf.PI * 2), d = R(14f, 30f);
                var p = new Vector3(Mathf.Sin(a) * d, 0, Mathf.Cos(a) * d);
                string kind = rng.NextDouble() < .45 ? "A" : rng.NextDouble() < .5 ? "B" : "B_Tall";
                Put($"VIS_V6_Tree{kind}_Green", world, p, R(0, 360), R(1.4f, 2.1f));
            }
            string[] grass = { "VIS_V6_GrassA_LargeA", "VIS_V6_GrassA_LargeB", "VIS_V6_GrassA_MidA", "VIS_V6_GrassA_MidB", "VIS_V6_GrassA_TallA", "VIS_V6_GrassB_C", "VIS_V6_GrassB_D" };
            for (int k = 0; k < 260; k++)
            {
                float a = R(0, Mathf.PI * 2), d = R(1.6f, 20f);
                var g = Put(grass[rng.Next(grass.Length)], world, new Vector3(Mathf.Sin(a) * d, 0, Mathf.Cos(a) * d), R(0, 360), R(2.2f, 3.4f));
                foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            for (int k = 0; k < 50; k++)
            {
                float a = R(0, Mathf.PI * 2), d = R(2f, 14f);
                var name = new[] { "VIS_V6_FlowerYellow_A", "VIS_V6_FlowerYellow_C", "VIS_V6_Dandelion_A", "VIS_V6_Fern_A", "VIS_V6_Fern_C" }[rng.Next(5)];
                Put(name, world, new Vector3(Mathf.Sin(a) * d, 0, Mathf.Cos(a) * d), R(0, 360), R(1.5f, 2.2f));
            }
            for (int k = 0; k < 12; k++)
            {
                float a = R(0, Mathf.PI * 2), d = R(8.5f, 13f);
                Put("VIS_V6_Shrub_" + "ABCD"[rng.Next(4)], world, new Vector3(Mathf.Sin(a) * d, 0, Mathf.Cos(a) * d), R(0, 360), R(3.2f, 4.4f));
            }
            Put("VIS_V6_RockMossLarge_B", world, new Vector3(-2.6f, 0, -1.4f), 30, .8f);
            Put("VIS_V6_RockMoss_C", world, new Vector3(2.8f, 0, 1.8f), 80, .5f);
            var log = Put("VIS_V6_DeadLog_A", world, new Vector3(1.6f, 0, -2.4f), 110, 1f); log.transform.localScale = new Vector3(.7f, .6f, .6f);
            Fx("PFX_V6_Pollen", world, new Vector3(0, 1.4f, 0)); Fx("PFX_V6_Butterflies", world, new Vector3(1.5f, .8f, 1f)); Fx("PFX_V6_FallingLeaves", world, new Vector3(0, 7f, 0));

            var guardiana = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GuardianaBuilder.PrefabPath), root);
            PrefabUtility.UnpackPrefabInstance(guardiana, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            guardiana.name = "Guardiana"; guardiana.transform.SetPositionAndRotation(new Vector3(-.4f, 0, 0), Quaternion.Euler(0, 200, 0));
            foreach (var d in guardiana.GetComponentsInChildren<Bosque.Character.CharacterAnimationDriver>()) Object.DestroyImmediate(d); // sin motor: reposo del Animator
            var fox = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FaunaBuilder.PrefabPath), root);
            fox.name = "ZorroNeblina"; fox.transform.SetPositionAndRotation(new Vector3(1.1f, 0, .9f), Quaternion.Euler(0, 235, 0));

            var camGo = new GameObject("CamaraMenu"); camGo.tag = "MainCamera"; camGo.transform.SetParent(root, false);
            camGo.transform.position = new Vector3(0, 1.9f, 6f);
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 50; cam.nearClipPlane = .1f; cam.farClipPlane = 250;
            camGo.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.antialiasingQuality = AntialiasingQuality.High;

            var sunGo = new GameObject("Sun"); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft; sunGo.transform.rotation = Quaternion.Euler(35f, 230f, 0f);
            var amb = new GameObject("V6_Ambience").transform; amb.SetParent(root, false);
            Volume Vol(string name, string profile, float w)
            {
                var go = new GameObject(name); go.transform.SetParent(amb, false);
                var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 10; v.weight = w;
                v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{B.Root}/PostFX/{profile}.asset"); return v;
            }
            var ambience = amb.gameObject.AddComponent<BosqueV6Ambience>();
            ambience.restoredVisuals = world.gameObject; ambience.sun = sun; ambience.skyBlend = B.M("M_Sky_Blend");
            ambience.degradedVolume = Vol("V6_PostFX_Degraded", "VP_V6_Degraded", 0); ambience.restoredVolume = Vol("V6_PostFX_Restored", "VP_V6_Restored", 1);
            ambience.ApplyImmediate(true);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;

            var audio = new GameObject("Audio_Menu"); audio.transform.SetParent(root, false);
            var ambGo = new GameObject("Ambiente"); ambGo.transform.SetParent(audio.transform, false);
            var ac = ambGo.AddComponent<AmbienceController>();
            AudioClip A(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Bosque/Audio/Ambience/{n}.ogg");
            ac.loops = new List<AmbienceController.LoopLayer>
            {
                new AmbienceController.LoopLayer { name = "Brisa en hojas", clip = A("AMB_Brisa_Hojas"), volume = .45f, weightDegradedRestored = new Vector2(0f, 1f) },
                new AmbienceController.LoopLayer { name = "Aves", clip = A("AMB_Aves"), volume = .4f, weightDegradedRestored = new Vector2(0f, 1f) },
            };
            ac.oneShots = new List<AmbienceController.RandomOneShots>();
            var musGo = new GameObject("Musica"); musGo.transform.SetParent(audio.transform, false);
            musGo.AddComponent<AudioSource>().clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Bosque/Audio/Music/MUS_Bosque_Hermoso.ogg");
            var music = musGo.AddComponent<MusicPlayer>();

            var menu = new GameObject("Menu").AddComponent<MainMenu>(); menu.transform.SetParent(root, false);
            menu.Configure(camGo.transform, ac, music);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, MenuScenePath)) throw new Exception("No se pudo guardar " + MenuScenePath);
        }

        private static void Fx(string name, Transform parent, Vector3 pos)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{B.Root}/Particles/{name}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent); go.transform.position = pos;
        }
    }
}
