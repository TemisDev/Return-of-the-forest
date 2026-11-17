using System;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using Bosque.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.CoreSetup
{
    public static class ArroyoGameplaySetup
    {
        [MenuItem("Tools/Bosque/Prototipo/Integrar gameplay del arroyo v2")]
        public static void FromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Integrate();
        }
        public static void IntegrateBatch()
        {
            try { Integrate(); Debug.Log("[ArroyoV2] Escena integrada."); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void Integrate()
        {
            ArroyoPlayableSetup.CreateOrOpen();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var level = GameObject.Find("ArroyoGreybox").transform;
            var gameplay = GameObject.Find("Gameplay").transform;
            var player = GameObject.Find("Player").GetComponent<ThirdPersonMotor>();
            var interactor = Ensure<PlayerInteractor>(player.gameObject);
            var root = Child(gameplay, "ArroyoGameplayV2");
            var session = Ensure<ArroyoSession>(root.gameObject);
            var obstacles = new RemovableObstacle[3];
            string[] ids = { "arroyo.log", "arroyo.rocks", "arroyo.branches" };
            for (int i = 0; i < obstacles.Length; i++)
            {
                var marker = Find(level, "Obstacle_0" + (i + 1));
                obstacles[i] = Ensure<RemovableObstacle>(marker.gameObject);
                obstacles[i].Configure(ids[i], marker.GetComponent<Collider>());
            }
            var barrier = Child(root, "ExitBlocker");
            barrier.position = new Vector3(.75f, 1.8f, 35f);
            var block = Ensure<BoxCollider>(barrier.gameObject);
            block.size = new Vector3(11.5f, 5f, .5f); block.isTrigger = false;
            var gateVisual = Child(barrier, "VisualMount");
            if (gateVisual.childCount == 0)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Barrera provisional"; cube.transform.SetParent(gateVisual, false);
                cube.transform.localScale = new Vector3(11.5f, 3f, .2f);
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
                cube.GetComponent<Renderer>().sharedMaterial = ArroyoArtSetup.Material("wood");
            }
            var exit = Child(root, "ExitTrigger");
            exit.position = new Vector3(.75f, .8f, 36.3f);
            var trigger = Ensure<BoxCollider>(exit.gameObject);
            trigger.size = new Vector3(11.5f, 3f, 1.4f); trigger.isTrigger = true;
            var oldBody = exit.GetComponent<Rigidbody>();
            if (oldBody != null) UnityEngine.Object.DestroyImmediate(oldBody);
            Ensure<ArroyoExit>(exit.gameObject).Configure(session);
            session.Configure(obstacles, Find(level, "DegradedVisuals").gameObject,
                Find(level, "RestoredVisuals").gameObject, barrier.gameObject, player, Find(level, "PlayerSpawn"));
            Ensure<ArroyoPrototypeHud>(root.gameObject).Configure(session, interactor);
            ArroyoArtSetup.Attach(level, root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Physics.SyncTransforms();
        }
        internal static Transform Find(Transform root, string name)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name) return item;
            throw new InvalidOperationException("Falta referencia de integración: " + name);
        }
        internal static Transform Child(Transform parent, string name)
        {
            var found = parent.Find(name);
            if (found != null) return found;
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        private static T Ensure<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null) component = go.AddComponent<T>();
            return component;
        }
    }
}
