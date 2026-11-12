using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bosque.LevelTools
{
    /// <summary>
    /// Utilidades de construcción idempotente para escenas greybox.
    /// Regla: buscar por nombre y actualizar; crear solo si falta; nunca destruir
    /// objetos ni componentes que ya existan (pueden pertenecer a otro equipo).
    /// </summary>
    internal sealed class GreyboxBuilder
    {
        public const string UrpLitShader = "Universal Render Pipeline/Lit";

        private readonly Dictionary<PrimitiveType, Mesh> primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();

        public int Created { get; private set; }
        public int Updated { get; private set; }

        /// <summary>Hijo directo por nombre; lo crea vacío si no existe.</summary>
        public Transform Group(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                t = go.transform;
                Created++;
            }
            else
            {
                Updated++;
            }
            return t;
        }

        /// <summary>
        /// Objeto con nombre único bajo <paramref name="root"/>. Si ya existe en cualquier
        /// nivel (por ejemplo, porque integración lo reubicó), se reutiliza sin cambiar su padre.
        /// </summary>
        public Transform UniqueEmpty(Transform root, Transform defaultParent, string name, Vector3 localPos, float yaw)
        {
            var t = FindDeep(root, name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(defaultParent, false);
                t = go.transform;
                Created++;
            }
            else
            {
                Updated++;
            }

            if (t.parent == defaultParent)
            {
                t.localPosition = localPos;
                t.localRotation = Quaternion.Euler(0f, yaw, 0f);
                t.localScale = Vector3.one;
            }
            return t;
        }

        public Transform Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider)
        {
            return Primitive(PrimitiveType.Cube, parent, name, center, Vector3.zero, size, mat, collider);
        }

        public Transform Box(Transform parent, string name, Vector3 center, Vector3 euler, Vector3 size, Material mat, bool collider)
        {
            return Primitive(PrimitiveType.Cube, parent, name, center, euler, size, mat, collider);
        }

        public Transform Primitive(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 euler,
            Vector3 scale, Material mat, bool collider)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                t = go.transform;
                Created++;
            }
            else
            {
                Updated++;
            }

            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(euler);
            t.localScale = scale;

            var mf = t.GetComponent<MeshFilter>();
            if (mf == null) mf = t.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = PrimitiveMesh(type);

            var mr = t.GetComponent<MeshRenderer>();
            if (mr == null) mr = t.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            if (collider && t.GetComponent<Collider>() == null)
            {
                AddPrimitiveCollider(t.gameObject, type);
            }
            return t;
        }

        public static BoxCollider EnsureBoxCollider(GameObject go, Vector3 center, Vector3 size)
        {
            var box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            box.isTrigger = false;
            return box;
        }

        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        public static void CollectByName(Transform root, string name, List<Transform> results)
        {
            if (root.name == name) results.Add(root);
            for (int i = 0; i < root.childCount; i++) CollectByName(root.GetChild(i), name, results);
        }

        public static void SetMarkerIcon(GameObject go, string builtinIcon)
        {
            var content = EditorGUIUtility.IconContent(builtinIcon);
            if (content != null && content.image is Texture2D tex)
            {
                EditorGUIUtility.SetIconForObject(go, tex);
            }
        }

        public static void EnsureFolder(string parent, string child)
        {
            string full = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, child);
        }

        /// <summary>Crea o actualiza un material URP Lit administrado por la herramienta.</summary>
        public static Material EnsureMaterial(string folder, string name, Color color, float smoothness)
        {
            var shader = Shader.Find(UrpLitShader);
            if (shader == null)
            {
                throw new InvalidOperationException("No se encontró el shader '" + UrpLitShader +
                                                    "'. El proyecto debe usar URP (lo mantiene Codex).");
            }

            string path = folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private Mesh PrimitiveMesh(PrimitiveType type)
        {
            if (primitiveMeshes.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var temp = GameObject.CreatePrimitive(type);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            primitiveMeshes[type] = mesh;
            return mesh;
        }

        private static void AddPrimitiveCollider(GameObject go, PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Sphere:
                    go.AddComponent<SphereCollider>();
                    break;
                case PrimitiveType.Capsule:
                case PrimitiveType.Cylinder:
                    var cap = go.AddComponent<CapsuleCollider>();
                    cap.direction = 1;
                    cap.height = 2f;
                    cap.radius = 0.5f;
                    break;
                default:
                    go.AddComponent<BoxCollider>();
                    break;
            }
        }
    }
}
