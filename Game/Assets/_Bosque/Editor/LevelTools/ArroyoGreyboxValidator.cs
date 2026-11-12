using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bosque.LevelTools
{
    /// <summary>
    /// Comprobaciones estáticas del contrato de escena (Docs/CONTRATOS.md).
    /// No ejecuta gameplay: valida estructura, colliders, alturas y medidas.
    /// </summary>
    public static class ArroyoGreyboxValidator
    {
        public sealed class Report
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public readonly List<string> Info = new List<string>();
            public bool Ok => Errors.Count == 0;

            public void LogToConsole()
            {
                var sb = new StringBuilder();
                sb.AppendLine("[ArroyoGreybox] Validación: " + (Ok ? "CORRECTA" : "CON ERRORES") +
                              " (" + Errors.Count + " errores, " + Warnings.Count + " avisos)");
                foreach (var e in Errors) sb.AppendLine("  ERROR: " + e);
                foreach (var w in Warnings) sb.AppendLine("  AVISO: " + w);
                foreach (var i in Info) sb.AppendLine("  INFO: " + i);
                if (Ok) Debug.Log(sb.ToString());
                else Debug.LogError(sb.ToString());
            }
        }

        public static Report ValidateOpenScene()
        {
            var r = new Report();
            var scene = SceneManager.GetActiveScene();

            // Raíz única.
            var roots = new List<GameObject>();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == ArroyoGreyboxGenerator.RootName) roots.Add(go);
            }
            if (roots.Count != 1)
            {
                r.Errors.Add("Se esperaba exactamente una raíz '" + ArroyoGreyboxGenerator.RootName + "', hay " + roots.Count + ".");
                return r;
            }
            var root = roots[0].transform;

            // Marcadores únicos bajo la raíz.
            var required = new List<string>
            {
                ArroyoGreyboxGenerator.PlayerSpawn,
                ArroyoGreyboxGenerator.SafeRespawn,
                ArroyoGreyboxGenerator.ExitMarker,
                ArroyoGreyboxGenerator.DegradedVisuals,
                ArroyoGreyboxGenerator.RestoredVisuals,
            };
            required.AddRange(ArroyoGreyboxGenerator.ObstacleNames);

            var found = new Dictionary<string, Transform>();
            foreach (var name in required)
            {
                var list = new List<Transform>();
                GreyboxBuilder.CollectByName(root, name, list);
                if (list.Count != 1)
                {
                    r.Errors.Add("'" + name + "' debe existir una vez bajo la raíz; encontrados: " + list.Count + ".");
                }
                else
                {
                    found[name] = list[0];
                }
                // También fuera de la raíz, en toda la escena.
                int outside = 0;
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.transform == root) continue;
                    var extra = new List<Transform>();
                    GreyboxBuilder.CollectByName(go.transform, name, extra);
                    outside += extra.Count;
                }
                if (outside > 0) r.Errors.Add("'" + name + "' aparece " + outside + " vez/veces fuera de la raíz.");
            }

            // Estado inicial de los grupos visuales.
            if (found.TryGetValue(ArroyoGreyboxGenerator.DegradedVisuals, out var degraded) && !degraded.gameObject.activeSelf)
                r.Errors.Add("DegradedVisuals debe empezar activo.");
            if (found.TryGetValue(ArroyoGreyboxGenerator.RestoredVisuals, out var restored) && restored.gameObject.activeSelf)
                r.Errors.Add("RestoredVisuals debe empezar inactivo.");
            if (degraded != null && restored != null &&
                (degraded.IsChildOf(restored) || restored.IsChildOf(degraded)))
                r.Errors.Add("DegradedVisuals y RestoredVisuals no deben estar anidados.");

            // Visuales sin física (para que la física no cambie entre estados).
            CheckNoColliders(degraded, r);
            CheckNoColliders(restored, r);

            // Obstáculos con collider sólido.
            foreach (var name in ArroyoGreyboxGenerator.ObstacleNames)
            {
                if (!found.TryGetValue(name, out var t)) continue;
                var cols = t.GetComponents<Collider>();
                bool solid = false;
                foreach (var c in cols) if (c.enabled && !c.isTrigger) solid = true;
                if (!solid) r.Errors.Add(name + " no tiene un collider sólido activo en su raíz.");
                if (!t.gameObject.activeInHierarchy) r.Errors.Add(name + " está inactivo.");
            }

            // Altura de los puntos de aparición/retorno/salida sobre superficie con collider.
            Physics.SyncTransforms();
            var groundChecks = new List<string>
            {
                ArroyoGreyboxGenerator.PlayerSpawn, ArroyoGreyboxGenerator.SafeRespawn, ArroyoGreyboxGenerator.ExitMarker
            };
            foreach (var name in groundChecks)
            {
                if (found.TryGetValue(name, out var t)) CheckGround(t, r, true);
            }
            foreach (var name in ArroyoGreyboxGenerator.SafePointNames)
            {
                var t = GreyboxBuilder.FindDeep(root, name);
                if (t != null) CheckGround(t, r, false);
                else r.Warnings.Add("Punto seguro opcional ausente: " + name);
            }

            // Longitud del recorrido (contrato: 25–40 m orientativo).
            if (found.TryGetValue(ArroyoGreyboxGenerator.PlayerSpawn, out var spawn) &&
                found.TryGetValue(ArroyoGreyboxGenerator.ExitMarker, out var exit))
            {
                float d = Vector3.Distance(spawn.position, exit.position);
                r.Info.Add("Distancia PlayerSpawn → ExitMarker: " + d.ToString("0.0") + " m.");
                if (d < 25f || d > 40f) r.Warnings.Add("Recorrido fuera del rango orientativo 25–40 m.");
            }

            // Sin cámaras activas (la jugable la aporta integración).
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam.gameObject.activeInHierarchy && cam.enabled)
                    r.Errors.Add("Cámara activa en la escena de nivel: " + cam.name + ". Solo se admite vista previa desactivada.");
            }

            // Materiales URP.
            int nonUrp = 0;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = mr.sharedMaterial;
                if (m == null || m.shader == null || m.shader.name != GreyboxBuilder.UrpLitShader) nonUrp++;
            }
            if (nonUrp > 0) r.Errors.Add(nonUrp + " renderer(s) sin material URP Lit.");

            // Scripts perdidos.
            int missing = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var c in t.GetComponents<Component>()) if (c == null) missing++;
            }
            if (missing > 0) r.Errors.Add(missing + " componente(s) con script perdido.");

            r.Info.Add("Escena: " + scene.path);
            return r;
        }

        private static void CheckNoColliders(Transform group, Report r)
        {
            if (group == null) return;
            var cols = group.GetComponentsInChildren<Collider>(true);
            if (cols.Length > 0)
                r.Warnings.Add(group.name + " contiene " + cols.Length +
                               " collider(s); la física cambiaría entre estados.");
        }

        private static void CheckGround(Transform t, Report r, bool required)
        {
            var origin = t.position + Vector3.up * 1.0f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                float diff = Mathf.Abs(hit.point.y - t.position.y);
                if (diff > 0.05f)
                {
                    var msg = t.name + " está a " + diff.ToString("0.00") + " m de la superficie (" + hit.collider.name + ").";
                    if (required) r.Errors.Add(msg); else r.Warnings.Add(msg);
                }
                else
                {
                    r.Info.Add(t.name + " sobre " + hit.collider.name + " en " + t.position.ToString("F2") + ".");
                }

                // Espacio libre de 2 m sobre el punto (cápsula de 1.8 m).
                if (Physics.Raycast(t.position + Vector3.up * 0.05f, Vector3.up, out var ceil, 2f, ~0,
                        QueryTriggerInteraction.Ignore))
                {
                    var msg = t.name + " tiene un obstáculo encima a " + ceil.distance.ToString("0.00") + " m (" + ceil.collider.name + ").";
                    if (required) r.Errors.Add(msg); else r.Warnings.Add(msg);
                }
            }
            else
            {
                var msg = t.name + " no tiene suelo con collider debajo.";
                if (required) r.Errors.Add(msg); else r.Warnings.Add(msg);
            }
        }
    }
}
