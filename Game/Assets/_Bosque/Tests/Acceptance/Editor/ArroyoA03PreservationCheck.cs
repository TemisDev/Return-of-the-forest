using System;
using System.Collections.Generic;
using System.Text;
using Bosque.LevelTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.Acceptance
{
    /// <summary>
    /// A-03: la regeneración conserva contenido ajeno. Trabaja sobre una COPIA temporal
    /// de ArroyoGreybox.unity dentro de Tests/Acceptance y la elimina al terminar.
    /// No toca la escena de nivel original ni la de integración.
    ///
    /// Batch: -executeMethod Bosque.Acceptance.ArroyoA03PreservationCheck.RunBatch
    /// </summary>
    public static class ArroyoA03PreservationCheck
    {
        private const string Parent = "Assets/_Bosque/Tests/Acceptance";
        private const string TmpName = "Tmp_A03";
        private const string Folder = Parent + "/" + TmpName;
        private const string CopyPath = Folder + "/ArroyoA03.unity";

        public static void RunBatch()
        {
            int code = 0;
            var sb = new StringBuilder();
            int fails = 0;
            void Check(bool ok, string what)
            {
                if (!ok) fails++;
                sb.AppendLine("[A03] " + (ok ? "OK" : "FALLA") + " | " + what);
            }

            try
            {
                if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
                AssetDatabase.CreateFolder(Parent, TmpName);
                if (!AssetDatabase.CopyAsset(ArroyoGreyboxGenerator.ScenePath, CopyPath))
                    throw new InvalidOperationException("No se pudo copiar la escena de nivel.");

                // 1. Añadir contenido ajeno a la copia.
                var scene = EditorSceneManager.OpenScene(CopyPath, OpenSceneMode.Single);
                var root = GameObject.Find(ArroyoGreyboxGenerator.RootName).transform;
                var geometry = root.Find("Geometry");
                var o1 = GreyboxBuilder.FindDeep(root, "Obstacle_01");
                var sp2 = GreyboxBuilder.FindDeep(root, "SafePoint_02");

                new GameObject("PruebaAjena").transform.SetParent(geometry, false);
                o1.gameObject.AddComponent<AudioSource>();
                new GameObject("HijoAjeno").transform.SetParent(o1, false);
                new GameObject("RaizAjena");
                new GameObject("ArteAjeno").transform.SetParent(root.Find(ArroyoGreyboxGenerator.DegradedVisuals), false);
                sp2.SetParent(geometry, true);
                sp2.position = new Vector3(1f, 0f, 21f);
                var o1Original = o1.localPosition;
                o1.localPosition = o1Original + new Vector3(0f, 0f, 0.5f);
                int countBefore = root.GetComponentsInChildren<Transform>(true).Length;
                EditorSceneManager.SaveScene(scene);

                // 2. Regenerar sobre la copia.
                ArroyoGreyboxGenerator.Generate(CopyPath);

                // 3. Reabrir desde disco y comprobar.
                EditorSceneManager.OpenScene(CopyPath, OpenSceneMode.Single);
                root = GameObject.Find(ArroyoGreyboxGenerator.RootName).transform;
                geometry = root.Find("Geometry");
                o1 = GreyboxBuilder.FindDeep(root, "Obstacle_01");

                Check(geometry.Find("PruebaAjena") != null, "Objeto ajeno bajo Geometry se conserva");
                Check(o1.GetComponent<AudioSource>() != null, "Componente ajeno (AudioSource) en Obstacle_01 se conserva");
                Check(o1.Find("HijoAjeno") != null, "Hijo ajeno bajo Obstacle_01 se conserva");
                Check(GameObject.Find("RaizAjena") != null, "Raíz ajena en la escena se conserva");
                Check(GreyboxBuilder.FindDeep(root, "ArteAjeno") != null, "Objeto ajeno dentro de DegradedVisuals se conserva");

                var sp = new List<Transform>();
                GreyboxBuilder.CollectByName(root, "SafePoint_02", sp);
                Check(sp.Count == 1, "SafePoint_02 reubicado no se duplica (encontrados " + sp.Count + ")");
                if (sp.Count == 1)
                {
                    Check(sp[0].parent == geometry, "SafePoint_02 mantiene su nuevo padre (Geometry)");
                    Check(Vector3.Distance(sp[0].position, new Vector3(1f, 0f, 21f)) < 0.01f,
                        "SafePoint_02 reubicado conserva su posición manual " + sp[0].position.ToString("F2"));
                }

                bool overwritten = Vector3.Distance(o1.localPosition, o1Original) < 0.001f;
                sb.AppendLine("[A03] INFO | Ajuste manual de posición en Obstacle_01 (+0.5 Z): " +
                              (overwritten ? "sobrescrito por el generador (comportamiento documentado)" : "conservado"));

                int countAfter = root.GetComponentsInChildren<Transform>(true).Length;
                Check(countAfter == countBefore, "Sin objetos nuevos ni perdidos bajo la raíz (" + countBefore + " → " + countAfter + ")");

                var report = ArroyoGreyboxValidator.ValidateOpenScene();
                Check(report.Ok, "Validador del contrato tras regenerar: " + (report.Ok ? "CORRECTA" : string.Join("; ", report.Errors)));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
                sb.AppendLine("[A03] INFO | Copia temporal eliminada: " + !AssetDatabase.IsValidFolder(Folder));
            }

            sb.Insert(0, "[A03] Resumen: " + (fails == 0 && code == 0 ? "CORRECTA" : fails + " fallo(s)") + "\n");
            Debug.Log(sb.ToString());
            EditorApplication.Exit(code != 0 ? 1 : (fails == 0 ? 0 : 2));
        }
    }
}
