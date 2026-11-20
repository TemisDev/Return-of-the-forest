using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bosque.Audio;
using Bosque.Character;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bosque.Tests.PlayMode
{
    /// <summary>
    /// Pruebas en modo juego de la Guardiana (animación) y del audio de zona sobre la escena real ArroyoPlayable.
    /// Conducen el motor y la interacción por API (sin teclado), como el verificador de builds.
    /// </summary>
    public sealed class GuardianaPlayModeTests
    {
        private ArroyoSession session;
        private ThirdPersonMotor motor;
        private PlayerInteractor actor;
        private CharacterAnimationDriver driver;
        private Animator animator;
        private ThirdPersonInput input;
        private SfxPlayer sfx;
        private AmbienceController ambience;
        private MusicPlayer music;
        private readonly List<string> played = new List<string>();

        [UnitySetUp]
        public IEnumerator Load()
        {
            BosqueAudio.ResetToDefaults();
            SceneManager.LoadScene("ArroyoPlayable");
            yield return null; yield return null;
            session = Object.FindFirstObjectByType<ArroyoSession>();
            motor = session.Player;
            input = motor.GetComponent<ThirdPersonInput>(); input.enabled = false; // sin teclado: el test conduce el motor
            actor = motor.GetComponent<PlayerInteractor>();
            driver = motor.GetComponentInChildren<CharacterAnimationDriver>();
            animator = driver.Animator;
            sfx = Object.FindFirstObjectByType<SfxPlayer>();
            ambience = Object.FindFirstObjectByType<AmbienceController>();
            music = Object.FindFirstObjectByType<MusicPlayer>();
            played.Clear();
            sfx.Played += played.Add;
        }

        [UnityTearDown]
        public IEnumerator Unload() { if (sfx != null) sfx.Played -= played.Add; yield return null; }

        private IEnumerator Run(Vector2 dir, float seconds, bool jumpFirst = false, bool walk = false)
        {
            float t = 0; bool first = true;
            while (t < seconds)
            {
                input.enabled = false; // la sesión lo reactiva al cambiar el progreso
                motor.Simulate(dir, jumpFirst && first, 0, Time.deltaTime, walk);
                first = false; t += Time.deltaTime; yield return null;
            }
        }

        private IEnumerator Settle(float seconds) => Run(Vector2.zero, seconds);

        private bool Base(string state) => animator.GetCurrentAnimatorStateInfo(0).IsName(state);
        private bool Gesture(string state) => animator.GetCurrentAnimatorStateInfo(1).IsName(state);

        private static float ArmDrop(Animator a)
        {
            var up = a.GetBoneTransform(HumanBodyBones.LeftUpperArm); var low = a.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var d = (low.position - up.position).normalized;
            return Vector3.Angle(d, Vector3.down);
        }

        [UnityTest]
        public IEnumerator RigAvatarAndControllerAreValid()
        {
            Assert.That(animator.isHuman && animator.avatar.isValid, "Avatar humanoide válido");
            Assert.That(animator.applyRootMotion, Is.False, "Sin root motion: mueve el CharacterController");
            var names = animator.parameters.Select(p => p.name).ToArray();
            CollectionAssert.IsSubsetOf(new[] { "Speed", "Grounded", "Falling", "VerticalSpeed", "Jump", "Interact", "Restore" }, names);
            Assert.That(animator.layerCount, Is.EqualTo(2));
            var capsule = motor.transform.Find("ProvisionalBody").GetComponent<MeshRenderer>();
            Assert.That(capsule.enabled, Is.False, "La cápsula provisional ya no se ve");
            Assert.That(motor.GetComponentsInChildren<Collider>().Count(c => !(c is CharacterController)), Is.EqualTo(0), "El personaje no añade colliders al jugador");
            yield return Settle(.5f);
            Assert.That(Base("Locomocion"), "Estado inicial");
            Assert.That(ArmDrop(animator), Is.LessThan(45f), "En reposo los brazos caen (no está en pose T)");
            // Orientación: al avanzar, la punta del pie apunta en la dirección del motor.
            yield return Run(Vector2.up, .6f);
            var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot); var toes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            var fwd = toes.position - foot.position; fwd.y = 0;
            Assert.That(Vector3.Dot(fwd.normalized, motor.transform.forward), Is.GreaterThan(.5f), "El personaje mira hacia donde avanza");
        }

        [UnityTest]
        public IEnumerator LocomotionFollowsRealSpeedWithoutRootMotion()
        {
            yield return Settle(.3f);
            var start = motor.transform.position;
            yield return Run(Vector2.up, 1.2f);
            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(motor.RunSpeed).Within(.6f), "Trote a la velocidad real del motor");
            Assert.That(Base("Locomocion"));
            float travelled = Vector3.Distance(start, motor.transform.position);
            Assert.That(travelled, Is.GreaterThan(4f), "El personaje se desplaza con el motor");
            Assert.That(driver.transform.localPosition.magnitude, Is.LessThan(.2f), "La malla no se separa del jugador (sin deriva de root motion)");
            yield return Run(Vector2.up, 1f, false, true);
            Assert.That(animator.GetFloat("Speed"), Is.LessThan(2f), "Shift: caminar");
            yield return Settle(.8f);
            Assert.That(animator.GetFloat("Speed"), Is.LessThan(.2f), "Vuelve a reposo al soltar");
        }

        [UnityTest]
        public IEnumerator JumpGoesThroughAirAndLandsBackToLocomotion()
        {
            yield return Settle(.3f);
            int jumps = played.Count(p => p == "jump");
            motor.Simulate(Vector2.zero, true, 0, Time.deltaTime); yield return null;
            bool sawJump = false, sawAir = false, sawLand = false;
            var trace = new System.Text.StringBuilder();
            for (float t = 0; t < 2.5f; t += Time.deltaTime)
            {
                input.enabled = false;
                motor.Simulate(Vector2.zero, false, 0, Time.deltaTime); yield return null;
                var st = Base("SaltoInicio") ? "S" : Base("Aire") ? "A" : Base("Aterrizaje") ? "L" : Base("Locomocion") ? "M" : "?";
                if (animator.IsInTransition(0)) st += "~";
                trace.Append($"{st}{(motor.IsGrounded ? "g" : "")} ");
                sawJump |= Base("SaltoInicio") || animator.GetNextAnimatorStateInfo(0).IsName("SaltoInicio");
                sawAir |= Base("Aire"); sawLand |= Base("Aterrizaje");
            }
            Debug.Log("[Guardiana][Salto] " + trace);
            Assert.That(sawJump, "Estado de impulso");
            Assert.That(sawAir || sawLand, "Pasa por el aire o aterriza");
            Assert.That(Base("Locomocion"), "Vuelve a locomoción sin quedarse bloqueado");
            Assert.That(motor.IsGrounded);
            Assert.That(played.Count(p => p == "jump") - jumps, Is.EqualTo(1), "Un solo sonido de salto");
        }

        [UnityTest]
        public IEnumerator FallingOffAStepUsesAirPoseAndLands()
        {
            motor.Teleport(new Vector3(3, 2.2f, 4), Quaternion.identity);
            bool sawAir = false;
            for (float t = 0; t < 2f; t += Time.deltaTime)
            {
                motor.Simulate(Vector2.zero, false, 0, Time.deltaTime); yield return null;
                sawAir |= Base("Aire");
            }
            Assert.That(sawAir, "Caída usa la pose de aire");
            Assert.That(Base("Locomocion") || Base("Aterrizaje"));
            Assert.That(played.Contains("land"), "Sonido de aterrizaje tras una caída");
        }

        [UnityTest]
        public IEnumerator InteractionAnimatesOnceAndCannotDuplicateProgressOrSounds()
        {
            motor.Teleport(new Vector3(-.6f, 0, 10), Quaternion.identity); yield return Settle(.3f);
            Assert.That(actor.TryInteract(), Is.True);
            Assert.That(actor.TryInteract(), Is.False, "La segunda pulsación no retira nada");
            Assert.That(actor.TryInteract(), Is.False);
            yield return Settle(.2f);
            Assert.That(Gesture("Interactuar"), "Gesto de interacción en la capa superior");
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(1));
            Assert.That(played.Count(p => p == "remove.log"), Is.EqualTo(1), "Un solo sonido de retirada del tronco");
            Assert.That(played.Count(p => p == "interact"), Is.EqualTo(1), "Un solo sonido de interacción válida");
            yield return Run(Vector2.right, .5f);
            Assert.That(motor.HorizontalSpeed, Is.GreaterThan(3f), "El gesto no bloquea el movimiento");
        }

        [UnityTest]
        public IEnumerator RestorationChangesAmbienceOnceAndRestartReturnsToDegraded()
        {
            Assert.That(ambience.Blend, Is.EqualTo(0f).Within(.01f), "Empieza degradado");
            Assert.That(ambience.ActiveLoops, Is.GreaterThanOrEqualTo(5), "Bucles ambientales sonando");
            float musicStart = music.Level;
            foreach (int z in new[] { 10, 18, 26 })
            {
                motor.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity); yield return Settle(.25f);
                Assert.That(actor.TryInteract(), Is.True, "Retirar en Z=" + z);
            }
            Assert.That(session.Progress.IsRestored);
            CollectionAssert.IsSubsetOf(new[] { "remove.log", "remove.rocks", "remove.branches", "restore" }, played);
            yield return Settle(.4f);
            Assert.That(Gesture("Restaurar") || Gesture("Interactuar"), "Gesto de restauración");
            yield return Settle(2f);
            Assert.That(ambience.Blend, Is.GreaterThan(.2f).And.LessThan(1f), "Transición progresiva, no instantánea");
            Assert.That(played.Count(p => p == "exit.unlock"), Is.EqualTo(1), "Desbloqueo de salida una vez");
            yield return Settle(4f);
            Assert.That(ambience.Blend, Is.EqualTo(1f).Within(.01f));
            Assert.That(music.Level, Is.GreaterThan(musicStart), "La música crece al restaurar");
            Assert.That(played.Count(p => p == "restore"), Is.EqualTo(1), "Restauración suena una sola vez");
            motor.Teleport(new Vector3(3, 0, 33), Quaternion.identity);
            for (int i = 0; i < 90 && !session.Progress.IsCompleted; i++) { motor.Simulate(Vector2.up, false, 0, Time.deltaTime); session.TryExit(motor); yield return null; }
            Assert.That(session.Progress.IsCompleted);
            Assert.That(played.Count(p => p == "complete"), Is.EqualTo(1), "Final del nivel una vez");
            int loops = ambience.ActiveLoops;
            session.Restart(); yield return Settle(1.6f);
            Assert.That(ambience.Restored, Is.False);
            Assert.That(ambience.Blend, Is.EqualTo(0f).Within(.01f), "Reinicio vuelve al ambiente degradado");
            Assert.That(ambience.ActiveLoops, Is.EqualTo(loops), "Los bucles no se reinician ni se duplican");
            Assert.That(Base("Locomocion") && Gesture("Vacio"), "Reinicio deja al personaje en reposo");
        }

        [UnityTest]
        public IEnumerator FootstepsOnlyWhileMovingOnGround()
        {
            yield return Settle(1f);
            Assert.That(played.Count(p => p == "footstep"), Is.Zero, "Sin pasos en reposo");
            yield return Run(Vector2.up, 2f);
            int steps = played.Count(p => p == "footstep");
            Assert.That(steps, Is.InRange(3, 12), "Pasos al trotar 2 s: " + steps);
            Assert.That(sfx.ActiveVoices, Is.LessThanOrEqualTo(14));
        }

        [UnityTest]
        public IEnumerator VolumeApiAffectsBuses()
        {
            BosqueAudio.SetVolume(AudioBus.Ambience, 0f, false);
            yield return null;
            Assert.That(BosqueAudio.Effective(AudioBus.Ambience), Is.EqualTo(0f));
            BosqueAudio.SetVolume(AudioBus.Master, .5f, false);
            Assert.That(BosqueAudio.Effective(AudioBus.Sfx), Is.EqualTo(.5f).Within(.001f));
            BosqueAudio.ResetToDefaults();
        }

        [UnityTest]
        public IEnumerator CaptureAnimatedCharacter()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Character/CAPTURAS"));
            Directory.CreateDirectory(dir);
            var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            var follow = cam.GetComponent<ThirdPersonCamera>(); if (follow != null) follow.enabled = false;
            IEnumerator Shot(string name, Vector3 offset, float lookHeight)
            {
                yield return null;
                var p = motor.transform.position;
                cam.transform.position = p + motor.transform.rotation * offset;
                cam.transform.LookAt(p + Vector3.up * lookHeight);
                var rt = new RenderTexture(1600, 900, 24); var prev = cam.targetTexture;
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var img = new Texture2D(1600, 900, TextureFormat.RGB24, false); img.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); img.Apply();
                File.WriteAllBytes(Path.Combine(dir, "guardiana_" + name + ".png"), img.EncodeToPNG());
                cam.targetTexture = prev; RenderTexture.active = null; rt.Release(); Object.Destroy(rt); Object.Destroy(img);
            }
            motor.Teleport(new Vector3(3, 0, 1), Quaternion.Euler(0, 180, 0)); yield return Settle(1.2f);
            yield return Shot("01_reposo_frente", new Vector3(.3f, 1.4f, 2.6f), 1.05f);
            motor.Teleport(new Vector3(3, 0, 1), Quaternion.Euler(0, 150, 0)); yield return Settle(.6f);
            yield return Shot("02_reposo_tres_cuartos", new Vector3(.9f, 1.3f, 2.2f), 1.1f);
            motor.Teleport(new Vector3(3, 0, -2), Quaternion.identity);
            yield return Run(Vector2.up, .5f, false, true);
            yield return Shot("03_caminar", new Vector3(2.2f, 1.2f, 1.2f), 1f);
            yield return Run(Vector2.up, .55f);
            yield return Shot("04_trotar", new Vector3(2.4f, 1.3f, 1.4f), 1f);
            motor.Simulate(Vector2.up, true, 0, Time.deltaTime); yield return Run(Vector2.up, .32f);
            yield return Shot("05_salto", new Vector3(2.6f, 1.5f, .6f), 1.3f);
            yield return Settle(1.2f);
            motor.Teleport(new Vector3(-.6f, 0, 10), Quaternion.Euler(0, -90, 0)); yield return Settle(.3f);
            actor.TryInteract(); yield return Settle(.5f);
            yield return Shot("06_interactuar", new Vector3(1.2f, 1.4f, 2.4f), 1f);
            foreach (int z in new[] { 18, 26 }) { motor.Teleport(new Vector3(-.6f, 0, z), Quaternion.Euler(0, -90, 0)); yield return Settle(.2f); actor.TryInteract(); }
            yield return Settle(.35f);
            yield return Shot("07_restaurar", new Vector3(.6f, 1.4f, 2.6f), 1.1f);
            yield return Settle(6f);
            motor.Teleport(new Vector3(3, 0, 12), Quaternion.identity);
            yield return Run(Vector2.up, .6f);
            if (follow != null) follow.enabled = true;
            yield return null;
            yield return Shot("08_espalda_juego", new Vector3(0, 2.2f, -4.2f), 1.45f);
            Assert.Pass("Capturas en " + dir);
        }
    }
}
