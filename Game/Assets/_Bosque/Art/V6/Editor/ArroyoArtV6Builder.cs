using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bosque.ArtV6.Editor
{
    /// <summary>
    /// Arte V6 realista: importación, materiales, prefabs visuales, partículas y perfiles de post-proceso.
    /// Solo escribe dentro de Assets/_Bosque/Art/V6. No crea colliders ni toca escenas de integración.
    /// </summary>
    public static class ArroyoArtV6Builder
    {
        public const string Root = "Assets/_Bosque/Art/V6";
        public const string Mat = Root + "/Materials";
        public const string Pfb = Root + "/Prefabs";

        [MenuItem("Tools/Bosque/Arte/V6/1 - Generar materiales, prefabs y partículas")]
        public static void BuildAll()
        {
            ConfigureTextures();
            AssetDatabase.Refresh();
            BuildMaterials();
            ConfigureModels();
            BuildPrefabs();
            ArroyoArtV6Particles.BuildAll();
            BuildVolumes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[V6] Arte V6 generado en " + Root);
        }

        public static void BuildAllBatch()
        {
            int code = 0;
            try
            {
                BuildAll();
                if (!ReportShaders()) code = 3;
                ReportModels();
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        // ------------------------------------------------------------------ Texturas
        public static void ConfigureTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture", new[] { Root + "/Textures", Root + "/Sky" });
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!(AssetImporter.GetAtPath(p) is TextureImporter ti)) continue;
                string n = Path.GetFileNameWithoutExtension(p);
                bool fx = p.Contains("/Textures/FX/");
                bool water = p.Contains("/Textures/Water/");
                bool sky = p.Contains("/Sky/");
                ti.mipmapEnabled = true;
                ti.wrapMode = fx ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                ti.anisoLevel = fx || sky ? 1 : 8;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                if (sky)
                {
                    ti.textureShape = TextureImporterShape.Texture2D;
                    ti.sRGBTexture = false;
                    ti.maxTextureSize = 2048;
                    ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp;
                    ti.mipmapEnabled = false;
                }
                else if (n.EndsWith("_Normal") || n.StartsWith("T_Water_Normal"))
                {
                    ti.textureType = TextureImporterType.NormalMap;
                }
                else if (n.EndsWith("_Mask") || n == "T_Water_Noise")
                {
                    ti.textureType = TextureImporterType.Default;
                    ti.sRGBTexture = false;
                    ti.alphaSource = TextureImporterAlphaSource.FromInput;
                }
                else
                {
                    ti.textureType = TextureImporterType.Default;
                    ti.sRGBTexture = true;
                    bool alpha = ti.DoesSourceTextureHaveAlpha();
                    ti.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                    ti.alphaIsTransparency = alpha && (fx || p.Contains("/Foliage/") || IsFoliageSet(n));
                    if (alpha && !fx) ti.mipMapsPreserveCoverage = true;
                    if (alpha && !fx) ti.alphaTestReferenceValue = 0.45f;
                }
                if (!sky) ti.maxTextureSize = p.Contains("/Ground/") || n.Contains("Boulder") || n.Contains("DeadLog") || n.Contains("RockMoss") ? 2048 : 1024;
                ti.SaveAndReimport();
            }
        }

        private static readonly string[] FoliageSets = { "Fern", "GrassA", "GrassB", "Shrub", "FlowerYellow", "Dandelion", "TreeABranches", "TreeBBranches" };
        private static bool IsFoliageSet(string textureName) => FoliageSets.Any(s => textureName.StartsWith("T_" + s + "_"));

        public static Texture2D Tex(string folder, string name)
        {
            foreach (var ext in new[] { ".png", ".jpg", ".hdr" })
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/{folder}/{name}{ext}");
                if (t != null) return t;
            }
            throw new FileNotFoundException($"Textura V6 no encontrada: {folder}/{name}");
        }

        // ------------------------------------------------------------------ Materiales
        public static Material EnsureMaterial(string name, string shaderName)
        {
            var shader = Shader.Find(shaderName) ?? throw new Exception("Shader no encontrado: " + shaderName);
            string path = $"{Mat}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Directory.CreateDirectory(Mat);
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Lit(string set, float smooth = 1f, Color? tint = null)
        {
            var m = EnsureMaterial("M_" + set, "Universal Render Pipeline/Lit");
            m.SetTexture("_BaseMap", Tex("Textures/Props", $"T_{set}_BaseMap"));
            m.SetColor("_BaseColor", tint ?? Color.white);
            m.SetTexture("_BumpMap", Tex("Textures/Props", $"T_{set}_Normal"));
            m.SetFloat("_BumpScale", 1f);
            var mask = Tex("Textures/Props", $"T_{set}_Mask");
            m.SetTexture("_MetallicGlossMap", mask);
            m.SetTexture("_OcclusionMap", mask);
            m.SetFloat("_OcclusionStrength", 1f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.EnableKeyword("_NORMALMAP");
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.EnableKeyword("_OCCLUSIONMAP");
            return m;
        }

        private static Material Foliage(string matName, string folder, string set, float cutoff, float wind, float windHeight,
                                        float translucency = 0.6f, Color? tint = null, float flutter = 0.01f, float smooth = 0.6f)
        {
            var m = EnsureMaterial(matName, "Bosque/V6/Foliage");
            m.SetTexture("_BaseMap", Tex(folder, $"T_{set}_BaseMap"));
            m.SetTexture("_BumpMap", Tex(folder, $"T_{set}_Normal"));
            m.SetTexture("_MaskMap", Tex(folder, $"T_{set}_Mask"));
            m.SetColor("_BaseColor", tint ?? Color.white);
            m.SetFloat("_Cutoff", cutoff);
            m.SetFloat("_WindStrength", wind);
            m.SetFloat("_WindHeight", windHeight);
            m.SetFloat("_WindFlutter", flutter);
            m.SetFloat("_Translucency", translucency);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }

        public static Material Triplanar(string name, string top, string side, float topTiling, float sideTiling,
                                         float wetLevel = -100f, Color? topTint = null, Color? sideTint = null, float threshold = 0.6f)
        {
            var m = EnsureMaterial(name, "Bosque/V6/TerrainTriplanar");
            m.SetTexture("_TopBaseMap", Tex("Textures/Ground", $"T_{top}_BaseMap"));
            m.SetTexture("_TopNormal", Tex("Textures/Ground", $"T_{top}_Normal"));
            m.SetTexture("_TopMask", Tex("Textures/Ground", $"T_{top}_Mask"));
            m.SetTexture("_SideBaseMap", Tex("Textures/Ground", $"T_{side}_BaseMap"));
            m.SetTexture("_SideNormal", Tex("Textures/Ground", $"T_{side}_Normal"));
            m.SetTexture("_SideMask", Tex("Textures/Ground", $"T_{side}_Mask"));
            m.SetTexture("_MacroNoise", Tex("Textures/Water", "T_Water_Noise"));
            m.SetFloat("_TopTiling", topTiling);
            m.SetFloat("_SideTiling", sideTiling);
            m.SetColor("_TopColor", topTint ?? Color.white);
            m.SetColor("_SideColor", sideTint ?? Color.white);
            m.SetFloat("_WetLevel", wetLevel);
            m.SetFloat("_TopThreshold", threshold);
            m.SetFloat("_SmoothnessScale", name.Contains("Rock") || name.Contains("Cliff") ? 0.55f : 0.8f);
            return m;
        }

        public static void BuildMaterials()
        {
            // Props con UV propias (Poly Haven, CC0)
            foreach (var s in new[] { "RockMossLarge", "RockMoss", "BoulderDryA", "BoulderDryB", "DeadLog", "DryBranches", "Stump", "RootCluster", "PineRootsA", "PineRootsB" })
                Lit(s);

            // Vegetación
            Foliage("M_Fern", "Textures/Props", "Fern", 0.4f, 0.06f, 0.6f, 0.7f);
            Foliage("M_GrassA", "Textures/Props", "GrassA", 0.4f, 0.05f, 0.35f, 0.8f, null, 0.004f);
            Foliage("M_GrassA_Dry", "Textures/Props", "GrassA", 0.45f, 0.04f, 0.35f, 0.5f, new Color(0.95f, 0.78f, 0.5f), 0.004f, 0.25f);
            Foliage("M_GrassB", "Textures/Props", "GrassB", 0.4f, 0.05f, 0.4f, 0.8f, null, 0.004f);
            Foliage("M_GrassB_Dry", "Textures/Props", "GrassB", 0.45f, 0.04f, 0.4f, 0.5f, new Color(0.92f, 0.74f, 0.46f), 0.004f, 0.25f);
            Foliage("M_Shrub", "Textures/Props", "Shrub", 0.4f, 0.04f, 0.3f, 0.6f);
            Foliage("M_FlowerYellow", "Textures/Props", "FlowerYellow", 0.4f, 0.05f, 0.25f, 0.7f);
            Foliage("M_Dandelion", "Textures/Props", "Dandelion", 0.4f, 0.05f, 0.25f, 0.6f);
            Foliage("M_TreeABark", "Textures/Props", "TreeABark", 0f, 0.1f, 6f, 0f, null, 0f, 0.4f);
            Foliage("M_TreeABranches", "Textures/Props", "TreeABranches", 0.3f, 0.1f, 6f, 0.2f, null, 0.003f, 0.4f);
            Foliage("M_TreeBBark", "Textures/Props", "TreeBBark", 0f, 0.1f, 6f, 0f, null, 0f, 0.4f);
            Foliage("M_TreeBBranches", "Textures/Props", "TreeBBranches", 0.3f, 0.1f, 6f, 0.2f, null, 0.003f, 0.4f);
            Foliage("M_LeafClusterGreen", "Textures/Foliage", "LeafClusterGreen", 0.45f, 0.1f, 6f, 0.75f, null, 0.015f, 0.45f);
            Foliage("M_LeafClusterLush", "Textures/Foliage", "LeafClusterLush", 0.45f, 0.1f, 6f, 0.75f, null, 0.015f, 0.45f);
            Foliage("M_LeafClusterDry", "Textures/Foliage", "LeafClusterDry", 0.62f, 0.1f, 6f, 0.25f, new Color(0.72f, 0.64f, 0.56f), 0.01f, 0.12f);

            // Suelos triplanares (no dependen de UV ni escala de bloques)
            Triplanar("M_Ground_Bank_Degraded", "DryGroundRocks", "RockDry", 3.2f, 4f, -100f, new Color(0.86f, 0.8f, 0.72f), new Color(0.7f, 0.65f, 0.58f));
            Triplanar("M_Ground_Bank_Restored", "ForestGround", "MossyRock", 3.2f, 4f, -0.05f);
            Triplanar("M_Ground_Bed_Degraded", "DryRiverbed", "RockDry", 2.6f, 3f);
            Triplanar("M_Ground_Bed_Restored", "RiverPebbles", "MossyRock", 1.6f, 3f, -0.12f);
            Triplanar("M_Ground_Cliff_Degraded", "DryGroundRocks", "RockDry", 3f, 4.5f, -100f, new Color(0.86f, 0.8f, 0.72f), new Color(0.66f, 0.61f, 0.55f), 0.7f);
            Triplanar("M_Ground_Cliff_Restored", "LeafyGrass", "MossyRock", 2.5f, 5f, -100f, null, null, 0.55f);
            Triplanar("M_Ground_Path_Restored", "MudLeaves", "MossyRock", 3f, 4f, -0.05f);
            Triplanar("M_Rock_Triplanar_Degraded", "DryGroundRocks", "RockDry", 3f, 2.5f, -100f, new Color(0.92f, 0.86f, 0.76f), new Color(0.68f, 0.62f, 0.54f), 0.75f);
            Triplanar("M_Ground_Valley_Restored", "LeafyGrass", "MossyRock", 6f, 8f, -100f, new Color(0.85f, 0.95f, 0.85f), null, 0.5f);
            Triplanar("M_Ground_Valley_Degraded", "DryGroundRocks", "RockDry", 6f, 8f, -100f, new Color(0.95f, 0.88f, 0.78f), new Color(0.66f, 0.6f, 0.52f), 0.5f);
            Triplanar("M_Rock_Triplanar_Restored", "LeafyGrass", "MossyRock", 2f, 2.5f, -0.1f, null, null, 0.62f);

            // Agua
            var w = EnsureMaterial("M_Water_Stream", "Bosque/V6/Water");
            SetWater(w, new Color(0.62f, 0.82f, 0.76f), new Color(0.04f, 0.2f, 0.22f), 0.8f, 0.06f, 0.5f, 0.7f, 0.12f, 1.1f);
            w.SetFloat("_FoamDistance", 0.1f);
            w.SetFloat("_ReflectionStrength", 0.55f);
            w.SetColor("_FoamColor", new Color(0.92f, 0.95f, 0.93f, 0.75f));
            w.SetFloat("_PuddleShape", 0f);
            var st = EnsureMaterial("M_Water_Stagnant", "Bosque/V6/Water");
            SetWater(st, new Color(0.78f, 0.72f, 0.6f), new Color(0.34f, 0.3f, 0.22f), 0.06f, 0.12f, 0.02f, 0.3f, 0f, 0f);
            st.SetFloat("_FoamDistance", 0.01f);
            st.SetColor("_FoamColor", new Color(0.16f, 0.13f, 0.09f, 0f));
            st.SetFloat("_EdgeFade", 0.004f);
            st.SetFloat("_PuddleShape", 1f);
            st.SetFloat("_ReflectionStrength", 0.7f);
            st.SetFloat("_Smoothness", 0.96f);
            st.SetFloat("_Refraction", 0f);

            // Cielo: mezcla de dos HDRI CC0
            var sky = EnsureMaterial("M_Sky_Blend", "Bosque/V6/SkyBlend");
            sky.SetTexture("_TexA", Tex("Sky", "HDRI_kloofendal_overcast_puresky"));
            sky.SetTexture("_TexB", Tex("Sky", "HDRI_kloofendal_48d_partly_cloudy_puresky"));
            sky.SetFloat("_ExposureA", 0.95f);
            sky.SetFloat("_ExposureB", 1.0f);
            sky.SetColor("_TintA", new Color(1.0f, 0.93f, 0.82f));
            sky.SetFloat("_Rotation", 140f);
            AssetDatabase.SaveAssets();
        }

        private static void SetWater(Material w, Color shallow, Color deep, float depth, float murk, float flowSpeed,
                                     float waves, float flowFoam, float caustics)
        {
            w.SetColor("_ShallowColor", shallow);
            w.SetColor("_DeepColor", deep);
            w.SetFloat("_DepthDistance", depth);
            w.SetFloat("_Murkiness", murk);
            w.SetTexture("_NormalA", Tex("Textures/Water", "T_Water_NormalA"));
            w.SetTexture("_NormalB", Tex("Textures/Water", "T_Water_NormalB"));
            w.SetTexture("_NoiseTex", Tex("Textures/Water", "T_Water_Noise"));
            w.SetFloat("_FlowSpeed", flowSpeed);
            w.SetFloat("_NormalStrength", waves);
            w.SetFloat("_FlowFoam", flowFoam);
            w.SetFloat("_CausticStrength", caustics);
            w.SetVector("_FlowDirection", new Vector4(0, 1, 0, 0));
        }

        // ------------------------------------------------------------------ Modelos
        public static void ConfigureModels()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { Root + "/Models" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!(AssetImporter.GetAtPath(p) is ModelImporter mi)) continue;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.importAnimation = false;
                mi.animationType = ModelImporterAnimationType.None;
                mi.importCameras = false;
                mi.importLights = false;
                mi.importBlendShapes = false;
                mi.isReadable = false;
                mi.addCollider = false;
                mi.meshCompression = ModelImporterMeshCompression.Off;
                mi.importNormals = ModelImporterNormals.Import;
                mi.importTangents = ModelImporterTangents.CalculateMikk;
                mi.generateSecondaryUV = false;
                mi.globalScale = 1f;
                mi.useFileScale = true;
                foreach (var em in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Material>().Select(x => x.name).Distinct().ToList())
                {
                    string baseName = System.Text.RegularExpressions.Regex.Replace(em, @"\.\d{3}$", "");
                    if (baseName == "M_LeafCluster") baseName = "M_LeafClusterGreen";
                    var target = AssetDatabase.LoadAssetAtPath<Material>($"{Mat}/{baseName}.mat");
                    if (target != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), em), target);
                    else Debug.LogWarning($"[V6] Sin material para {em} en {p}");
                }
                mi.SaveAndReimport();
            }
        }

        public static void ReportModels()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { Root + "/Models" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                var b = new Bounds(); bool first = true; int tris = 0;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    var mb = mf.sharedMesh.bounds;
                    var m = mf.transform.localToWorldMatrix;
                    var wb = new Bounds(m.MultiplyPoint3x4(mb.center), Vector3.zero);
                    foreach (var c in Corners(mb)) wb.Encapsulate(m.MultiplyPoint3x4(c));
                    if (first) { b = wb; first = false; } else b.Encapsulate(wb);
                    tris += mf.sharedMesh.triangles.Length / 3;
                }
                var mats = string.Join(",", go.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Select(x => x ? x.name : "NULL"));
                Debug.Log($"[V6][Model] {Path.GetFileNameWithoutExtension(p)} size={b.size.ToString("F2")} min={b.min.ToString("F2")} tris={tris} mats={mats}");
            }
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (int i = 0; i < 8; i++)
                yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
        }

        public static bool ReportShaders()
        {
            bool ok = true;
            foreach (var g in AssetDatabase.FindAssets("t:Shader", new[] { Root + "/Shaders" }))
            {
                var s = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(g));
                var msgs = ShaderUtil.GetShaderMessages(s);
                int errors = msgs.Count(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error);
                Debug.Log($"[V6][Shader] {s.name} supported={s.isSupported} errors={errors} messages={msgs.Length}");
                foreach (var m in msgs) Debug.Log($"[V6][Shader]   {m.severity} {m.file}:{m.line} {m.message}");
                if (errors > 0 || !s.isSupported) ok = false;
            }
            return ok;
        }

        // ------------------------------------------------------------------ Prefabs
        private static readonly string[] SmallFoliage = { "Grass", "Flower", "Dandelion", "Shrub" };

        public static GameObject Model(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{name}.fbx") ?? throw new FileNotFoundException(name);

        public static Material M(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{Mat}/{name}.mat") ?? throw new FileNotFoundException(name);

        public static GameObject Prefab(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{Pfb}/{name}.prefab") ?? throw new FileNotFoundException(name);

        /// <summary>Corte por tamaño en pantalla: la vegetación pequeña desaparece a distancia (la niebla lo disimula).</summary>
        private static float CullFor(string name)
        {
            if (name.Contains("Grass") || name.Contains("Flower") || name.Contains("Dandelion")) return 0.032f;
            if (name.Contains("Shrub") || name.Contains("Fern")) return 0.02f;
            if (name.Contains("DryBranch")) return 0.015f;
            if (name.Contains("Cliff") || name.Contains("RockMossLarge")) return 0.003f;
            return 0.006f;
        }

        private static void AddCull(GameObject go, float cull)
        {
            if (go.GetComponent<LODGroup>() != null) return;
            var lg = go.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(cull, go.GetComponentsInChildren<Renderer>()) });
            lg.RecalculateBounds();
        }

        private static void SavePrefab(GameObject go, string name)
        {
            Directory.CreateDirectory(Pfb);
            AddCull(go, CullFor(name));
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            PrefabUtility.SaveAsPrefabAsset(go, $"{Pfb}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static GameObject Wrap(string prefabName, params (string model, Material mat)[] parts)
        {
            var root = new GameObject(prefabName);
            foreach (var (model, mat) in parts)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(Model(model));
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                inst.transform.SetParent(root.transform, false);
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>())
                {
                    if (mat != null) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
                    bool small = SmallFoliage.Any(model.Contains);
                    r.shadowCastingMode = small ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    r.receiveShadows = true;
                    r.lightProbeUsage = LightProbeUsage.BlendProbes;
                }
            }
            return root;
        }

        public static void BuildPrefabs()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { Root + "/Models" }))
            {
                string model = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g));
                if (model.Contains("TreeBroadleaf")) continue;
                SavePrefab(Wrap("VIS_V6_" + model.Replace("SM_", ""), (model, null)), "VIS_V6_" + model.Replace("SM_", ""));
                if (model.StartsWith("SM_GrassA") || model.StartsWith("SM_GrassB"))
                {
                    var dry = M(model.StartsWith("SM_GrassA") ? "M_GrassA_Dry" : "M_GrassB_Dry");
                    SavePrefab(Wrap("VIS_V6_" + model.Replace("SM_", "") + "_Dry", (model, dry)), "VIS_V6_" + model.Replace("SM_", "") + "_Dry");
                }
            }
            SavePrefab(TreeLod(Wrap("VIS_V6_TreeA_Green", ("SM_TreeBroadleaf_A", null), ("SM_TreeBroadleaf_A_Canopy", M("M_LeafClusterGreen"))), "SM_TreeBroadleaf_A"), "VIS_V6_TreeA_Green");
            SavePrefab(TreeLod(Wrap("VIS_V6_TreeA_Dead", ("SM_TreeBroadleaf_A", null), ("SM_TreeBroadleaf_A_Canopy", M("M_LeafClusterDry"))), "SM_TreeBroadleaf_A"), "VIS_V6_TreeA_Dead");
            SavePrefab(TreeLod(Wrap("VIS_V6_TreeB_Green", ("SM_TreeBroadleaf_B", null), ("SM_TreeBroadleaf_B_Canopy", M("M_LeafClusterLush"))), "SM_TreeBroadleaf_B"), "VIS_V6_TreeB_Green");
            SavePrefab(TreeLod(Wrap("VIS_V6_TreeB_Dead", ("SM_TreeBroadleaf_B", null), ("SM_TreeBroadleaf_B_Canopy", M("M_LeafClusterDry"))), "SM_TreeBroadleaf_B"), "VIS_V6_TreeB_Dead");
            // Variante para la orilla jugable: sin tarjetas de hojas por debajo de 2 m (la copa queda sobre la cámara).
            var high = HighCanopy("SM_TreeBroadleaf_B_Canopy", 2.0f);
            foreach (var (suffix, leaf) in new[] { ("Green", "M_LeafClusterLush"), ("Dead", "M_LeafClusterDry") })
            {
                var go = Wrap("VIS_V6_TreeB_Tall_" + suffix, ("SM_TreeBroadleaf_B", null));
                var c = new GameObject("Canopy_High", typeof(MeshFilter), typeof(MeshRenderer));
                c.transform.SetParent(go.transform, false);
                c.GetComponent<MeshFilter>().sharedMesh = high;
                var r = c.GetComponent<MeshRenderer>(); r.sharedMaterial = M(leaf); r.shadowCastingMode = ShadowCastingMode.On;
                SavePrefab(TreeLod(go, "SM_TreeBroadleaf_B"), "VIS_V6_TreeB_Tall_" + suffix);
            }
            // Rocas grandes con material triplanar por estado (cubren bordes de bloques)
            for (int i = 0; i < 6; i++)
            {
                string m = "SM_RockMossLarge_" + (char)('A' + i);
                SavePrefab(Wrap($"VIS_V6_Cliff_{(char)('A' + i)}_Degraded", (m, M("M_Rock_Triplanar_Degraded"))), $"VIS_V6_Cliff_{(char)('A' + i)}_Degraded");
                SavePrefab(Wrap($"VIS_V6_Cliff_{(char)('A' + i)}_Restored", (m, M("M_Rock_Triplanar_Restored"))), $"VIS_V6_Cliff_{(char)('A' + i)}_Restored");
            }
        }

        /// <summary>Árbol con LOD: madera completa de cerca, madera reducida (≈2.6k tri) a distancia; la copa se comparte.</summary>
        private static GameObject TreeLod(GameObject root, string woodModel)
        {
            var wood0 = root.GetComponentsInChildren<MeshRenderer>().Where(r => r.GetComponent<MeshFilter>().sharedMesh.name == woodModel).ToArray();
            var canopy = root.GetComponentsInChildren<MeshRenderer>().Except(wood0).ToArray();
            var lodInst = (GameObject)PrefabUtility.InstantiatePrefab(Model(woodModel + "_LOD1"));
            PrefabUtility.UnpackPrefabInstance(lodInst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            lodInst.transform.SetParent(root.transform, false);
            var wood1 = lodInst.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < wood1.Length; i++) wood1[i].sharedMaterials = wood0[0].sharedMaterials;
            var lg = root.AddComponent<LODGroup>();
            lg.SetLODs(new[]
            {
                new LOD(0.22f, wood0.Concat(canopy).ToArray()),
                new LOD(0.006f, wood1.Concat(canopy).ToArray()),
            });
            lg.RecalculateBounds();
            return root;
        }

        /// <summary>Copia de una copa de tarjetas conservando solo los quads cuyo vértice más bajo supera minY.</summary>
        private static Mesh HighCanopy(string model, float minY)
        {
            var src = Model(model).GetComponentInChildren<MeshFilter>().sharedMesh;
            var v = src.vertices; var n = src.normals; var uv = src.uv; var tri = src.triangles;
            var keep = new List<int>();
            for (int t = 0; t + 5 < tri.Length; t += 6)
            {
                float lo = float.MaxValue;
                for (int k = 0; k < 6; k++) lo = Mathf.Min(lo, v[tri[t + k]].y);
                if (lo >= minY) for (int k = 0; k < 6; k++) keep.Add(tri[t + k]);
            }
            var m = new Mesh { name = model + "_High" };
            m.vertices = v; m.normals = n; m.uv = uv; m.triangles = keep.ToArray();
            m.RecalculateBounds(); m.RecalculateTangents();
            Directory.CreateDirectory(Root + "/Models/Generated");
            string path = $"{Root}/Models/Generated/{m.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(m, existing); return existing; }
            AssetDatabase.CreateAsset(m, path);
            Debug.Log($"[V6] {m.name}: {keep.Count / 3} de {tri.Length / 3} triángulos (y >= {minY} m)");
            return m;
        }

        // ------------------------------------------------------------------ Post-proceso
        public static void BuildVolumes()
        {
            var deg = EnsureProfile("VP_V6_Degraded");
            Tonemap(deg);
            var ca = Get<ColorAdjustments>(deg);
            ca.postExposure.Override(-0.1f); ca.contrast.Override(14f); ca.saturation.Override(-42f);
            ca.colorFilter.Override(new Color(0.97f, 0.95f, 0.9f));
            var wb = Get<WhiteBalance>(deg); wb.temperature.Override(6f); wb.tint.Override(2f);
            var bl = Get<Bloom>(deg); bl.threshold.Override(1.1f); bl.intensity.Override(0.25f); bl.scatter.Override(0.6f);
            var vg = Get<Vignette>(deg); vg.intensity.Override(0.3f); vg.smoothness.Override(0.45f);
            var fg = Get<FilmGrain>(deg); fg.type.Override(FilmGrainLookup.Thin1); fg.intensity.Override(0.12f);
            var smh = Get<ShadowsMidtonesHighlights>(deg); smh.shadows.Override(new Vector4(1f, 0.97f, 0.92f, -0.03f));

            var res = EnsureProfile("VP_V6_Restored");
            Tonemap(res);
            var ca2 = Get<ColorAdjustments>(res);
            ca2.postExposure.Override(0.1f); ca2.contrast.Override(12f); ca2.saturation.Override(10f);
            ca2.colorFilter.Override(Color.white);
            var wb2 = Get<WhiteBalance>(res); wb2.temperature.Override(5f); wb2.tint.Override(-3f);
            var bl2 = Get<Bloom>(res); bl2.threshold.Override(0.95f); bl2.intensity.Override(0.55f); bl2.scatter.Override(0.7f);
            bl2.tint.Override(new Color(1f, 0.96f, 0.88f));
            var vg2 = Get<Vignette>(res); vg2.intensity.Override(0.2f); vg2.smoothness.Override(0.5f);
            var smh2 = Get<ShadowsMidtonesHighlights>(res); smh2.shadows.Override(new Vector4(0.94f, 0.98f, 1.04f, 0f));
            smh2.highlights.Override(new Vector4(1.04f, 1.0f, 0.95f, 0f));
            AssetDatabase.SaveAssets();
        }

        private static void Tonemap(VolumeProfile p)
        {
            var t = Get<Tonemapping>(p); t.mode.Override(TonemappingMode.ACES);
        }

        private static VolumeProfile EnsureProfile(string name)
        {
            string path = $"{Root}/PostFX/{name}.asset";
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null)
            {
                Directory.CreateDirectory($"{Root}/PostFX");
                p = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(p, path);
            }
            return p;
        }

        private static T Get<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (!p.TryGet<T>(out var c))
            {
                c = p.Add<T>(true);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, p);
            }
            c.active = true;
            EditorUtility.SetDirty(c);
            EditorUtility.SetDirty(p);
            return c;
        }
    }
}
