#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates independent V4 materials/prefabs. Never opens, writes or re-generates a scene.
/// Creates missing assets only; preserves all manually modified generated assets.
/// </summary>
public static class ArroyoArtV4Builder
{
    private const string Root = "Assets/_Bosque/Art/V4";
    private const string Models = Root + "/Models";
    private const string Textures = Root + "/Textures";
    private const string Materials = Root + "/Generated/Materials";
    private const string Prefabs = Root + "/Generated/Prefabs";

    private struct Entry
    {
        public string model, name, material, color, texture;
        public Entry(string model, string name, string material, string color, string texture)
        { this.model=model; this.name=name; this.material=material; this.color=color; this.texture=texture; }
    }

    private static readonly Entry[] Entries = {
        new Entry("RiverStoneCluster_V4", "VIS_V4_RiverStones", "MAT_V4_Rock", "#8B9089", "rock_matte_detail_v4.png"),
        new Entry("ReedFan_V4", "VIS_V4_ReedFan", "MAT_V4_Reeds", "#477B58", "moss_matte_detail_v4.png"),
        new Entry("MossyBranch_V4", "VIS_V4_MossyBranch", "MAT_V4_Branch", "#826346", "wood_matte_detail_v4.png"),
        new Entry("FoamRibbon_V4", "VIS_V4_FoamRibbon", "MAT_V4_Foam", "#D1E5DE", "foam_alpha_v4.png")
    };

    [MenuItem("Tools/Bosque/Arte/V4/Generar materiales y prefabs")]
    public static void Generate()
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) {
            Debug.LogError("[Arte V4] URP/Lit no encontrado. Revisar paquetes/render pipeline.");
            return;
        }
        EnsureFolder(Materials);
        EnsureFolder(Prefabs);
        int newMaterials=0, newPrefabs=0, missingModels=0;
        foreach (var e in Entries)
        {
            var matPath=Materials+"/"+e.material+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null)
            {
                mat=new Material(shader);
                if (ColorUtility.TryParseHtmlString(e.color, out Color color))
                    mat.SetColor("_BaseColor",color);
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+"/"+e.texture);
                if(tex!=null) mat.SetTexture("_BaseMap",tex);
                mat.SetFloat("_Smoothness",0.08f);
                mat.enableInstancing=true;
                AssetDatabase.CreateAsset(mat,matPath);
                newMaterials++;
            }
            var prefabPath=Prefabs+"/"+e.name+".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null) continue;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Models+"/"+e.model+".obj");
            if(source==null) { missingModels++; Debug.LogWarning("[Arte V4] Falta modelo: "+e.model); continue; }
            GameObject instance=null;
            try
            {
                instance=(GameObject)PrefabUtility.InstantiatePrefab(source);
                instance.name=e.name;
                foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial=mat;
                // The art kit is visual-only. No duplicate gameplay/collision components.
                foreach(var collider in instance.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(collider);
                PrefabUtility.SaveAsPrefabAsset(instance,prefabPath);
                newPrefabs++;
            }
            finally { if(instance!=null) Object.DestroyImmediate(instance); }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Arte V4] Nuevos materiales: {newMaterials}, prefabs: {newPrefabs}, modelos ausentes: {missingModels}. Validar manualmente en Unity.");
    }

    private static void EnsureFolder(string path)
    {
        string[] chunks=path.Split('/'); string current=chunks[0];
        for(int i=1;i<chunks.Length;i++)
        {
            string next=current+"/"+chunks[i];
            if(!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current,chunks[i]);
            current=next;
        }
    }
}
#endif
