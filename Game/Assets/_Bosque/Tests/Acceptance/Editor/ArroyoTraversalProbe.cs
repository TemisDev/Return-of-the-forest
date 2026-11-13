using System;
using System.Collections.Generic;
using System.Text;
using Bosque.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bosque.Acceptance
{
    /// <summary>
    /// C-02. Sonda AUTOMATIZADA de recorrido sobre la escena de integración de Codex.
    /// Abre ArroyoPlayable.unity, conduce el ThirdPersonMotor real con su API pública
    /// (Simulate / ThirdPersonCamera.Step) y NUNCA guarda la escena.
    /// No sustituye la prueba manual con teclado y ratón: mide geometría y alcance.
    ///
    /// Batch: -executeMethod Bosque.Acceptance.ArroyoTraversalProbe.RunBatch
    /// </summary>
    public static class ArroyoTraversalProbe
    {
        private const string IntegrationScene = "Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity";
        private const float Dt = 0.02f;

        private static ThirdPersonMotor motor;
        private static CharacterController cc;
        private static ThirdPersonCamera rig;
        private static Transform safeRespawn;
        private static readonly StringBuilder Out = new StringBuilder();
        private static int passed, failed, info;

        public static void RunBatch()
        {
            int code = 0;
            Out.Length = 0;
            passed = failed = info = 0;
            try
            {
                EditorSceneManager.OpenScene(IntegrationScene, OpenSceneMode.Single);
                var player = GameObject.Find("Player");
                if (player == null) throw new InvalidOperationException("No hay objeto 'Player' en " + IntegrationScene);
                motor = player.GetComponent<ThirdPersonMotor>();
                cc = player.GetComponent<CharacterController>();
                rig = Object.FindFirstObjectByType<ThirdPersonCamera>();
                safeRespawn = FindDeep("SafeRespawn");
                Physics.SyncTransforms();
                Line("INFO", "Setup", "CC h=" + cc.height + " r=" + cc.radius + " step=" + cc.stepOffset +
                                      " slope=" + cc.slopeLimit + " skin=" + cc.skinWidth);

                RunRoute();
                RunLowLog();
                RunRamps();
                RunChannelEscape();
                RunObstacleReach();
                RunFallAndRails();
                RunGateAndWalls();
                RunCamera();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }

            Out.Insert(0, "[C02] Resumen: " + passed + " OK, " + failed + " FALLA, " + info + " INFO\n");
            Debug.Log(Out.ToString());
            // Sin SaveScene: la escena de integración queda intacta en disco.
            EditorApplication.Exit(code);
        }

        // ------------------------------------------------------------ casos

        private static void RunRoute()
        {
            Teleport(new Vector3(3f, 0f, 0f), 0f);
            float t0 = 0f, minY = float.MaxValue;
            var wps = new[]
            {
                new Vector3(3f, 0f, 29f), new Vector3(4.7f, 0f, 29f), new Vector3(4.7f, 0f, 31.2f), new Vector3(3f, 0f, 36f)
            };
            bool ok = true;
            foreach (var wp in wps)
            {
                var r = WalkTo(wp, 15f);
                t0 += r.Time;
                minY = Mathf.Min(minY, r.MinY);
                if (!r.Reached) { ok = false; break; }
            }
            Result(ok && minY > -0.05f, "R-01 Recorrido", "Spawn→Exit rodeando el tronco por el este: " +
                   (ok ? "llega" : "NO llega") + " en " + t0.ToString("0.0") + " s, Y mínima " + minY.ToString("0.00") +
                   ", final " + P());

            Teleport(new Vector3(3f, 0f, 0f), 0f);
            var straight = WalkTo(new Vector3(3f, 0f, 36f), 15f);
            Line("INFO", "R-02 Línea recta", "Spawn→Exit por X=3 sin rodear: " +
                 (straight.Reached ? "llega" : "se detiene") + " en " + P() + " tras " + straight.Time.ToString("0.0") + " s");
        }

        private static void RunLowLog()
        {
            Teleport(new Vector3(2f, 0f, 27.5f), 0f);
            bool jumped = false;
            float t = 0f, peak = 0f;
            while (t < 3f)
            {
                bool jump = !jumped && motor.transform.position.z >= 29.0f;
                if (jump) jumped = true;
                Step(Vector2.up, jump, 0f);
                peak = Mathf.Max(peak, motor.transform.position.y);
                t += Dt;
            }
            Result(motor.transform.position.z > 31f, "R-03 Salto tronco", "Saltar LowLog_Optional (0.35 m) en Z≈29: final " +
                   P() + ", altura máx. " + peak.ToString("0.00") + " m");
        }

        private static void RunRamps()
        {
            foreach (var z in new[] { 5f, 31.5f })
            {
                Teleport(new Vector3(1f, 0f, z), -90f);
                var down = WalkTo(new Vector3(-3.3f, 0f, z), 5f);
                float bottomY = motor.transform.position.y;
                var up = WalkTo(new Vector3(1f, 0f, z), 5f);
                bool ok = down.Reached && bottomY < -0.5f && up.Reached && motor.transform.position.y > -0.05f;
                Result(ok, "R-04 Rampa Z" + z, "Bajar al cauce (Y " + bottomY.ToString("0.00") + ") y subir caminando: " +
                       (ok ? "sí" : "NO") + ", final " + P());
            }
        }

        private static void RunChannelEscape()
        {
            // Entre Obstacle_01 y Obstacle_02 no hay rampa.
            Teleport(new Vector3(-2.5f, -0.6f, 14f), 90f);
            var walk = WalkTo(new Vector3(1f, 0f, 14f), 3f);
            bool walkedOut = motor.transform.position.y > -0.05f;
            Line("INFO", "R-05 Salir del cauce sin rampa", "Caminando hacia la orilla en Z14: " +
                 (walkedOut ? "sale" : "no sale (pared 0.6 m > step 0.3)") + ", " + P());

            // Un único salto manteniendo la dirección hacia la orilla; se suelta al pisar la orilla
            // (Z14 está en el tramo del borde abierto: seguir empujando haría caer por el este).
            float t = 0f;
            bool jumpSent = false, out2 = false;
            while (t < 2f)
            {
                bool j = !jumpSent;
                jumpSent = true;
                Step(Vector2.up, j, 90f);
                t += Dt;
                var p = motor.transform.position;
                if (p.y > -0.05f && p.x > -0.6f) { out2 = true; break; }
            }
            for (int s = 0; s < 25; s++) Step(Vector2.zero, false, 90f);
            out2 = out2 && motor.transform.position.y > -0.05f;
            Result(out2, "R-06 Salir del cauce saltando", "Un salto pegado a la pared en Z14: " + (out2 ? "sale" : "NO sale") +
                   " en " + t.ToString("0.00") + " s, " + P());
        }

        private static void RunObstacleReach()
        {
            for (int i = 1; i <= 3; i++)
            {
                var obs = FindDeep("Obstacle_0" + i);
                var col = obs.GetComponent<Collider>();
                float z = obs.position.z;
                float top = col.bounds.max.y;

                Teleport(new Vector3(2f, 0f, z), -90f);
                var r = WalkTo(new Vector3(-2.5f, 0f, z), 4f);
                var chest = motor.transform.position + Vector3.up * 0.9f;
                float toSurface = Vector3.Distance(chest, col.ClosestPoint(chest));
                var flat = obs.position - motor.transform.position;
                flat.y = 0f;
                bool onTop = motor.transform.position.y > 0.05f;
                Line("INFO", "R-07 Obstáculo " + i + " desde orilla",
                    "Top collider Y=" + top.ToString("0.00") + " (sobre orilla " + (top - 0f).ToString("0.00") +
                    " m); jugador termina en " + P() + (onTop ? " ENCIMA del obstáculo" : "") +
                    "; dist. pecho→superficie " + toSurface.ToString("0.00") + " m; horizontal al centro " +
                    flat.magnitude.ToString("0.00") + " m");
                if (onTop)
                {
                    // ¿Puede cruzar el cauce por encima?
                    var cross = WalkTo(new Vector3(-3.6f, 0f, z), 3f);
                    Line("INFO", "R-07b Obstáculo " + i + " como puente", "Sigue hacia el muro oeste: " + P());
                }

                // Parado en el borde de la orilla, sin subirse: alcance mínimo realista.
                Teleport(new Vector3(-0.62f, 0f, z), -90f);
                chest = motor.transform.position + Vector3.up * 0.9f;
                float edgeSurface = Vector3.Distance(chest, col.ClosestPoint(chest));
                flat = obs.position - motor.transform.position;
                flat.y = 0f;
                Result(edgeSurface <= 1.5f, "R-08 Alcance obstáculo " + i,
                    "Desde el borde (X -0.62): pecho→superficie " + edgeSurface.ToString("0.00") +
                    " m, horizontal al centro " + flat.magnitude.ToString("0.00") + " m (umbral de la prueba: 1.5 m a superficie)");
            }
        }

        private static void RunFallAndRails()
        {
            Teleport(new Vector3(5f, 0f, 20f), 90f);
            float t = 0f, leftEdge = -1f;
            bool respawned = false;
            var last = motor.transform.position;
            while (t < 6f)
            {
                Step(Vector2.up, false, 90f);
                t += Dt;
                var p = motor.transform.position;
                if (leftEdge < 0f && p.x > 6.2f) leftEdge = t;
                if ((p - last).magnitude > 3f) { respawned = true; break; }
                last = p;
            }
            float dist = Vector3.Distance(motor.transform.position, safeRespawn.position);
            Result(respawned && dist < 0.2f, "R-09 Caída borde abierto",
                "Salir por X>6 en Z20: " + (respawned ? "retorna" : "NO retorna") + " a SafeRespawn (dist " +
                dist.ToString("0.00") + " m) " + (respawned && leftEdge >= 0 ? (t - leftEdge).ToString("0.00") + " s tras dejar el borde" : "") +
                "; retorno a Z " + motor.transform.position.z.ToString("0.0") + " (retroceso ≈ " + (20f - motor.transform.position.z).ToString("0") + " m)");

            // Repetición: 5 caídas seguidas.
            int ok = 0;
            for (int k = 0; k < 5; k++)
            {
                Teleport(new Vector3(5f, 0f, 14f + k * 3f), 90f);
                t = 0f;
                last = motor.transform.position;
                while (t < 6f)
                {
                    Step(Vector2.up, false, 90f);
                    t += Dt;
                    if ((motor.transform.position - last).magnitude > 3f) break;
                    last = motor.transform.position;
                }
                // Asentarse y comprobar que queda quieto en el suelo.
                for (int s = 0; s < 25; s++) Step(Vector2.zero, false, 0f);
                if (Vector3.Distance(motor.transform.position, safeRespawn.position) < 0.2f) ok++;
            }
            Result(ok == 5, "R-10 Caída repetida", ok + "/5 caídas retornan y quedan apoyadas en SafeRespawn");

            Teleport(new Vector3(5f, 0f, 6f), 90f);
            WalkTo(new Vector3(8f, 0f, 6f), 2f);
            bool blocked = motor.transform.position.x < 5.6f && motor.transform.position.y > -0.05f;
            Result(blocked, "R-11 Barandilla", "Caminar contra EastRail_South (0.5 m) en Z6: " + (blocked ? "bloquea" : "NO bloquea") + ", " + P());

            float tt = 0f;
            bool sent = false, fell = false;
            last = motor.transform.position;
            while (tt < 4f)
            {
                Step(Vector2.up, !sent, 90f);
                sent = true;
                tt += Dt;
                if ((motor.transform.position - last).magnitude > 3f) { fell = true; break; }
                last = motor.transform.position;
            }
            Line("INFO", "R-12 Saltar barandilla", "Un salto contra la barandilla en Z6: " + (fell ? "la supera y cae (retorna)" : "no la supera") + ", " + P());
        }

        private static void RunGateAndWalls()
        {
            Teleport(new Vector3(3f, 0f, 36f), 0f);
            var r = WalkTo(new Vector3(3f, 0f, 39.8f), 3f);
            Result(motor.transform.position.z > 39f, "R-13 Portal", "Cruzar el portal hasta el muro norte: " + P());

            Teleport(new Vector3(-2.5f, -0.6f, 7f), -90f);
            WalkTo(new Vector3(-6f, -0.6f, 7f), 2f);
            bool w = motor.transform.position.x > -4f + cc.radius - 0.05f;
            Result(w, "R-14 Muro oeste", "Empujar contra WestWall desde el cauce: X " + motor.transform.position.x.ToString("0.00") + (w ? " (no atraviesa)" : " ATRAVIESA"));

            Teleport(new Vector3(3f, 0f, -2f), 180f);
            WalkTo(new Vector3(3f, 0f, -7f), 2f);
            bool s = motor.transform.position.z > -4f + cc.radius - 0.05f;
            Result(s, "R-15 Muro sur", "Empujar contra SouthWall: Z " + motor.transform.position.z.ToString("0.00") + (s ? " (no atraviesa)" : " ATRAVIESA"));

            // Tramo despejado: de (0.5, 0, 1) hacia el noreste durante 0.5 s (≈2.25 m nominales).
            var start = new Vector3(0.5f, 0f, 1f);
            Teleport(start, 0f);
            start = motor.transform.position;
            for (int i = 0; i < 25; i++) Step(new Vector2(1f, 1f), false, 0f);
            var d = motor.transform.position - start;
            d.y = 0f;
            Result(d.magnitude > 2.1f && d.magnitude < 2.4f, "R-16 Diagonal en nivel",
                "W+D 0.5 s en tramo libre: " + d.magnitude.ToString("0.00") + " m (nominal 2.25 m; sin bonificación diagonal)");
        }

        private static void RunCamera()
        {
            if (rig == null) { Result(false, "C-xx Cámara", "No hay ThirdPersonCamera en la escena"); return; }
            var spots = new (string name, Vector3 pos, float yaw)[]
            {
                ("muro sur", new Vector3(3f, 0f, -3.5f), 180f),
                ("barandilla este", new Vector3(5.2f, 0f, 6f), 90f),
                ("cauce junto a muro oeste", new Vector3(-3.4f, -0.6f, 7f), -90f),
                ("bajo el dintel", new Vector3(3f, 0f, 38.5f), 0f),
                ("muro norte", new Vector3(3f, 0f, 39.5f), 0f),
                ("borde abierto", new Vector3(5.5f, 0f, 20f), 90f),
                ("junto a Obstacle_02", new Vector3(-0.6f, 0f, 18f), -90f),
            };
            var playerCols = new HashSet<Collider>(motor.GetComponentsInChildren<Collider>(true));
            foreach (var spot in spots)
            {
                Teleport(spot.pos, spot.yaw);
                rig.Configure(motor.transform);
                int samples = 0, inside = 0, occluded = 0, tooClose = 0;
                float minDist = float.MaxValue;
                string worst = "";
                foreach (float pitchTarget in new[] { 20f, -20f, 65f })
                {
                    SetPitch(pitchTarget);
                    for (int k = 0; k < 24; k++)
                    {
                        rig.Step(new Vector2(15f / 0.12f, 0f), Dt);
                        for (int s = 0; s < 20; s++) rig.Step(Vector2.zero, Dt);
                        samples++;
                        var cam = rig.transform.position;
                        var focus = motor.transform.position + Vector3.up * 1.35f;
                        float dist = Vector3.Distance(cam, focus);
                        minDist = Mathf.Min(minDist, dist);
                        bool isInside = false;
                        foreach (var c in Physics.OverlapSphere(cam, 0.05f, ~0, QueryTriggerInteraction.Ignore))
                            if (!playerCols.Contains(c)) { isInside = true; worst = c.name + " yaw " + rig.Yaw.ToString("0") + " pitch " + pitchTarget; }
                        if (isInside) inside++;
                        if (Physics.Linecast(focus, cam, out var hit, ~0, QueryTriggerInteraction.Ignore) && !playerCols.Contains(hit.collider)) occluded++;
                        if (dist < 0.6f) tooClose++;
                    }
                }
                Result(inside == 0 && occluded == 0, "C-01 Cámara " + spot.name,
                    samples + " orientaciones: dentro de geometría " + inside + ", línea de visión tapada " + occluded +
                    ", distancia <0.6 m " + tooClose + ", mínima " + minDist.ToString("0.00") + " m" + (worst != "" ? " (ej. " + worst + ")" : ""));
            }
        }

        // ------------------------------------------------------------ utilidades

        private struct WalkResult { public bool Reached; public float Time; public float MinY; }

        private static WalkResult WalkTo(Vector3 target, float maxTime)
        {
            var r = new WalkResult { MinY = float.MaxValue };
            var checkpoint = motor.transform.position;
            float sinceCheck = 0f;
            while (r.Time < maxTime)
            {
                var p = motor.transform.position;
                r.MinY = Mathf.Min(r.MinY, p.y);
                var d = target - p;
                d.y = 0f;
                if (d.magnitude < 0.25f) { r.Reached = true; return r; }
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                Step(Vector2.up, false, yaw);
                r.Time += Dt;
                sinceCheck += Dt;
                if (sinceCheck >= 0.5f)
                {
                    if ((motor.transform.position - checkpoint).magnitude < 0.05f) return r; // atascado
                    checkpoint = motor.transform.position;
                    sinceCheck = 0f;
                }
            }
            return r;
        }

        private static void SetPitch(float target)
        {
            // pitch inicial 20 tras Configure; Step resta lookDelta.y * 0.12.
            for (int i = 0; i < 20; i++) rig.Step(new Vector2(0f, 200f), Dt); // fuerza -20
            float delta = target - (-20f);
            int steps = Mathf.CeilToInt(Mathf.Abs(delta) / 20f);
            for (int i = 0; i < steps; i++) rig.Step(new Vector2(0f, -(delta / steps) / 0.12f), Dt);
        }

        private static void Teleport(Vector3 pos, float yaw)
        {
            cc.enabled = false;
            motor.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            cc.enabled = true;
            Physics.SyncTransforms();
            for (int i = 0; i < 15; i++) Step(Vector2.zero, false, yaw);
        }

        private static void Step(Vector2 input, bool jump, float yaw)
        {
            motor.Simulate(input, jump, yaw, Dt);
            Physics.SyncTransforms();
        }

        private static string P() => motor.transform.position.ToString("F2");

        private static Transform FindDeep(string name)
        {
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            throw new InvalidOperationException("Falta " + name);
        }

        private static void Result(bool ok, string id, string detail)
        {
            if (ok) passed++; else failed++;
            Line(ok ? "OK" : "FALLA", id, detail);
        }

        private static void Line(string status, string id, string detail)
        {
            if (status == "INFO") info++;
            Out.AppendLine("[C02] " + status + " | " + id + " | " + detail);
        }
    }
}
