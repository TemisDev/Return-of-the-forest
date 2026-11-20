using System;
using System.Collections;
using System.Collections.Generic;
using Bosque.Audio;
using Bosque.Character;
using Bosque.Interaction;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    /// <summary>
    /// Adaptador de la zona "Arroyo dormido": escucha los eventos reales (sesión, interacción, motor y pasos) y
    /// los traduce a audio y gestos del personaje. Es el único punto acoplado al arroyo; el sistema de audio y
    /// el personaje son genéricos y sirven para otras zonas con su propio adaptador.
    /// </summary>
    public sealed class ArroyoAudioBridge : MonoBehaviour
    {
        [Serializable] public struct ObstacleCue { public string obstacleId; public string cue; }

        [SerializeField] private ArroyoSession session;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private AmbienceController ambience;
        [SerializeField] private MusicPlayer music;
        [SerializeField] private CharacterAnimationDriver character;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float exitUnlockDelay = 1.6f;
        [SerializeField] private ObstacleCue[] obstacleCues =
        {
            new ObstacleCue { obstacleId = "arroyo.log", cue = "remove.log" },
            new ObstacleCue { obstacleId = "arroyo.rocks", cue = "remove.rocks" },
            new ObstacleCue { obstacleId = "arroyo.branches", cue = "remove.branches" },
        };

        private readonly HashSet<string> removed = new HashSet<string>();
        private bool wasRestored, wasCompleted, bound;
        private PlayerInteractor interactor;
        private Coroutine pendingUnlock;

        public void Configure(ArroyoSession owner, SfxPlayer player, AmbienceController amb, MusicPlayer mus,
            CharacterAnimationDriver driver, Transform exit)
        {
            Unbind(); session = owner; sfx = player; ambience = amb; music = mus; character = driver; exitPoint = exit;
            // En el editor solo se guardan referencias; la suscripción a eventos ocurre al jugar (o en pruebas que llamen a Bind).
            if (Application.isPlaying && isActiveAndEnabled) Bind();
        }

        private void OnEnable() => Bind();
        private void Start() => Bind();
        private void OnDisable() => Unbind();

        /// <summary>Se suscribe a la sesión, la interacción, el motor y los pasos. Público para pruebas en modo edición.</summary>
        public void Bind()
        {
            if (bound || session == null) return;
            session.InitializeFromScene();
            if (session.Progress == null) return;
            bound = true;
            session.StateChanged += OnStateChanged;
            var motor = session.Player;
            interactor = motor != null ? motor.GetComponent<PlayerInteractor>() : null;
            if (interactor != null) interactor.Interacted += OnInteracted;
            if (motor != null) { motor.Jumped += OnJumped; motor.Landed += OnLanded; }
            if (character != null) character.Footstep += OnFootstep;
            Snapshot();
            ambience?.SetRestored(wasRestored, true);
            music?.SetRestored(wasRestored, true);
        }

        private void Unbind()
        {
            if (!bound) return;
            bound = false;
            if (session != null) session.StateChanged -= OnStateChanged;
            if (interactor != null) interactor.Interacted -= OnInteracted;
            var motor = session != null ? session.Player : null;
            if (motor != null) { motor.Jumped -= OnJumped; motor.Landed -= OnLanded; }
            if (character != null) character.Footstep -= OnFootstep;
        }

        private void Snapshot()
        {
            removed.Clear();
            foreach (var c in obstacleCues) if (session.Progress.IsRemoved(c.obstacleId)) removed.Add(c.obstacleId);
            wasRestored = session.Progress.IsRestored; wasCompleted = session.Progress.IsCompleted;
        }

        private void OnStateChanged()
        {
            var p = session.Progress;
            // Retiradas nuevas: un sonido por obstáculo, aunque se pulse E repetidamente (el progreso no cambia dos veces).
            foreach (var c in obstacleCues)
            {
                if (!p.IsRemoved(c.obstacleId) || removed.Contains(c.obstacleId)) continue;
                removed.Add(c.obstacleId);
                sfx?.Play(c.cue, ObstaclePosition(c.obstacleId));
            }
            bool reset = p.RemovedCount == 0 && removed.Count > 0;
            if (reset)
            {
                removed.Clear();
                if (pendingUnlock != null) { StopCoroutine(pendingUnlock); pendingUnlock = null; }
                sfx?.StopAll();
                ambience?.SetRestored(false);
                music?.SetRestored(false);
                character?.ResetPose();
            }
            if (p.IsRestored && !wasRestored)
            {
                var at = session.Player != null ? session.Player.transform.position : transform.position;
                sfx?.Play("restore", at);
                ambience?.SetRestored(true);
                music?.SetRestored(true);
                character?.PlayRestore();
                if (exitPoint != null && isActiveAndEnabled) pendingUnlock = StartCoroutine(Unlock());
            }
            if (p.IsCompleted && !wasCompleted)
                sfx?.Play("complete", session.Player != null ? session.Player.transform.position : transform.position);
            wasRestored = p.IsRestored; wasCompleted = p.IsCompleted;
        }

        private IEnumerator Unlock()
        {
            yield return new WaitForSeconds(exitUnlockDelay);
            pendingUnlock = null;
            if (session.Progress.IsRestored) sfx?.Play("exit.unlock", exitPoint.position);
        }

        private Vector3 ObstaclePosition(string id)
        {
            foreach (var o in FindObjectsByType<RemovableObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (o.ObstacleId == id) return o.transform.position;
            return session.Player != null ? session.Player.transform.position : transform.position;
        }

        private void OnInteracted(InteractionTarget target) => sfx?.Play("interact", target != null ? target.transform.position : transform.position);
        private void OnJumped() => sfx?.Play("jump", session.Player.transform.position);
        private void OnLanded(float impact) { if (impact > 3f) sfx?.Play("land", session.Player.transform.position, Mathf.InverseLerp(3f, 12f, impact) * .6f + .4f); }
        private void OnFootstep(bool left, float intensity) =>
            sfx?.Play("footstep", session.Player.transform.position, .45f + .55f * intensity);
    }
}
