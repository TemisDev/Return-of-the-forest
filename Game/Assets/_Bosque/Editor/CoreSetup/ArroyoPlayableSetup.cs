using System;
using Bosque.LevelTools;
using Bosque.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bosque.CoreSetup
{
    public static class ArroyoPlayableSetup
    {
        public const string ScenePath = "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity";
        [MenuItem("Tools/Bosque/Prototipo/Crear o abrir arroyo jugable")]
        public static void OpenFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateOrOpen();
        }
        public static void CreateBatch()
        {
            try { CreateOrOpen(); Debug.Log("[T06] Integracion preparada: " + ScenePath); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void CreateOrOpen()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                return; // Never overwrite a scene that may already contain subsequent integration work.
            }
            var scene = EditorSceneManager.OpenScene(ArroyoGreyboxGenerator.ScenePath, OpenSceneMode.Single);
            var report = ArroyoGreyboxValidator.ValidateOpenScene();
            report.LogToConsole();
            if (!report.Ok) throw new InvalidOperationException("El nivel de origen no cumple el contrato.");
            var root = GameObject.Find("ArroyoGreybox").transform;
            var spawn = Find(root, "PlayerSpawn");
            var safe = Find(root, "SafeRespawn");
            var helpers = root.Find("EditorHelpers");
            if (helpers != null) helpers.gameObject.SetActive(false);
            var gameplay = new GameObject("Gameplay");
            var player = new GameObject("Player");
            player.transform.SetParent(gameplay.transform);
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.center = Vector3.up * .9f; cc.radius = .35f;
            cc.stepOffset = .3f; cc.skinWidth = .03f; cc.slopeLimit = 45f;
            var motor = player.AddComponent<ThirdPersonMotor>();
            motor.SetRespawn(safe);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "ProvisionalBody";
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.up * .9f;
            visual.transform.localScale = new Vector3(.65f, .9f, .65f);
            const string materialPath = "Assets/_Bosque/Scenes/Integration/Player.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(.82f, .47f, .16f));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            visual.GetComponent<Renderer>().sharedMaterial = material;
            var cameraObject = new GameObject("GameplayCamera");
            cameraObject.transform.SetParent(gameplay.transform);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = .05f; camera.farClipPlane = 150f; camera.fieldOfView = 65f;
            cameraObject.AddComponent<AudioListener>();
            var rig = cameraObject.AddComponent<ThirdPersonCamera>();
            Physics.SyncTransforms();
            rig.Configure(player.transform);
            player.AddComponent<ThirdPersonInput>().Configure(rig);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
        private static Transform Find(Transform root, string name)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name) return item;
            throw new InvalidOperationException("Falta marcador: " + name);
        }
    }
}
