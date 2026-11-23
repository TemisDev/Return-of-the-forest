using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bosque.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bosque.LevelTools
{
    /// <summary>
    /// V16: lo que se ve sólido debe serlo. Busca en una escena los objetos sólidos (rocas, peñascos, acantilados,
    /// troncos, tocones, raíces, árboles, postes y tablas) sin colisionador que los cubra y les añade un MeshCollider
    /// estático con su propia malla (la colisión coincide con lo que se ve: se puede caminar encima de una roca o del
    /// tronco-puente sin hundirse). No toca lo que el juego gestiona (residuos, maderas, objetos que se cargan, obstáculos
    /// que se retiran) ni la vegetación blanda. Árboles: solo la corteza (el follaje no bloquea).
    /// Lo llaman los constructores de cada zona antes de guardar la escena; también hay un informe en lote.
    /// </summary>
    public static class SolidPropColliders
    {
        private static readonly string[] Solid = { "rock", "boulder", "cliff", "stump", "deadlog", "log_", "_log", "root", "tree", "trunk", "bark", "poste", "tablas", "puente", "stone", "piedra", "roca", "tronco" };
        private static readonly string[] Soft = { "grass", "fern", "flower", "dandelion", "shrub", "leaf", "leaves", "foliage", "branch", "ichu", "pasto", "detail", "agua", "water", "fx", "particle", "baliza", "ripple" };

        public struct Result { public int added, alreadySolid, skipped; public List<string> addedNames; }

        /// <summary>Montajes artísticos antiguos del arroyo que el contrato exige solo visuales (la colisión es del greybox).</summary>
        private static readonly string[] VisualOnlyRoots = { "ArtV4Integration", "VisualIntegrationV3", "ArtV3" };

        private static bool InVisualOnlyMount(Transform t)
        {
            for (var p = t; p != null; p = p.parent) if (VisualOnlyRoots.Contains(p.name)) return true;
            return false;
        }

        /// <summary>Quita colisionadores de los montajes solo visuales (por si una pasada anterior los añadió).</summary>
        public static int CleanVisualOnlyMounts()
        {
            int n = 0;
            foreach (var root in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => VisualOnlyRoots.Contains(x.name)))
                foreach (var c in root.GetComponentsInChildren<Collider>(true)) { Object.DestroyImmediate(c); n++; }
            return n;
        }

        private static bool IsManaged(Transform t)
        {
            if (InVisualOnlyMount(t)) return true;
            // Objetos del juego que gestionan su propia colisión o deben poder atravesarse/recogerse.
            for (var p = t; p != null; p = p.parent)
                foreach (var mb in p.GetComponents<MonoBehaviour>())
                {
                    if (mb == null) continue;
                    var n = mb.GetType().Name;
                    if (mb is InteractionTarget || n.Contains("Interaction") || n.Contains("Litter") || n.Contains("FallenWood") || n.Contains("RefugeItem") || n.Contains("Removable") ||
                        n.Contains("Obstacle") || n.Contains("GrowthPoint") || n.Contains("Fauna") || n.Contains("Character") || n.Contains("Motor")) return true;
                }
            return false;
        }

        private static string Key(Renderer r)
        {
            var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(r.gameObject);
            var mf = r.GetComponent<MeshFilter>();
            string mats = string.Join(" ", r.sharedMaterials.Where(m => m != null).Select(m => m.name));
            return ((src != null ? src.name : "") + " " + r.name + " " + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "") + " " + mats).ToLowerInvariant();
        }

        private static bool IsSolid(Renderer r, out bool foliageMaterial)
        {
            foliageMaterial = false;
            if (!(r is MeshRenderer)) return false;
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return false;
            var k = Key(r);
            var size = r.bounds.size;
            if (Mathf.Max(size.x, size.z) < .45f && size.y < .6f) return false;        // guijarros y adornos pequeños
            if (!Solid.Any(k.Contains)) return false;
            // En árboles y raíces con follaje, solo la corteza: si el material es de hojas, no.
            string mats = string.Join(" ", r.sharedMaterials.Where(m => m != null).Select(m => m.name)).ToLowerInvariant();
            foliageMaterial = mats.Contains("leaf") || mats.Contains("leaves") || mats.Contains("foliage") || mats.Contains("branches") && !mats.Contains("bark");
            if (Soft.Any(s => k.Contains(s)) && !k.Contains("bark") && !k.Contains("deadlog") && !k.Contains("rock")) return false;
            return true;
        }

        /// <summary>
        /// ¿Ya hay un colisionador que lo cubra? Primero en su propia jerarquía; después, por posición: un colisionador
        /// activo de tamaño parecido (no el suelo ni el terreno) que ocupe su centro, como los muros del greybox que ya
        /// bloquean detrás de un acantilado visual.
        /// </summary>
        private static bool Covered(Renderer r)
        {
            var v = r.bounds;
            float Vol(Bounds b) => Mathf.Max(b.size.x, .05f) * Mathf.Max(b.size.y, .05f) * Mathf.Max(b.size.z, .05f);
            // Un colisionador propio (en el objeto o sus hijos) ya cuenta, aunque esté apagado (bounds vacíos si inactivo).
            if (r.GetComponentsInChildren<Collider>(true).Any(c => !c.isTrigger && !(c is CharacterController))) return true;
            foreach (var c in r.GetComponentsInParent<Collider>(true))
                if (!c.isTrigger && !(c is TerrainCollider) && !(c is CharacterController) && c.bounds.Intersects(v))
                {
                    var b = c.bounds;
                    float cov = Mathf.Min(b.size.x / Mathf.Max(v.size.x, .01f), 1) * Mathf.Min(b.size.y / Mathf.Max(v.size.y, .01f), 1) * Mathf.Min(b.size.z / Mathf.Max(v.size.z, .01f), 1);
                    if (cov > .35f) return true;
                }
            Physics.SyncTransforms();
            foreach (var c in Physics.OverlapBox(v.center, v.extents * .4f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c is TerrainCollider || c is CharacterController || c.transform.IsChildOf(r.transform)) continue;
                if (Vol(c.bounds) <= Vol(v) * 12f) return true;     // un colisionador hecho para esto, no el suelo
            }
            return false;
        }

        private static bool IsTree(string key) => key.Contains("tree");
        private static bool IsLowLod(Renderer r) => System.Text.RegularExpressions.Regex.IsMatch(r.name, @"_LOD[1-9]");

        /// <summary>
        /// Cápsula sobre el tronco real de un árbol: centro y radio medidos en los vértices de corteza del primer metro y
        /// medio (o de toda la malla si no hay material de corteza). El follaje no bloquea al jugador ni a la cámara.
        /// Devuelve la cápsula, en un hijo "ColisionTronco" del objeto.
        /// </summary>
        public static CapsuleCollider TrunkCapsule(GameObject go)
        {
            var mfs = go.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null && !IsLowLod(m.GetComponent<Renderer>())).ToList();
            var pts = new List<Vector3>();
            foreach (var mf in mfs)
            {
                var mr = mf.GetComponent<MeshRenderer>(); var mesh = mf.sharedMesh; var verts = mesh.vertices;
                var mats = mr != null ? mr.sharedMaterials : new Material[0];
                var idx = new List<int>();
                for (int sm = 0; sm < mesh.subMeshCount; sm++)
                {
                    var mn = sm < mats.Length && mats[sm] != null ? mats[sm].name.ToLowerInvariant() : "";
                    if (mn.Contains("bark") || mn.Contains("trunk") || mn.Contains("corteza")) idx.AddRange(mesh.GetTriangles(sm));
                }
                IEnumerable<int> use = idx.Count > 0 ? idx.Distinct() : Enumerable.Range(0, verts.Length);
                foreach (var i in use) pts.Add(mf.transform.TransformPoint(verts[i]));
            }
            if (pts.Count == 0) return null;
            float minY = pts.Min(q => q.y);
            var low = pts.Where(q => q.y < minY + 1.5f && q.y > minY + .15f).ToList();
            if (low.Count < 8) low = pts.Where(q => q.y < minY + 1.5f).ToList();
            var c = low.Aggregate(Vector3.zero, (a, b) => a + b) / low.Count;
            float radius = Mathf.Clamp(low.Average(q => new Vector2(q.x - c.x, q.z - c.z).magnitude) * 1.1f, .12f, 1.2f);
            var found = go.transform.Find("ColisionTronco");
            var holder = found != null ? found.gameObject : new GameObject("ColisionTronco");
            holder.transform.SetParent(go.transform, false);
            holder.transform.position = new Vector3(c.x, minY, c.z); holder.transform.rotation = Quaternion.identity;
            var ls = go.transform.lossyScale;
            holder.transform.localScale = new Vector3(1f / Mathf.Max(ls.x, 1e-4f), 1f / Mathf.Max(ls.y, 1e-4f), 1f / Mathf.Max(ls.z, 1e-4f));
            var cap = holder.GetComponent<CapsuleCollider>();
            if (cap == null) cap = holder.AddComponent<CapsuleCollider>();   // GetComponent devuelve un nulo de Unity: no sirve "??"
            cap.direction = 1; cap.radius = radius; cap.height = 3.2f; cap.center = new Vector3(0, 1.6f, 0);
            return cap;
        }

        /// <summary>Añade colisionadores a lo sólido de la escena activa. Devuelve el recuento (no guarda la escena).</summary>
        public static Result Apply(bool dryRun = false)
        {
            var res = new Result { addedNames = new List<string>() };
            var readable = new HashSet<string>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!IsSolid(r, out bool foliage)) continue;
                if (foliage) { res.skipped++; continue; }
                if (IsManaged(r.transform)) { res.skipped++; continue; }
                bool tree = IsTree(Key(r));
                if (tree && IsLowLod(r)) continue;                              // un solo colisionador por árbol
                if (tree ? r.transform.Find("ColisionTronco") != null || r.transform.parent != null && r.transform.parent.Find("ColisionTronco") != null : Covered(r))
                { res.alreadySolid++; continue; }
                res.added++; res.addedNames.Add(r.name);
                if (dryRun) continue;
                if (tree)
                {
                    var lod = r.GetComponentInParent<LODGroup>();
                    TrunkCapsule(lod != null ? lod.gameObject : r.gameObject);
                }
                else
                {
                    var mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh; mc.convex = false;
                    var mp = AssetDatabase.GetAssetPath(mc.sharedMesh);
                    if (!string.IsNullOrEmpty(mp)) readable.Add(mp);
                }
                EditorUtility.SetDirty(r.gameObject);
            }
            // Sin "Prebake Collision Meshes", una MeshCollider con escala no uniforme necesita la malla legible en la build.
            foreach (var path in readable)
                if (AssetImporter.GetAtPath(path) is ModelImporter mi && !mi.isReadable) { mi.isReadable = true; mi.SaveAndReimport(); }
            return res;
        }

        /// <summary>
        /// Árboles del Terrain: el colisionador del prefab debe caer sobre el tronco. Si el tronco del modelo no está
        /// en el origen del prefab (pivote desplazado), la cápsula queda al lado y el tronco se atraviesa.
        /// Devuelve, por prototipo, la distancia horizontal entre la cápsula y el tronco (base de la corteza).
        /// </summary>
        public static List<(string prefab, float offset, float radius)> TerrainTreeReport()
        {
            var list = new List<(string, float, float)>();
            foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
                foreach (var p in t.terrainData.treePrototypes)
                {
                    if (p.prefab == null) continue;
                    var col = p.prefab.GetComponentInChildren<Collider>();
                    var bark = p.prefab.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh != null).Select(m => (m, r: m.GetComponent<MeshRenderer>()))
                        .Where(x => x.r != null && x.r.sharedMaterials.Any(mt => mt != null && mt.name.ToLowerInvariant().Contains("bark"))).ToList();
                    if (bark.Count == 0) { list.Add((p.prefab.name, -1, 0)); continue; }
                    // Centro del tronco: vértices de corteza por debajo de 1 m.
                    var pts = new List<Vector3>();
                    foreach (var (m, _) in bark)
                        foreach (var v in m.sharedMesh.vertices) { var w = p.prefab.transform.InverseTransformPoint(m.transform.TransformPoint(v)); if (w.y < 1f) pts.Add(w); }
                    if (pts.Count == 0) { list.Add((p.prefab.name, -2, 0)); continue; }
                    var c = pts.Aggregate(Vector3.zero, (a, b) => a + b) / pts.Count;
                    float radius = pts.Average(q => new Vector2(q.x - c.x, q.z - c.z).magnitude);
                    Vector3 cc = Vector3.zero; float cr = 0;
                    if (col is CapsuleCollider cap) { cc = cap.center; cr = cap.radius; }
                    list.Add((p.prefab.name + (col == null ? " (sin colisionador)" : $" (cápsula r {cr:F2})"), new Vector2(c.x - cc.x, c.z - cc.z).magnitude, radius));
                }
            return list;
        }

        public static readonly string[] Scenes =
        {
            "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity", "Assets/_Bosque/Scenes/Zonas/ClaroVacio.unity",
            "Assets/_Bosque/Scenes/Zonas/RefugioPerdido.unity", "Assets/_Bosque/Scenes/Zonas/ValleOlvidado.unity",
        };

        /// <summary>Informe en lote (no cambia nada): qué le falta colisión en cada zona.</summary>
        public static void AuditBatch()
        {
            var sb = new StringBuilder();
            foreach (var s in Scenes)
            {
                EditorSceneManager.OpenScene(s, OpenSceneMode.Single);
                var r = Apply(true);
                sb.AppendLine($"[Auditoria] {Path.GetFileNameWithoutExtension(s)}: sin colisión {r.added}, ya sólidos {r.alreadySolid}, omitidos {r.skipped}");
                foreach (var g in r.addedNames.GroupBy(n => System.Text.RegularExpressions.Regex.Replace(n, @"[\s_]*\(\d+\)$|_\d+$", "")).OrderByDescending(g => g.Count()))
                    sb.AppendLine($"    {g.Count(),4} × {g.Key}");
                foreach (var (prefab, off, rad) in TerrainTreeReport())
                    sb.AppendLine($"    árbol de terreno {prefab}: tronco a {off:F2} m de la cápsula, radio del tronco {rad:F2} m");
            }
            Debug.Log(sb.ToString());
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Charcos del arroyo (V16): eran cilindros aplastados (canto visible y UV de tapa); pasan a planos con la misma
        /// huella, que el sombreador difumina con borde de tierra mojada.
        /// </summary>
        public static int FixPuddles()
        {
            var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            int n = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mf.name != "V6_Puddle" || mf.sharedMesh == quad) continue;
                var t = mf.transform; var ls = t.localScale; float yaw = t.eulerAngles.y;
                mf.sharedMesh = quad;
                t.rotation = Quaternion.Euler(90, yaw, 0);
                t.localScale = new Vector3(ls.x * 1.5f, ls.z * 1.5f, 1f);
                t.position += Vector3.up * .006f;
                EditorUtility.SetDirty(mf.gameObject); n++;
            }
            return n;
        }

        /// <summary>Aplica en las cuatro zonas y guarda (para escenas ya construidas, sin regenerarlas).</summary>
        public static void ApplyAllBatch()
        {
            var sb = new StringBuilder();
            foreach (var s in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(s, OpenSceneMode.Single);
                int puddles = FixPuddles();
                int cleaned = CleanVisualOnlyMounts();
                int dupes = 0;
                foreach (var go in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(m => m.gameObject).Distinct().ToList())
                    foreach (var extra in go.GetComponents<MeshCollider>().Skip(1)) { Object.DestroyImmediate(extra); dupes++; }
                if (dupes > 0) sb.AppendLine($"[Solidez] {Path.GetFileNameWithoutExtension(s)}: {dupes} colisionadores duplicados quitados");
                if (cleaned > 0) sb.AppendLine($"[Solidez] {Path.GetFileNameWithoutExtension(s)}: {cleaned} colisionadores quitados de montajes solo visuales");
                var r = Apply();
                if (puddles > 0) sb.AppendLine($"[Charcos] {Path.GetFileNameWithoutExtension(s)}: {puddles} charcos convertidos en planos");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                sb.AppendLine($"[Solidez] {Path.GetFileNameWithoutExtension(s)}: añadidos {r.added}, ya sólidos {r.alreadySolid}, omitidos {r.skipped}");
            }
            Debug.Log(sb.ToString());
            EditorApplication.Exit(0);
        }
    }
}
