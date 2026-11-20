using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bosque.ArtV6.Editor
{
    /// <summary>Sistemas de partículas V6 (prefabs en Art/V6/Particles). Sin scripts de gameplay.</summary>
    public static class ArroyoArtV6Particles
    {
        private const string Out = ArroyoArtV6Builder.Root + "/Particles";

        // ------------------------------------------------------------------ materiales
        private static Material FxMat(string name, string tex, bool additive, float light, float soft = 0.4f, float cutoff = 0f, Color? tint = null)
        {
            var m = ArroyoArtV6Builder.EnsureMaterial(name, "Bosque/V6/Particle");
            m.SetTexture("_BaseMap", ArroyoArtV6Builder.Tex("Textures/FX", tex));
            m.SetColor("_TintColor", tint ?? Color.white);
            m.SetFloat("_SrcBlend", (float)(additive ? BlendMode.SrcAlpha : BlendMode.SrcAlpha));
            m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_LightInfluence", light);
            m.SetFloat("_SoftDistance", soft);
            m.SetFloat("_AlphaCutoff", cutoff);
            m.renderQueue = 3000;
            return m;
        }

        // ------------------------------------------------------------------ utilidades
        private static ParticleSystem NewSystem(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortMode = ParticleSystemSortMode.Distance;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.playOnAwake = true;
            var shape = ps.shape; shape.enabled = true;
            return ps;
        }

        private static ParticleSystem.MinMaxCurve R(float a, float b) => new ParticleSystem.MinMaxCurve(a, b);

        private static ParticleSystem.MinMaxGradient Fade(Color c, float inT = 0.15f, float outT = 0.7f)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0), new GradientColorKey(c, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(c.a, inT), new GradientAlphaKey(c.a, outT), new GradientAlphaKey(0, 1) });
            return new ParticleSystem.MinMaxGradient(g);
        }

        private static ParticleSystem.MinMaxCurve Curve(params float[] kv)
        {
            var c = new AnimationCurve();
            for (int i = 0; i < kv.Length; i += 2) c.AddKey(kv[i], kv[i + 1]);
            return new ParticleSystem.MinMaxCurve(1f, c);
        }

        private static void Sheet(ParticleSystem ps, int x, int y, bool randomFrame, float cycles = 0f)
        {
            var ts = ps.textureSheetAnimation;
            ts.enabled = true;
            ts.mode = ParticleSystemAnimationMode.Grid;
            ts.numTilesX = x; ts.numTilesY = y;
            ts.animation = ParticleSystemAnimationType.WholeSheet;
            if (randomFrame)
            {
                ts.startFrame = R(0, 0.999f); // normalizado 0-1 sobre todas las celdas
                ts.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            }
            ts.cycleCount = cycles > 0 ? (int)cycles : 1;
        }

        private static void Noise(ParticleSystem ps, float strength, float freq)
        {
            var n = ps.noise; n.enabled = true; n.strength = strength; n.frequency = freq; n.scrollSpeed = 0.2f; n.damping = true;
            n.quality = ParticleSystemNoiseQuality.Medium;
        }

        private static void Save(ParticleSystem root, string name)
        {
            Directory.CreateDirectory(Out);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{Out}/{name}.prefab");
            Object.DestroyImmediate(root.gameObject);
        }

        private static void Mat(ParticleSystem ps, Material m, ParticleSystemRenderMode mode = ParticleSystemRenderMode.Billboard)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = m; r.renderMode = mode;
            if (mode == ParticleSystemRenderMode.Billboard) r.alignment = ParticleSystemRenderSpace.View;
        }

        // ------------------------------------------------------------------ sistemas
        public static void BuildAll()
        {
            var mote = FxMat("M_FX_Mote", "T_FX_SoftDot", false, 1f, 0.1f);
            var dust = FxMat("M_FX_Dust", "T_FX_Dust_2x2", false, 1f, 0.8f);
            var mist = FxMat("M_FX_Mist", "T_FX_Smoke_2x2", false, 1f, 0.6f);
            var splash = FxMat("M_FX_Splash", "T_FX_Splash_2x2", false, 0.85f, 0.15f);
            var drop = FxMat("M_FX_Droplet", "T_FX_Droplet", false, 0.85f, 0.05f);
            var ripple = FxMat("M_FX_Ripple", "T_FX_Ripple", false, 0.9f, 0.02f);
            var glow = FxMat("M_FX_Glow", "T_FX_SoftDot", true, 0f, 0.1f);
            var sparkle = FxMat("M_FX_Sparkle", "T_FX_Sparkle", true, 0f, 0.1f);
            var leaves = FxMat("M_FX_Leaves", "T_FX_Leaves_2x2", false, 1f, 0.05f, 0.3f);
            var butterfly = FxMat("M_FX_Butterfly", "T_FX_Butterfly_2x1", false, 1f, 0.05f, 0.35f);

            // 1. Polvo en suspensión (degradado)
            {
                var ps = NewSystem("PFX_V6_DustMotes");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(7, 12); m.startSpeed = R(0.02f, 0.1f);
                m.startSize = R(0.02f, 0.05f); m.maxParticles = 500; m.startColor = new Color(1f, 0.86f, 0.62f, 0.55f);
                var e = ps.emission; e.rateOverTime = 40;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(11f, 2.5f, 44f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(new Color(1, 1, 1, 1), 0.2f, 0.75f);
                Noise(ps, 0.25f, 0.25f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = R(0.12f, 0.25f); v.y = R(-0.02f, 0.03f); v.z = R(-0.02f, 0.02f);
                Mat(ps, mote); Save(ps, "PFX_V6_DustMotes");
            }
            // 2. Ráfagas de polvo a ras de suelo (degradado)
            {
                var ps = NewSystem("PFX_V6_DustGusts");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(4, 7); m.startSpeed = 0;
                m.startSize = R(1.4f, 3.2f); m.startRotation = R(0, 6.28f); m.maxParticles = 60; m.startColor = new Color(0.78f, 0.66f, 0.5f, 0.2f);
                var e = ps.emission; e.rateOverTime = 2.2f;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(10f, 0.2f, 44f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.25f, 0.6f);
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Curve(0, 0.55f, 1, 1.35f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = R(0.5f, 1.1f); v.y = R(0.05f, 0.2f); v.z = R(-0.2f, 0.2f);
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.3f, 0.3f);
                Sheet(ps, 2, 2, true);
                Mat(ps, dust); Save(ps, "PFX_V6_DustGusts");
            }
            // 3. Neblina sobre el arroyo (restaurado)
            {
                var ps = NewSystem("PFX_V6_StreamMist");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(7, 11); m.startSpeed = 0;
                m.startSize = R(1.2f, 2.6f); m.startRotation = R(0, 6.28f); m.maxParticles = 50; m.startColor = new Color(0.92f, 0.96f, 1f, 0.13f);
                var e = ps.emission; e.rateOverTime = 4;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(2.6f, 0.1f, 44f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.3f, 0.6f);
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Curve(0, 0.7f, 1, 1.3f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.z = R(0.15f, 0.35f); v.y = R(0.02f, 0.08f); v.x = R(-0.05f, 0.05f);
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = R(-0.15f, 0.15f);
                Sheet(ps, 2, 2, true);
                Mat(ps, mist); Save(ps, "PFX_V6_StreamMist");
            }
            // 4. Salpicaduras contra rocas (restaurado), con gotas
            {
                var ps = NewSystem("PFX_V6_WaterSplash");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(0.35f, 0.65f); m.startSpeed = R(0.6f, 1.4f);
                m.startSize = R(0.18f, 0.42f); m.startRotation = R(-0.4f, 0.4f); m.gravityModifier = 0.6f; m.maxParticles = 60;
                m.startColor = new Color(0.95f, 0.98f, 1f, 0.75f);
                var e = ps.emission; e.rateOverTime = 14;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Cone; s.angle = 28; s.radius = 0.25f; s.rotation = new Vector3(-90, 0, 0);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.1f, 0.5f);
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Curve(0, 0.6f, 1, 1.25f);
                Sheet(ps, 2, 2, true);
                Mat(ps, splash);
                var d = NewSystem("Droplets", ps.transform);
                var dm = d.main; dm.loop = true; dm.prewarm = true; dm.startLifetime = R(0.4f, 0.8f); dm.startSpeed = R(1.2f, 2.6f);
                dm.startSize = R(0.025f, 0.055f); dm.gravityModifier = 1.0f; dm.maxParticles = 80; dm.startColor = new Color(0.9f, 0.95f, 1f, 0.85f);
                var de = d.emission; de.rateOverTime = 30;
                var dsh = d.shape; dsh.shapeType = ParticleSystemShapeType.Cone; dsh.angle = 35; dsh.radius = 0.2f; dsh.rotation = new Vector3(-90, 0, 0);
                Mat(d, drop, ParticleSystemRenderMode.Stretch);
                var dr = d.GetComponent<ParticleSystemRenderer>(); dr.velocityScale = 0.06f; dr.lengthScale = 1.2f;
                Save(ps, "PFX_V6_WaterSplash");
            }
            // 5. Ondas en la superficie (restaurado)
            {
                var ps = NewSystem("PFX_V6_WaterRipples");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(1.2f, 2f); m.startSpeed = 0;
                m.startSize = R(0.5f, 1.1f); m.maxParticles = 50; m.startColor = new Color(1, 1, 1, 0.28f);
                var e = ps.emission; e.rateOverTime = 6;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(2.4f, 0f, 44f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.1f, 0.35f);
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Curve(0, 0.2f, 1, 1f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.z = R(0.3f, 0.5f); v.x = R(0, 0); v.y = R(0, 0);
                Mat(ps, ripple, ParticleSystemRenderMode.HorizontalBillboard);
                Save(ps, "PFX_V6_WaterRipples");
            }
            // 6. Polen luminoso (restaurado)
            {
                var ps = NewSystem("PFX_V6_Pollen");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(6, 10); m.startSpeed = R(0.02f, 0.08f);
                m.startSize = R(0.025f, 0.05f); m.maxParticles = 400; m.startColor = new Color(1f, 0.95f, 0.7f, 0.6f);
                var e = ps.emission; e.rateOverTime = 25;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(11f, 3f, 44f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.2f, 0.7f);
                Noise(ps, 0.35f, 0.3f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = R(0.02f, 0.08f); v.x = R(0.05f, 0.12f); v.z = R(-0.02f, 0.02f);
                Mat(ps, glow); Save(ps, "PFX_V6_Pollen");
            }
            // 7. Luciérnagas (restaurado, para escenas en sombra o atardecer)
            {
                var ps = NewSystem("PFX_V6_Fireflies");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(4, 7); m.startSpeed = R(0.05f, 0.2f);
                m.startSize = R(0.08f, 0.14f); m.maxParticles = 60; m.startColor = new Color(0.75f, 1f, 0.35f, 1f);
                var e = ps.emission; e.rateOverTime = 7;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(8f, 1.6f, 40f);
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                          new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0.15f, 0.35f), new GradientAlphaKey(1, 0.55f), new GradientAlphaKey(0.2f, 0.75f), new GradientAlphaKey(0, 1) });
                var col = ps.colorOverLifetime; col.enabled = true; col.color = g;
                Noise(ps, 0.6f, 0.5f);
                Mat(ps, glow); Save(ps, "PFX_V6_Fireflies");
            }
            // 8. Hojas que caen (restaurado; versión seca para degradado)
            foreach (var dry in new[] { false, true })
            {
                var ps = NewSystem(dry ? "PFX_V6_FallingLeaves_Dry" : "PFX_V6_FallingLeaves");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(8, 13); m.startSpeed = 0;
                m.startSize = R(0.1f, 0.18f); m.maxParticles = 80; m.gravityModifier = 0.035f;
                m.startRotation3D = true; m.startRotationX = R(0, 6.28f); m.startRotationY = R(0, 6.28f); m.startRotationZ = R(0, 6.28f);
                m.startColor = dry ? new Color(0.85f, 0.7f, 0.55f, 1f) : Color.white;
                var e = ps.emission; e.rateOverTime = dry ? 1.5f : 3f;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(12f, 0.5f, 44f);
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true; rot.x = R(-2f, 2f); rot.y = R(-1.5f, 1.5f); rot.z = R(-2f, 2f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.05f, 0.85f);
                Noise(ps, 0.5f, 0.35f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = R(0.2f, 0.45f); v.y = R(-0.3f, -0.15f); v.z = R(-0.1f, 0.1f);
                var ts = ps.textureSheetAnimation; ts.enabled = true; ts.numTilesX = 2; ts.numTilesY = 2;
                ts.startFrame = dry ? R(0.5f, 0.999f) : R(0f, 0.74f); ts.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                Mat(ps, leaves);
                ps.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.Local;
                Save(ps, dry ? "PFX_V6_FallingLeaves_Dry" : "PFX_V6_FallingLeaves");
            }
            // 9. Mariposas (restaurado)
            {
                var ps = NewSystem("PFX_V6_Butterflies");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(10, 16); m.startSpeed = R(0.2f, 0.4f);
                m.startSize = R(0.12f, 0.17f); m.maxParticles = 8;
                var e = ps.emission; e.rateOverTime = 0.6f;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(8f, 1.2f, 36f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.05f, 0.9f);
                Noise(ps, 1.1f, 0.35f);
                var ts = ps.textureSheetAnimation; ts.enabled = true; ts.numTilesX = 2; ts.numTilesY = 1;
                ts.frameOverTime = Curve(0, 0, 1, 1); ts.cycleCount = 60;
                Mat(ps, butterfly); Save(ps, "PFX_V6_Butterflies");
            }
            // 10. Brillo del arco de salida (restaurado)
            {
                var ps = NewSystem("PFX_V6_ExitGlow");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(2.5f, 4f); m.startSpeed = 0;
                m.startSize = R(0.04f, 0.09f); m.maxParticles = 120; m.startColor = new Color(1f, 0.85f, 0.5f, 0.9f);
                var e = ps.emission; e.rateOverTime = 22;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(4f, 2.4f, 0.6f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.2f, 0.6f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = R(0.2f, 0.45f); v.x = R(0, 0); v.z = R(0, 0);
                Noise(ps, 0.2f, 0.6f);
                Mat(ps, glow); Save(ps, "PFX_V6_ExitGlow");
            }
            // 11. Indicador sutil de obstáculo interactuable (degradado)
            {
                var ps = NewSystem("PFX_V6_ObstacleHint");
                var m = ps.main; m.loop = true; m.prewarm = true; m.startLifetime = R(1.2f, 2f); m.startSpeed = R(0.05f, 0.15f);
                m.startSize = R(0.12f, 0.24f); m.maxParticles = 20; m.startColor = new Color(0.75f, 1f, 0.7f, 0.7f);
                var e = ps.emission; e.rateOverTime = 4;
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(2.6f, 0.6f, 1.0f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.25f, 0.5f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = R(0.15f, 0.3f); v.x = R(0, 0); v.z = R(0, 0);
                Mat(ps, sparkle); Save(ps, "PFX_V6_ObstacleHint");
            }
            // 12. Ráfaga al retirar obstáculo: polvo + astillas + brotes de luz
            {
                var ps = NewSystem("PFX_V6_ObstacleRemoved");
                var m = ps.main; m.loop = false; m.duration = 2.5f; m.startLifetime = R(1.2f, 2.2f); m.startSpeed = R(0.8f, 2.2f);
                m.startSize = R(0.7f, 1.6f); m.startRotation = R(0, 6.28f); m.maxParticles = 40; m.stopAction = ParticleSystemStopAction.Destroy;
                m.startColor = new Color(0.62f, 0.52f, 0.38f, 0.5f);
                var e = ps.emission; e.rateOverTime = 0; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Hemisphere; s.radius = 0.8f;
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.05f, 0.45f);
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = Curve(0, 0.5f, 1, 1.6f);
                var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.drag = 3f; lv.dampen = 0.2f;
                Sheet(ps, 2, 2, true);
                Mat(ps, dust);

                var chips = NewSystem("Debris", ps.transform);
                var cm = chips.main; cm.loop = false; cm.duration = 2f; cm.startLifetime = R(1.2f, 2f); cm.startSpeed = R(2f, 4f);
                cm.startSize = R(0.06f, 0.14f); cm.gravityModifier = 1.2f; cm.maxParticles = 40;
                cm.startRotation3D = true; cm.startRotationX = R(0, 6.28f); cm.startRotationZ = R(0, 6.28f);
                cm.startColor = new Color(0.7f, 0.55f, 0.4f, 1f);
                var ce = chips.emission; ce.rateOverTime = 0; ce.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
                var csh = chips.shape; csh.shapeType = ParticleSystemShapeType.Hemisphere; csh.radius = 0.5f;
                var crot = chips.rotationOverLifetime; crot.enabled = true; crot.separateAxes = true; crot.x = R(-6, 6); crot.z = R(-6, 6);
                var coll = chips.collision; coll.enabled = true; coll.type = ParticleSystemCollisionType.World; coll.dampen = 0.5f; coll.bounce = 0.25f; coll.quality = ParticleSystemCollisionQuality.Low;
                var cts = chips.textureSheetAnimation; cts.enabled = true; cts.numTilesX = 2; cts.numTilesY = 2; cts.startFrame = R(0.5f, 0.999f); cts.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                Mat(chips, leaves);

                var sp = NewSystem("Sprouts", ps.transform);
                var sm = sp.main; sm.loop = false; sm.duration = 2f; sm.startDelay = 0.25f; sm.startLifetime = R(1.2f, 2f); sm.startSpeed = R(0.3f, 1f);
                sm.startSize = R(0.15f, 0.35f); sm.maxParticles = 30; sm.startColor = new Color(0.7f, 1f, 0.55f, 1f);
                var se = sp.emission; se.rateOverTime = 0; se.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
                var ssh = sp.shape; ssh.shapeType = ParticleSystemShapeType.Box; ssh.scale = new Vector3(2.6f, 0.4f, 1f);
                var scol = sp.colorOverLifetime; scol.enabled = true; scol.color = Fade(Color.white, 0.1f, 0.5f);
                var sv = sp.velocityOverLifetime; sv.enabled = true; sv.space = ParticleSystemSimulationSpace.World; sv.y = R(0.6f, 1.2f); sv.x = R(0, 0); sv.z = R(0, 0);
                Mat(sp, sparkle);
                Save(ps, "PFX_V6_ObstacleRemoved");
            }
            // 13. Ola de restauración (al completar 3/3): agua, brillo y hojas a lo largo del cauce
            {
                var ps = NewSystem("PFX_V6_RestorationWave");
                var m = ps.main; m.loop = false; m.duration = 4f; m.startLifetime = R(2f, 3.5f); m.startSpeed = R(0.3f, 1.2f);
                m.startSize = R(0.12f, 0.3f); m.maxParticles = 300; m.stopAction = ParticleSystemStopAction.Destroy;
                m.startColor = new Color(0.8f, 1f, 0.6f, 1f);
                var e = ps.emission; e.rateOverTime = 0;
                e.SetBursts(new[] { new ParticleSystem.Burst(0f, 80), new ParticleSystem.Burst(0.6f, 60), new ParticleSystem.Burst(1.2f, 40) });
                var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(9f, 0.4f, 42f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(Color.white, 0.1f, 0.55f);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = R(0.5f, 1.4f); v.x = R(0, 0); v.z = R(0, 0);
                Noise(ps, 0.4f, 0.5f);
                Mat(ps, sparkle);

                var wm = NewSystem("WaterMist", ps.transform);
                var mm = wm.main; mm.loop = false; mm.duration = 3f; mm.startLifetime = R(3, 5); mm.startSpeed = R(0.2f, 0.6f);
                mm.startSize = R(1.5f, 3f); mm.maxParticles = 60; mm.startColor = new Color(0.9f, 0.96f, 1f, 0.22f); mm.startRotation = R(0, 6.28f);
                var me = wm.emission; me.rateOverTime = 0; me.SetBursts(new[] { new ParticleSystem.Burst(0f, 30), new ParticleSystem.Burst(0.8f, 20) });
                var msh = wm.shape; msh.shapeType = ParticleSystemShapeType.Box; msh.scale = new Vector3(2.6f, 0.1f, 42f); msh.position = new Vector3(0, 0, 0);
                var mcol = wm.colorOverLifetime; mcol.enabled = true; mcol.color = Fade(Color.white, 0.2f, 0.5f);
                var msz = wm.sizeOverLifetime; msz.enabled = true; msz.size = Curve(0, 0.6f, 1, 1.5f);
                Sheet(wm, 2, 2, true);
                Mat(wm, mist);

                var lf = NewSystem("Leaves", ps.transform);
                var lm = lf.main; lm.loop = false; lm.duration = 3f; lm.startLifetime = R(3, 5); lm.startSpeed = R(0.5f, 1.5f);
                lm.startSize = R(0.1f, 0.18f); lm.maxParticles = 80; lm.gravityModifier = 0.05f;
                lm.startRotation3D = true; lm.startRotationX = R(0, 6.28f); lm.startRotationZ = R(0, 6.28f);
                var le = lf.emission; le.rateOverTime = 0; le.SetBursts(new[] { new ParticleSystem.Burst(0.3f, 60) });
                var lsh = lf.shape; lsh.shapeType = ParticleSystemShapeType.Box; lsh.scale = new Vector3(9f, 0.5f, 42f);
                var lrot = lf.rotationOverLifetime; lrot.enabled = true; lrot.separateAxes = true; lrot.x = R(-3, 3); lrot.z = R(-3, 3);
                var lcol = lf.colorOverLifetime; lcol.enabled = true; lcol.color = Fade(Color.white, 0.05f, 0.8f);
                Noise(lf, 0.8f, 0.4f);
                var lts = lf.textureSheetAnimation; lts.enabled = true; lts.numTilesX = 2; lts.numTilesY = 2; lts.startFrame = R(0f, 0.49f); lts.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                Mat(lf, leaves);
                lf.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.Local;
                Save(ps, "PFX_V6_RestorationWave");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
