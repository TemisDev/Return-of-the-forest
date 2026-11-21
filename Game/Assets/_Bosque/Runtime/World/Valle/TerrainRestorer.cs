using System.Collections.Generic;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>
    /// Recuperación visual del terreno del valle, por sectores (cuadrantes) y por manchas alrededor de cada residuo:
    /// la capa de suelo seco (0) pasa a pasto (1); los árboles secos (prototipo par) pasan a su versión verde (impar);
    /// el pasto seco (detalle 0) pasa a pasto verde (detalle 1). Trabaja sobre una copia de TerrainData para no
    /// modificar el asset, y puede volver al estado degradado al reiniciar.
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    public sealed class TerrainRestorer : MonoBehaviour
    {
        [SerializeField] private float sectorBlendMetres = 10f;
        [SerializeField] private float sectorFadeSeconds = 3f;
        [SerializeField] private float spotRadius = 5f;

        private Terrain terrain;
        private TerrainData data;
        private int aw, ah, layers, dw, dh;
        private float[,,] degraded;
        private float[,] spot;
        private readonly float[] sectorLevel = new float[4];
        private readonly float[] sectorTarget = new float[4];
        private float[][,] sectorMask;
        private TreeInstance[] treesDegraded;
        private int[,] dryDetail, greenDetailBase;
        private bool[] detailRestored = new bool[4];
        private float nextApply;
        private bool dirty;

        public bool Ready => data != null;
        public float SectorLevel(int s) => sectorLevel[s];
        public int GreenTrees { get; private set; }

        /// <summary>Sector (0 NE, 1 NO, 2 SO, 3 SE) de una posición del mundo, respecto al centro del terreno.</summary>
        public static int SectorOf(Vector3 world, Vector3 center) =>
            world.z >= center.z ? (world.x >= center.x ? 0 : 1) : (world.x < center.x ? 2 : 3);

        private Vector3 Center => terrain.transform.position + new Vector3(data.size.x * .5f, 0, data.size.z * .5f);

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (data != null) return;
            terrain = GetComponent<Terrain>();
            data = Instantiate(terrain.terrainData); data.name = terrain.terrainData.name + " (juego)";
            terrain.terrainData = data;
            var col = GetComponent<TerrainCollider>(); if (col != null) col.terrainData = data;
            aw = data.alphamapWidth; ah = data.alphamapHeight; layers = data.alphamapLayers;
            degraded = data.GetAlphamaps(0, 0, aw, ah);
            spot = new float[ah, aw];
            sectorMask = new float[4][,];
            for (int s = 0; s < 4; s++) sectorMask[s] = new float[ah, aw];
            var size = data.size; var c = new Vector2(size.x * .5f, size.z * .5f);
            for (int y = 0; y < ah; y++)
                for (int x = 0; x < aw; x++)
                {
                    float wx = (x + .5f) / aw * size.x - c.x, wz = (y + .5f) / ah * size.z - c.y;
                    float east = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-sectorBlendMetres, sectorBlendMetres, wx));
                    float north = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-sectorBlendMetres, sectorBlendMetres, wz));
                    sectorMask[0][y, x] = east * north; sectorMask[1][y, x] = (1 - east) * north;
                    sectorMask[2][y, x] = (1 - east) * (1 - north); sectorMask[3][y, x] = east * (1 - north);
                }
            treesDegraded = data.treeInstances;
            dw = data.detailWidth; dh = data.detailHeight;
            if (data.detailPrototypes.Length >= 2)
            {
                dryDetail = data.GetDetailLayer(0, 0, dw, dh, 0);
                greenDetailBase = data.GetDetailLayer(0, 0, dw, dh, 1);
            }
        }

        private void OnDestroy() { if (data != null) Destroy(data); }

        /// <summary>Inicia la recuperación gradual de un sector completo.</summary>
        public void RestoreSector(int s)
        {
            Initialize();
            if (sectorTarget[s] >= 1f) return;
            sectorTarget[s] = 1f; dirty = true;
            // Árboles del sector: versión verde.
            var center = Center; var trees = data.treeInstances; var size = data.size; var origin = terrain.transform.position;
            for (int i = 0; i < trees.Length; i++)
            {
                var w = Vector3.Scale(trees[i].position, size) + origin;
                if (SectorOf(w, center) == s && (trees[i].prototypeIndex & 1) == 0) trees[i].prototypeIndex += 1;
            }
            data.treeInstances = trees; terrain.Flush();
            GreenTrees = CountGreen(trees);
            // Pasto del sector: seco → verde.
            if (dryDetail != null && !detailRestored[s])
            {
                detailRestored[s] = true;
                var dry = data.GetDetailLayer(0, 0, dw, dh, 0); var green = data.GetDetailLayer(0, 0, dw, dh, 1);
                for (int y = 0; y < dh; y++)
                    for (int x = 0; x < dw; x++)
                    {
                        var w = new Vector3((x + .5f) / dw * size.x, 0, (y + .5f) / dh * size.z) + origin;
                        if (SectorOf(w, center) != s) continue;
                        // Pasto verde donde había ichu y también en parte del suelo desnudo.
                        int g = dryDetail[y, x] > 0 ? 2 : ((x * 7 + y * 13) % 5 == 0 && dryDetailAllowed(x, y) ? 1 : 0);
                        green[y, x] = Mathf.Max(green[y, x], Mathf.Max(greenDetailBase[y, x], g));
                        dry[y, x] = 0;
                    }
                data.SetDetailLayer(0, 0, 0, dry); data.SetDetailLayer(0, 0, 1, green);
            }
        }

        private bool dryDetailAllowed(int x, int y)
        {
            float u = (x + .5f) / dw, v = (y + .5f) / dh;
            return data.GetSteepness(u, v) < 26f && degraded[Mathf.Min((int)(v * ah), ah - 1), Mathf.Min((int)(u * aw), aw - 1), 3] < .3f;
        }

        private static int CountGreen(TreeInstance[] t) { int n = 0; foreach (var i in t) if ((i.prototypeIndex & 1) == 1) n++; return n; }

        /// <summary>Mancha de suelo recuperado alrededor de un residuo recogido.</summary>
        public void RestoreSpot(Vector3 world)
        {
            Initialize();
            var local = world - terrain.transform.position;
            float cx = local.x / data.size.x * aw, cy = local.z / data.size.z * ah;
            float r = spotRadius / data.size.x * aw;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(cx - r), 0, aw - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(cx + r), 0, aw - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(cy - r), 0, ah - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(cy + r), 0, ah - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                    if (d < 1f) spot[y, x] = Mathf.Max(spot[y, x], Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(.35f, 1f, d)) * .85f);
                }
            Apply(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        private void Update()
        {
            if (data == null) return;
            bool animating = false;
            for (int s = 0; s < 4; s++)
            {
                if (sectorLevel[s] == sectorTarget[s]) continue;
                sectorLevel[s] = Mathf.MoveTowards(sectorLevel[s], sectorTarget[s], Time.deltaTime / sectorFadeSeconds);
                animating = true;
            }
            if ((animating || dirty) && Time.unscaledTime >= nextApply)
            {
                nextApply = Time.unscaledTime + .12f; dirty = animating;
                Apply(0, 0, aw, ah);
            }
        }

        /// <summary>Recalcula las capas de suelo en una región: seco → pasto según sector y manchas.</summary>
        private void Apply(int x0, int y0, int w, int h)
        {
            var map = new float[h, w, layers];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int gx = x0 + x, gy = y0 + y;
                    float f = spot[gy, gx];
                    for (int s = 0; s < 4; s++) f = Mathf.Max(f, sectorLevel[s] * sectorMask[s][gy, gx]);
                    for (int l = 0; l < layers; l++) map[y, x, l] = degraded[gy, gx, l];
                    if (layers > 1) { float moved = degraded[gy, gx, 0] * f; map[y, x, 0] -= moved; map[y, x, 1] += moved; }
                }
            data.SetAlphamaps(x0, y0, map);
        }

        /// <summary>Fracción de pasto en una posición (para pruebas).</summary>
        public float GrassAt(Vector3 world)
        {
            var local = world - terrain.transform.position;
            int x = Mathf.Clamp((int)(local.x / data.size.x * aw), 0, aw - 1), y = Mathf.Clamp((int)(local.z / data.size.z * ah), 0, ah - 1);
            return data.GetAlphamaps(x, y, 1, 1)[0, 0, 1];
        }

        public void ResetAll()
        {
            Initialize();
            for (int s = 0; s < 4; s++) { sectorLevel[s] = sectorTarget[s] = 0; detailRestored[s] = false; }
            System.Array.Clear(spot, 0, spot.Length);
            data.SetAlphamaps(0, 0, degraded);
            data.treeInstances = treesDegraded; terrain.Flush();
            GreenTrees = CountGreen(treesDegraded);
            if (dryDetail != null) { data.SetDetailLayer(0, 0, 0, dryDetail); data.SetDetailLayer(0, 0, 1, greenDetailBase); }
        }
    }
}
