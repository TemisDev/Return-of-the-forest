using Bosque.Session;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>
    /// Color del agua del valle: turbia mientras el sector está sucio, más clara al limpiarlo y cristalina cuando
    /// el valle revive. Cada trozo de la malla lleva su sector y la dirección de su corriente.
    /// </summary>
    public sealed class ValleWaterLook : MonoBehaviour
    {
        [SerializeField] private ValleSession session;
        [SerializeField] private Renderer[] renderers = new Renderer[0];
        [SerializeField] private int[] sectors = new int[0];
        [SerializeField] private Vector2[] flows = new Vector2[0];
        [SerializeField] private bool[] still = new bool[0];
        [SerializeField] private float fadeSeconds = 3f;
        private float[] level;
        private MaterialPropertyBlock block;

        private static readonly int ShallowId = Shader.PropertyToID("_ShallowColor"), DeepId = Shader.PropertyToID("_DeepColor"),
            MurkId = Shader.PropertyToID("_Murkiness"), FoamId = Shader.PropertyToID("_FoamColor"), FlowDirId = Shader.PropertyToID("_FlowDirection"),
            FlowSpeedId = Shader.PropertyToID("_FlowSpeed"), FlowFoamId = Shader.PropertyToID("_FlowFoam"), ReflId = Shader.PropertyToID("_ReflectionStrength"),
            CausticId = Shader.PropertyToID("_CausticStrength"), TilingId = Shader.PropertyToID("_NormalTiling"), NormalId = Shader.PropertyToID("_NormalStrength"),
            FlowMapId = Shader.PropertyToID("_FlowMap"), EdgeId = Shader.PropertyToID("_EdgeFade"), FoamDistId = Shader.PropertyToID("_FoamDistance");

        public int Count => renderers.Length;
        /// <summary>Claridad media (0 turbia, 1 cristalina).</summary>
        public float Clarity { get { if (level == null || level.Length == 0) return 0; float s = 0; foreach (var l in level) s += l; return s / level.Length; } }

        public void Configure(ValleSession owner, Renderer[] r, int[] sector, Vector2[] flow, bool[] lake)
        { session = owner; renderers = r; sectors = sector; flows = flow; still = lake; level = null; }

        private void OnEnable() { level = null; Apply(true); }

        private float Target(int i)
        {
            if (session == null) return 0f;
            if (session.IsRestored) return 1f;
            int s = sectors[i];
            return s >= 0 && s < session.Sectors.Length && session.Sectors[s].IsClean ? .7f : 0f;
        }

        private void Update() => Apply(false);

        private void Apply(bool force)
        {
            if (renderers == null) return;
            if (level == null || level.Length != renderers.Length) { level = new float[renderers.Length]; for (int i = 0; i < level.Length; i++) level[i] = Target(i); force = true; }
            block ??= new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                float t = Target(i);
                if (!force && Mathf.Approximately(level[i], t)) continue;
                level[i] = force ? level[i] : Mathf.MoveTowards(level[i], t, Time.deltaTime / fadeSeconds);
                float k = level[i]; bool lake = still[i];
                var r = renderers[i]; if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(ShallowId, Color.Lerp(new Color(.66f, .58f, .42f), new Color(.6f, .82f, .76f), k));
                block.SetColor(DeepId, Color.Lerp(new Color(.34f, .29f, .19f), new Color(.04f, .2f, .22f), k));
                block.SetFloat(MurkId, Mathf.Lerp(.62f, .06f, k));
                block.SetColor(FoamId, Color.Lerp(new Color(.62f, .58f, .5f, .3f), new Color(.92f, .95f, .93f, .6f), k));
                block.SetVector(FlowDirId, new Vector4(flows[i].x, flows[i].y, 0, 0));
                block.SetFloat(FlowSpeedId, lake ? .04f : Mathf.Lerp(.28f, .55f, k));
                // Ríos: la corriente sigue el cauce vértice a vértice (sin costuras entre trozos); lagunas: deriva suave.
                block.SetFloat(FlowMapId, lake ? 0f : 1f);
                // Orilla: el agua se desvanece en los últimos centímetros y hace espuma al tocar piedras y bordes.
                block.SetFloat(EdgeId, .28f); block.SetFloat(FoamDistId, lake ? .35f : .5f);
                // Ondas más grandes y suaves que en el arroyo: a distancia, una repetición corta se ve como cuadrícula.
                block.SetFloat(TilingId, lake ? 9f : 7f);
                block.SetFloat(NormalId, Mathf.Lerp(.14f, .22f, k));   // olas finas: más fuerza se ve como agua picada
                block.SetFloat(FlowFoamId, lake ? 0f : Mathf.Lerp(.06f, .1f, k));
                block.SetFloat(ReflId, Mathf.Lerp(.42f, .55f, k));
                block.SetFloat(CausticId, Mathf.Lerp(0f, .9f, k));
                r.SetPropertyBlock(block);
            }
        }
    }
}
