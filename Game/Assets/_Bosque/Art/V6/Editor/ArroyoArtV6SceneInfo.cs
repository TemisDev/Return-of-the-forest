using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.ArtV6.Editor
{
    /// <summary>Volcado de solo lectura de una escena (jerarquía, transformaciones, componentes).</summary>
    public static class ArroyoArtV6SceneInfo
    {
        public static void DumpBatch()
        {
            var args = System.Environment.GetCommandLineArgs();
            int ai = System.Array.IndexOf(args, "-dumpScene");
            string scene = ai >= 0 ? args[ai + 1] : "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity";
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.dev-logs/v6/" + Path.GetFileNameWithoutExtension(scene) + "-dump.txt"));
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            var sb = new StringBuilder();
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects()) Dump(root.transform, 0, sb);
            File.WriteAllText(output, sb.ToString());
            EditorApplication.Exit(0);
        }

        private static void Dump(Transform t, int depth, StringBuilder sb)
        {
            var comps = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c =>
            {
                string n = c.GetType().Name;
                if (c is BoxCollider b) n += $"[c{b.center.ToString("F2")} s{b.size.ToString("F2")}]";
                if (c is MeshFilter mf && mf.sharedMesh) n += $"[{mf.sharedMesh.name}]";
                if (c is Renderer r) n += $"[{string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "null"))}{(r.enabled ? "" : " OFF")}]";
                if (c is Light l) n += $"[{l.type} I{l.intensity} rot{t.eulerAngles.ToString("F0")}]";
                return n;
            });
            sb.AppendLine($"{new string(' ', depth * 2)}{t.name}{(t.gameObject.activeSelf ? "" : " (inactive)")} " +
                          $"p{t.position.ToString("F2")} r{t.eulerAngles.ToString("F0")} s{t.lossyScale.ToString("F2")} | {string.Join(" ", comps)}");
            foreach (Transform c in t) Dump(c, depth + 1, sb);
        }
    }
}
