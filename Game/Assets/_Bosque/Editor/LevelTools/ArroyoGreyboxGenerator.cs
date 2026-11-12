using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bosque.LevelTools
{
    /// <summary>
    /// C-01 / T07. Genera o actualiza la escena greybox del arroyo con primitivas.
    /// Sin gameplay: solo geometría, colliders, marcadores de contrato y grupos visuales.
    ///
    /// Trazado (1 unidad = 1 metro, Y arriba, el recorrido avanza hacia +Z):
    ///   - Orilla transitable: X -1..6, Z -4..40, superficie Y = 0.
    ///   - Cauce seco: X -4..-1, fondo Y = -0.6, con dos rampas de acceso.
    ///   - Muro de roca oeste, muros sur y norte; borde este abierto entre Z 12 y 28 (caída).
    ///   - Obstáculos en el cauce en Z 10, 18 y 26. Salida con portal en Z 36–38.5.
    /// </summary>
    public static class ArroyoGreyboxGenerator
    {
        public const string LevelFolder = "Assets/_Bosque/Levels/Arroyo";
        public const string ScenePath = LevelFolder + "/ArroyoGreybox.unity";
        public const string MaterialsFolderName = "GreyboxMaterials";
        public const string MaterialsFolder = LevelFolder + "/" + MaterialsFolderName;

        public const string RootName = "ArroyoGreybox";

        // Nombres de contrato (Docs/CONTRATOS.md). No renombrar sin acordarlo con Codex.
        public const string PlayerSpawn = "PlayerSpawn";
        public const string SafeRespawn = "SafeRespawn";
        public const string ExitMarker = "ExitMarker";
        public static readonly string[] ObstacleNames = { "Obstacle_01", "Obstacle_02", "Obstacle_03" };
        public const string DegradedVisuals = "DegradedVisuals";
        public const string RestoredVisuals = "RestoredVisuals";

        // Puntos seguros adicionales: propuesta de nivel, fuera del contrato actual.
        public static readonly string[] SafePointNames = { "SafePoint_01", "SafePoint_02", "SafePoint_03" };

        // Medidas del nivel (metros).
        public const float BankTopY = 0f;
        public const float ChannelBottomY = -0.6f;
        public const float ChannelXMin = -4f;
        public const float ChannelXMax = -1f;
        public const float BankXMax = 6f;
        public const float LevelZMin = -4f;
        public const float LevelZMax = 40f;
        public const float FallEdgeZMin = 12f;
        public const float FallEdgeZMax = 28f;

        public static readonly Vector3 PlayerSpawnPos = new Vector3(3f, BankTopY, 0f);
        public static readonly Vector3 SafeRespawnPos = new Vector3(3f, BankTopY, 2f);
        public static readonly Vector3 ExitMarkerPos = new Vector3(3f, BankTopY, 36f);
        public static readonly float[] ObstacleZ = { 10f, 18f, 26f };

        private const string MenuGenerate = "Tools/Bosque/Niveles/Arroyo - Generar o actualizar greybox";
        private const string MenuValidate = "Tools/Bosque/Niveles/Arroyo - Validar escena";

        private sealed class Palette
        {
            public Material Bank, ChannelBed, Rock, Wood, ObstacleRock, Debris, DryGrass, DeadFoliage,
                Foliage, Grass, Water, FlowerA, FlowerB, Stem, ExitStone, Helper;
        }

        [MenuItem(MenuGenerate, priority = 100)]
        public static void GenerateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Generate();
                var report = ArroyoGreyboxValidator.ValidateOpenScene();
                report.LogToConsole();
                EditorUtility.DisplayDialog("Arroyo greybox",
                    report.Ok
                        ? "Escena generada y validada:\n" + ScenePath
                        : "Escena generada con " + report.Errors.Count + " error(es) de validación. Revisa la consola.",
                    "Aceptar");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Arroyo greybox", "Falló la generación: " + e.Message, "Aceptar");
            }
        }

        [MenuItem(MenuValidate, priority = 101)]
        public static void ValidateFromMenu()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                {
                    EditorUtility.DisplayDialog("Arroyo greybox", "La escena no existe todavía. Genera primero.", "Aceptar");
                    return;
                }
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            var report = ArroyoGreyboxValidator.ValidateOpenScene();
            report.LogToConsole();
            EditorUtility.DisplayDialog("Arroyo greybox",
                report.Ok ? "Validación correcta." : report.Errors.Count + " error(es). Revisa la consola.", "Aceptar");
        }

        /// <summary>
        /// Entrada para modo batch:
        /// Unity.exe -batchmode -projectPath Game -executeMethod Bosque.LevelTools.ArroyoGreyboxGenerator.GenerateAndValidateBatch -logFile ...
        /// Código de salida: 0 correcto, 1 excepción, 2 validación fallida.
        /// </summary>
        public static void GenerateAndValidateBatch()
        {
            int code;
            try
            {
                Generate();
                var report = ArroyoGreyboxValidator.ValidateOpenScene();
                report.LogToConsole();
                code = report.Ok ? 0 : 2;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        /// <summary>Solo valida la escena existente (batch). No la modifica.</summary>
        public static void ValidateBatch()
        {
            int code;
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var report = ArroyoGreyboxValidator.ValidateOpenScene();
                report.LogToConsole();
                code = report.Ok ? 0 : 2;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static void Generate()
        {
            var palette = BuildMaterials();
            var scene = OpenOrCreateScene();
            var root = FindOrCreateRoot(scene);
            var b = new GreyboxBuilder();

            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            BuildGeometry(b, root, palette);
            BuildMarkers(b, root);
            BuildObstacles(b, root, palette);
            BuildExit(b, root, palette);
            BuildDegradedVisuals(b, root, palette);
            BuildRestoredVisuals(b, root, palette);
            BuildLighting(b, root);
            BuildPreviewCamera(b, root);
            BuildEditorHelpers(b, root, palette);

            // Estado inicial del contrato.
            b.Group(root, DegradedVisuals).gameObject.SetActive(true);
            b.Group(root, RestoredVisuals).gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("No se pudo guardar " + ScenePath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[ArroyoGreybox] Escena guardada en " + ScenePath + ". Objetos creados: " + b.Created +
                      ", actualizados: " + b.Updated + ".");
        }

        // ------------------------------------------------------------------ Escena

        private static Scene OpenOrCreateScene()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == ScenePath && active.isLoaded) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("No se pudo crear " + ScenePath);
            }
            return scene;
        }

        private static Transform FindOrCreateRoot(Scene scene)
        {
            var matches = new List<GameObject>();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == RootName) matches.Add(go);
            }
            if (matches.Count > 1)
            {
                throw new InvalidOperationException("Hay " + matches.Count + " raíces '" + RootName +
                                                    "'. Resuélvelo a mano; la herramienta no borra contenido.");
            }
            if (matches.Count == 1) return matches[0].transform;
            return new GameObject(RootName).transform;
        }

        private static Palette BuildMaterials()
        {
            GreyboxBuilder.EnsureFolder(LevelFolder, MaterialsFolderName);
            string f = MaterialsFolder;
            return new Palette
            {
                Bank = GreyboxBuilder.EnsureMaterial(f, "AG_Bank", new Color(0.62f, 0.50f, 0.33f), 0.1f),
                ChannelBed = GreyboxBuilder.EnsureMaterial(f, "AG_ChannelBed", new Color(0.45f, 0.36f, 0.26f), 0.05f),
                Rock = GreyboxBuilder.EnsureMaterial(f, "AG_Rock", new Color(0.48f, 0.47f, 0.45f), 0.15f),
                Wood = GreyboxBuilder.EnsureMaterial(f, "AG_Wood", new Color(0.36f, 0.24f, 0.15f), 0.1f),
                ObstacleRock = GreyboxBuilder.EnsureMaterial(f, "AG_ObstacleRock", new Color(0.33f, 0.33f, 0.36f), 0.2f),
                Debris = GreyboxBuilder.EnsureMaterial(f, "AG_Debris", new Color(0.50f, 0.38f, 0.22f), 0.05f),
                DryGrass = GreyboxBuilder.EnsureMaterial(f, "AG_DryGrass", new Color(0.74f, 0.62f, 0.36f), 0.05f),
                DeadFoliage = GreyboxBuilder.EnsureMaterial(f, "AG_DeadFoliage", new Color(0.42f, 0.38f, 0.32f), 0.05f),
                Foliage = GreyboxBuilder.EnsureMaterial(f, "AG_Foliage", new Color(0.20f, 0.52f, 0.22f), 0.2f),
                Grass = GreyboxBuilder.EnsureMaterial(f, "AG_Grass", new Color(0.36f, 0.62f, 0.28f), 0.15f),
                Water = GreyboxBuilder.EnsureMaterial(f, "AG_Water", new Color(0.20f, 0.48f, 0.72f), 0.85f),
                FlowerA = GreyboxBuilder.EnsureMaterial(f, "AG_FlowerYellow", new Color(0.95f, 0.82f, 0.20f), 0.3f),
                FlowerB = GreyboxBuilder.EnsureMaterial(f, "AG_FlowerWhite", new Color(0.95f, 0.93f, 0.90f), 0.3f),
                Stem = GreyboxBuilder.EnsureMaterial(f, "AG_Stem", new Color(0.25f, 0.45f, 0.18f), 0.1f),
                ExitStone = GreyboxBuilder.EnsureMaterial(f, "AG_ExitStone", new Color(0.78f, 0.76f, 0.70f), 0.2f),
                Helper = GreyboxBuilder.EnsureMaterial(f, "AG_EditorHelper", new Color(0.90f, 0.30f, 0.60f), 0.0f),
            };
        }

        // ------------------------------------------------------------------ Geometría física

        private static readonly Vector3[] TreeBases =
        {
            // Sobre el muro oeste (Y = 3) y en la orilla, en tramos con barandilla.
            new Vector3(-4.75f, 3f, 3f),
            new Vector3(-4.75f, 3f, 15f),
            new Vector3(-4.75f, 3f, 27f),
            new Vector3(-4.75f, 3f, 37f),
            new Vector3(5.1f, 0f, -2.5f),
            new Vector3(5.1f, 0f, 8f),
            new Vector3(5.1f, 0f, 33f),
        };

        private const float TrunkHeight = 3f;

        private static void BuildGeometry(GreyboxBuilder b, Transform root, Palette p)
        {
            var g = b.Group(root, "Geometry");
            float lengthZ = LevelZMax - LevelZMin;
            float midZ = (LevelZMax + LevelZMin) * 0.5f;

            // Orilla: losa de 1 m de espesor con la cara superior en Y = 0.
            b.Box(g, "Bank", new Vector3((ChannelXMax + BankXMax) * 0.5f, BankTopY - 0.5f, midZ),
                new Vector3(BankXMax - ChannelXMax, 1f, lengthZ), p.Bank, true);

            // Fondo del cauce.
            b.Box(g, "ChannelFloor", new Vector3((ChannelXMin + ChannelXMax) * 0.5f, ChannelBottomY - 0.5f, midZ),
                new Vector3(ChannelXMax - ChannelXMin, 1f, lengthZ), p.ChannelBed, true);

            // Muros perimetrales (Y -1..3).
            b.Box(g, "WestWall", new Vector3(-4.75f, 1f, midZ), new Vector3(1.5f, 4f, lengthZ), p.Rock, true);
            b.Box(g, "SouthWall", new Vector3(0.25f, 1f, LevelZMin - 0.75f), new Vector3(11.5f, 4f, 1.5f), p.Rock, true);
            b.Box(g, "NorthWall", new Vector3(0.25f, 1f, LevelZMax + 0.75f), new Vector3(11.5f, 4f, 1.5f), p.Rock, true);

            // Barandilla baja de rocas en el borde este; hueco entre Z 12 y 28 para probar caídas.
            float southLen = FallEdgeZMin - LevelZMin;
            float northLen = LevelZMax - FallEdgeZMax;
            b.Box(g, "EastRail_South", new Vector3(5.75f, 0.25f, LevelZMin + southLen * 0.5f),
                new Vector3(0.5f, 0.5f, southLen), p.Rock, true);
            b.Box(g, "EastRail_North", new Vector3(5.75f, 0.25f, FallEdgeZMax + northLen * 0.5f),
                new Vector3(0.5f, 0.5f, northLen), p.Rock, true);

            // Rampas de acceso al cauce: de (X -1, Y 0) a (X -3, Y -0.6). Pendiente ≈ 16.7°.
            BuildRamp(b, g, "Ramp_South", 5f, p.Bank);
            BuildRamp(b, g, "Ramp_North", 31.5f, p.Bank);

            // Tronco bajo opcional (0.35 m). Se puede rodear por el este (2 m libres).
            b.Primitive(PrimitiveType.Cylinder, g, "LowLog_Optional", new Vector3(1.75f, 0.175f, 30f),
                new Vector3(0f, 0f, 90f), new Vector3(0.35f, 1.75f, 0.35f), p.Wood, true);

            // Troncos de árbol: física común a ambos estados. Las copas cambian por grupo visual.
            var trees = b.Group(g, "Trees");
            for (int i = 0; i < TreeBases.Length; i++)
            {
                var tree = b.Group(trees, TreeName(i));
                tree.localPosition = TreeBases[i];
                tree.localRotation = Quaternion.identity;
                tree.localScale = Vector3.one;
                b.Primitive(PrimitiveType.Cylinder, tree, "Trunk", new Vector3(0f, TrunkHeight * 0.5f, 0f),
                    Vector3.zero, new Vector3(0.4f, TrunkHeight * 0.5f, 0.4f), p.Wood, true);
            }
        }

        private static void BuildRamp(GreyboxBuilder b, Transform parent, string name, float z, Material mat)
        {
            const float thickness = 0.2f;
            const float width = 2.5f;
            var top = new Vector2(ChannelXMax, BankTopY);
            var bottom = new Vector2(ChannelXMax - 2f, ChannelBottomY);
            Vector2 d = top - bottom;
            float length = d.magnitude;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Vector2 mid = (top + bottom) * 0.5f;
            float rad = angle * Mathf.Deg2Rad;
            var up = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
            Vector2 c = mid - up * (thickness * 0.5f);
            b.Box(parent, name, new Vector3(c.x, c.y, z), new Vector3(0f, 0f, angle),
                new Vector3(length, thickness, width), mat, true);
        }

        private static string TreeName(int i) => "Tree_" + (i + 1).ToString("00");

        // ------------------------------------------------------------------ Marcadores

        private static void BuildMarkers(GreyboxBuilder b, Transform root)
        {
            var m = b.Group(root, "Markers");

            var spawn = b.UniqueEmpty(root, m, PlayerSpawn, PlayerSpawnPos, 0f);
            GreyboxBuilder.SetMarkerIcon(spawn.gameObject, "sv_label_3");

            var respawn = b.UniqueEmpty(root, m, SafeRespawn, SafeRespawnPos, 0f);
            GreyboxBuilder.SetMarkerIcon(respawn.gameObject, "sv_label_4");

            var safe = b.Group(m, "SafePoints");
            var safePositions = new[]
            {
                new Vector3(2f, BankTopY, 11f),
                new Vector3(1f, BankTopY, 20f),
                new Vector3(3f, BankTopY, 33f),
            };
            for (int i = 0; i < SafePointNames.Length; i++)
            {
                var sp = b.UniqueEmpty(root, safe, SafePointNames[i], safePositions[i], 0f);
                GreyboxBuilder.SetMarkerIcon(sp.gameObject, "sv_label_4");
            }
        }

        // ------------------------------------------------------------------ Obstáculos

        private static void BuildObstacles(GreyboxBuilder b, Transform root, Palette p)
        {
            var group = b.Group(root, "Obstacles");
            float cx = (ChannelXMin + ChannelXMax) * 0.5f;

            // Obstacle_01: tronco caído atravesado en el cauce.
            var o1 = b.UniqueEmpty(root, group, ObstacleNames[0], new Vector3(cx, ChannelBottomY + 0.4f, ObstacleZ[0]), 0f);
            GreyboxBuilder.EnsureBoxCollider(o1.gameObject, Vector3.zero, new Vector3(2.9f, 0.8f, 0.8f));
            b.Primitive(PrimitiveType.Cylinder, o1, "Visual_Log", Vector3.zero, new Vector3(0f, 0f, 90f),
                new Vector3(0.8f, 1.45f, 0.8f), p.Wood, false);
            b.Primitive(PrimitiveType.Cylinder, o1, "Visual_Branch", new Vector3(0.6f, 0.45f, 0.1f),
                new Vector3(20f, 0f, -35f), new Vector3(0.15f, 0.45f, 0.15f), p.Wood, false);
            GreyboxBuilder.SetMarkerIcon(o1.gameObject, "sv_label_6");

            // Obstacle_02: montón de rocas.
            var o2 = b.UniqueEmpty(root, group, ObstacleNames[1], new Vector3(cx, ChannelBottomY + 0.5f, ObstacleZ[1]), 0f);
            GreyboxBuilder.EnsureBoxCollider(o2.gameObject, Vector3.zero, new Vector3(2.8f, 1.0f, 1.4f));
            b.Primitive(PrimitiveType.Sphere, o2, "Visual_Rock_A", new Vector3(-0.8f, 0f, 0f), Vector3.zero,
                new Vector3(1.2f, 1.0f, 1.2f), p.ObstacleRock, false);
            b.Primitive(PrimitiveType.Sphere, o2, "Visual_Rock_B", new Vector3(0.25f, -0.05f, 0.1f), new Vector3(0f, 30f, 0f),
                new Vector3(1.1f, 0.9f, 1.0f), p.ObstacleRock, false);
            b.Primitive(PrimitiveType.Sphere, o2, "Visual_Rock_C", new Vector3(0.95f, -0.1f, -0.1f), Vector3.zero,
                new Vector3(0.85f, 0.8f, 0.9f), p.ObstacleRock, false);
            GreyboxBuilder.SetMarkerIcon(o2.gameObject, "sv_label_6");

            // Obstacle_03: acumulación de ramas y hojarasca.
            var o3 = b.UniqueEmpty(root, group, ObstacleNames[2], new Vector3(cx, ChannelBottomY + 0.45f, ObstacleZ[2]), 0f);
            GreyboxBuilder.EnsureBoxCollider(o3.gameObject, Vector3.zero, new Vector3(2.8f, 0.9f, 1.2f));
            b.Box(o3, "Visual_Branch_A", new Vector3(0f, -0.2f, 0.15f), new Vector3(0f, 18f, 8f),
                new Vector3(2.6f, 0.22f, 0.22f), p.Debris, false);
            b.Box(o3, "Visual_Branch_B", new Vector3(0.1f, 0.0f, -0.15f), new Vector3(0f, -22f, -6f),
                new Vector3(2.5f, 0.2f, 0.2f), p.Debris, false);
            b.Primitive(PrimitiveType.Sphere, o3, "Visual_LeafPile", new Vector3(-0.4f, 0.1f, 0f), Vector3.zero,
                new Vector3(1.6f, 0.6f, 1.0f), p.DeadFoliage, false);
            GreyboxBuilder.SetMarkerIcon(o3.gameObject, "sv_label_6");
        }

        // ------------------------------------------------------------------ Salida

        private static void BuildExit(GreyboxBuilder b, Transform root, Palette p)
        {
            var exit = b.Group(root, "Exit");
            var marker = b.UniqueEmpty(root, exit, ExitMarker, ExitMarkerPos, 0f);
            GreyboxBuilder.SetMarkerIcon(marker.gameObject, "sv_label_1");

            // Solo visual (sin collider): señala la zona de llegada.
            b.Box(exit, "ExitPad", new Vector3(ExitMarkerPos.x, 0.02f, ExitMarkerPos.z), new Vector3(4f, 0.04f, 4f),
                p.ExitStone, false);

            // Portal físico: hueco libre de 4.2 m de ancho y 2.5 m de alto.
            b.Box(exit, "GatePillar_W", new Vector3(0.5f, 1.25f, 38.5f), new Vector3(0.8f, 2.5f, 0.8f), p.Rock, true);
            b.Box(exit, "GatePillar_E", new Vector3(5.5f, 1.25f, 38.5f), new Vector3(0.8f, 2.5f, 0.8f), p.Rock, true);
            b.Box(exit, "GateLintel", new Vector3(3f, 2.7f, 38.5f), new Vector3(5.8f, 0.4f, 0.8f), p.Rock, true);
        }

        // ------------------------------------------------------------------ Visuales por estado (sin colliders)

        private static void BuildDegradedVisuals(GreyboxBuilder b, Transform root, Palette p)
        {
            var g = b.Group(root, DegradedVisuals);
            float cx = (ChannelXMin + ChannelXMax) * 0.5f;

            // Barro agrietado en el cauce.
            float[] bedZ = { -1f, 6f, 14f, 22f, 30f, 37f };
            for (int i = 0; i < bedZ.Length; i++)
            {
                b.Box(g, "DryBedCrack_" + (i + 1).ToString("00"), new Vector3(cx, ChannelBottomY + 0.01f, bedZ[i]),
                    new Vector3(0f, i * 23f, 0f), new Vector3(1.8f, 0.02f, 2.2f), p.DeadFoliage, false);
            }

            // Hierba seca en la orilla.
            var grass = new[]
            {
                new Vector3(4.5f, 0f, 4f), new Vector3(0.5f, 0f, 9f), new Vector3(4f, 0f, 16f),
                new Vector3(0.2f, 0f, 24f), new Vector3(4.6f, 0f, 29f), new Vector3(0.6f, 0f, 34f),
            };
            for (int i = 0; i < grass.Length; i++)
            {
                b.Box(g, "DryGrass_" + (i + 1).ToString("00"), grass[i] + new Vector3(0f, 0.01f, 0f),
                    new Vector3(0f, i * 31f, 0f), new Vector3(1.6f, 0.02f, 1.2f), p.DryGrass, false);
            }

            // Arbustos secos, bajos y escasos.
            var bushes = new[] { new Vector3(5.2f, 0f, 3f), new Vector3(-0.5f, 0f, 14.5f), new Vector3(5.2f, 0f, 36f) };
            for (int i = 0; i < bushes.Length; i++)
            {
                b.Primitive(PrimitiveType.Sphere, g, "DeadBush_" + (i + 1).ToString("00"),
                    bushes[i] + new Vector3(0f, 0.2f, 0f), Vector3.zero, new Vector3(0.6f, 0.4f, 0.6f), p.DeadFoliage, false);
            }

            // Ramas desnudas en lugar de copas.
            for (int i = 0; i < TreeBases.Length; i++)
            {
                var canopy = b.Group(g, "DeadCanopy_" + TreeName(i));
                canopy.localPosition = TreeBases[i] + new Vector3(0f, TrunkHeight, 0f);
                canopy.localRotation = Quaternion.identity;
                canopy.localScale = Vector3.one;
                b.Primitive(PrimitiveType.Cylinder, canopy, "Branch_A", new Vector3(0.3f, 0.4f, 0f),
                    new Vector3(0f, 0f, -35f), new Vector3(0.12f, 0.55f, 0.12f), p.DeadFoliage, false);
                b.Primitive(PrimitiveType.Cylinder, canopy, "Branch_B", new Vector3(-0.25f, 0.35f, 0.1f),
                    new Vector3(15f, 0f, 30f), new Vector3(0.1f, 0.45f, 0.1f), p.DeadFoliage, false);
            }

            // Zarzas secas en el portal (solo visual; el bloqueo lógico es de T09).
            var brambles = b.Group(g, "ExitBrambles");
            brambles.localPosition = new Vector3(3f, 0f, 38.5f);
            brambles.localRotation = Quaternion.identity;
            brambles.localScale = Vector3.one;
            b.Primitive(PrimitiveType.Cylinder, brambles, "Bramble_A", new Vector3(0f, 0.8f, 0f),
                new Vector3(0f, 0f, 60f), new Vector3(0.12f, 2.0f, 0.12f), p.DeadFoliage, false);
            b.Primitive(PrimitiveType.Cylinder, brambles, "Bramble_B", new Vector3(0f, 0.8f, 0.05f),
                new Vector3(0f, 0f, -60f), new Vector3(0.12f, 2.0f, 0.12f), p.DeadFoliage, false);
            b.Primitive(PrimitiveType.Sphere, brambles, "Bramble_Clump", new Vector3(0f, 0.35f, 0f), Vector3.zero,
                new Vector3(2.5f, 0.7f, 0.6f), p.DeadFoliage, false);
        }

        private static void BuildRestoredVisuals(GreyboxBuilder b, Transform root, Palette p)
        {
            var g = b.Group(root, RestoredVisuals);
            float cx = (ChannelXMin + ChannelXMax) * 0.5f;

            // Lámina de agua: superficie en Y -0.3, sin collider.
            b.Box(g, "Water", new Vector3(cx, -0.45f, (LevelZMin + LevelZMax) * 0.5f),
                new Vector3(ChannelXMax - ChannelXMin - 0.02f, 0.3f, LevelZMax - LevelZMin - 0.02f), p.Water, false);

            var grass = new[]
            {
                new Vector3(4.5f, 0f, 4f), new Vector3(0.5f, 0f, 9f), new Vector3(4f, 0f, 16f),
                new Vector3(0.2f, 0f, 24f), new Vector3(4.6f, 0f, 29f), new Vector3(0.6f, 0f, 34f),
                new Vector3(2.5f, 0f, 20f), new Vector3(3f, 0f, 11f),
            };
            for (int i = 0; i < grass.Length; i++)
            {
                b.Box(g, "Grass_" + (i + 1).ToString("00"), grass[i] + new Vector3(0f, 0.015f, 0f),
                    new Vector3(0f, i * 31f, 0f), new Vector3(2.2f, 0.03f, 1.8f), p.Grass, false);
            }

            // Arbustos verdes, más grandes y numerosos: la silueta cambia, no solo el color.
            var bushes = new[]
            {
                new Vector3(5.2f, 0f, 3f), new Vector3(-0.5f, 0f, 14.5f), new Vector3(5.2f, 0f, 36f),
                new Vector3(5.2f, 0f, 10.5f), new Vector3(-0.5f, 0f, 28.5f),
            };
            for (int i = 0; i < bushes.Length; i++)
            {
                b.Primitive(PrimitiveType.Sphere, g, "Bush_" + (i + 1).ToString("00"),
                    bushes[i] + new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(1.1f, 0.9f, 1.1f), p.Foliage, false);
            }

            // Flores: tallo + corola (forma además de color).
            var flowers = new[]
            {
                new Vector3(4.2f, 0f, 1.5f), new Vector3(0.3f, 0f, 7.5f), new Vector3(4.8f, 0f, 17f),
                new Vector3(0.0f, 0f, 22.5f), new Vector3(1.2f, 0f, 35f), new Vector3(4.8f, 0f, 35.5f),
            };
            for (int i = 0; i < flowers.Length; i++)
            {
                var flower = b.Group(g, "Flower_" + (i + 1).ToString("00"));
                flower.localPosition = flowers[i];
                flower.localRotation = Quaternion.identity;
                flower.localScale = Vector3.one;
                b.Primitive(PrimitiveType.Cylinder, flower, "Stem", new Vector3(0f, 0.2f, 0f), Vector3.zero,
                    new Vector3(0.05f, 0.2f, 0.05f), p.Stem, false);
                b.Primitive(PrimitiveType.Sphere, flower, "Head", new Vector3(0f, 0.43f, 0f), Vector3.zero,
                    new Vector3(0.22f, 0.12f, 0.22f), i % 2 == 0 ? p.FlowerA : p.FlowerB, false);
            }

            // Copas frondosas.
            for (int i = 0; i < TreeBases.Length; i++)
            {
                b.Primitive(PrimitiveType.Sphere, g, "Canopy_" + TreeName(i),
                    TreeBases[i] + new Vector3(0f, TrunkHeight + 0.6f, 0f), Vector3.zero,
                    new Vector3(2.4f, 2.0f, 2.4f), p.Foliage, false);
            }

            // Portal con vegetación: arco despejado.
            b.Box(g, "ExitMoss_Lintel", new Vector3(3f, 2.95f, 38.5f), new Vector3(6.0f, 0.15f, 1.0f), p.Foliage, false);
            b.Primitive(PrimitiveType.Sphere, g, "ExitMoss_W", new Vector3(0.5f, 2.2f, 38.0f), Vector3.zero,
                new Vector3(0.9f, 0.9f, 0.5f), p.Foliage, false);
            b.Primitive(PrimitiveType.Sphere, g, "ExitMoss_E", new Vector3(5.5f, 2.2f, 38.0f), Vector3.zero,
                new Vector3(0.9f, 0.9f, 0.5f), p.Foliage, false);
        }

        // ------------------------------------------------------------------ Luz, cámara de vista previa, ayudas

        private static void BuildLighting(GreyboxBuilder b, Transform root)
        {
            var lighting = b.Group(root, "Lighting");
            var sun = b.Group(lighting, "Sun");
            sun.localPosition = new Vector3(0f, 10f, 0f);
            sun.localRotation = Quaternion.Euler(50f, -35f, 0f);
            sun.localScale = Vector3.one;
            var light = GreyboxBuilder.EnsureComponent<Light>(sun.gameObject);
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;

            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.48f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.26f, 0.20f);
        }

        private static void BuildPreviewCamera(GreyboxBuilder b, Transform root)
        {
            // Solo vista previa. Desactivada: la cámara jugable es de Codex (T06).
            var camT = b.Group(root, "PreviewCamera");
            camT.localPosition = new Vector3(16f, 16f, -8f);
            camT.LookAt(root.TransformPoint(new Vector3(1f, 0f, 18f)));
            camT.localScale = Vector3.one;
            var cam = GreyboxBuilder.EnsureComponent<Camera>(camT.gameObject);
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            camT.gameObject.SetActive(false);
        }

        private static void BuildEditorHelpers(GreyboxBuilder b, Transform root, Palette p)
        {
            // Cápsula de 1.8 m para comprobar escala. EditorOnly: no entra en builds. Sin collider.
            var helpers = b.Group(root, "EditorHelpers");
            helpers.gameObject.tag = "EditorOnly";
            var capsule = b.Primitive(PrimitiveType.Capsule, helpers, "ScaleReference_1_8m",
                new Vector3(4.5f, 0.9f, 1f), Vector3.zero, new Vector3(0.5f, 0.9f, 0.5f), p.Helper, false);
            capsule.gameObject.tag = "EditorOnly";
        }
    }
}
