using Bosque.Audio;
using Bosque.Character;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    /// <summary>Adaptador de "El valle olvidado": eventos reales → audio y gestos de la Guardiana.</summary>
    public sealed class ValleAudioBridge : MonoBehaviour
    {
        [SerializeField] private ValleSession session;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private AmbienceController ambience;
        [SerializeField] private MusicPlayer music;
        [SerializeField] private CharacterAnimationDriver character;
        private bool bound;

        public void Configure(ValleSession owner, SfxPlayer player, AmbienceController amb, MusicPlayer mus, CharacterAnimationDriver driver)
        { session = owner; sfx = player; ambience = amb; music = mus; character = driver; }

        private void Start() => Bind();
        private void OnDisable() => Unbind();

        private Vector3 P => session.Player.transform.position;

        public void Bind()
        {
            if (bound || session == null) return;
            session.InitializeFromScene();
            bound = true;
            session.Collected += OnCollected; session.WoodCleared += OnWood; session.DepositedItems += OnDeposit;
            session.AttemptRejected += OnRejected; session.SectorRestored += OnSector; session.ValleRestored += OnValle;
            session.StateChanged += OnState;
            if (session.Pulse != null) session.Pulse.Pulsed += OnPulse;
            session.Player.Jumped += OnJumped; session.Player.Landed += OnLanded;
            if (character != null) character.Footstep += OnFootstep;
            ambience?.SetRestored(false, true); music?.SetRestored(false, true);
        }

        private void Unbind()
        {
            if (!bound) return;
            bound = false;
            session.Collected -= OnCollected; session.WoodCleared -= OnWood; session.DepositedItems -= OnDeposit;
            session.AttemptRejected -= OnRejected; session.SectorRestored -= OnSector; session.ValleRestored -= OnValle;
            session.StateChanged -= OnState;
            if (session.Pulse != null) session.Pulse.Pulsed -= OnPulse;
            if (session.Player != null) { session.Player.Jumped -= OnJumped; session.Player.Landed -= OnLanded; }
            if (character != null) character.Footstep -= OnFootstep;
        }

        private bool wasRestored, wasCompleted;
        private void OnCollected(LitterItem i) => sfx?.Play("interact", i.transform.position);
        private void OnWood(FallenWood w) { sfx?.Play(w.Label.Contains("rama") ? "remove.branches" : "remove.log", w.transform.position); character?.PlayRestore(); }
        private void OnDeposit(RecycleBin b, int n) => sfx?.Play("remove.rocks", b.transform.position);
        private void OnRejected(string hint) => sfx?.Play("grow.fail", P);
        private void OnSector(ValleSector s) { sfx?.Play("grow", P); character?.PlayRestore(); }
        private void OnValle() { sfx?.Play("restore", P); sfx?.Play("bridge", P); ambience?.SetRestored(true); music?.SetRestored(true); }
        private void OnPulse(int n) => sfx?.Play("exit.unlock", P, .6f);
        private void OnState()
        {
            if (!session.IsRestored && wasRestored) { ambience?.SetRestored(false); music?.SetRestored(false); sfx?.StopAll(); character?.ResetPose(); }
            if (session.IsCompleted && !wasCompleted) sfx?.Play("complete", P);
            wasRestored = session.IsRestored; wasCompleted = session.IsCompleted;
        }
        private void OnJumped() => sfx?.Play("jump", P);
        private void OnLanded(float impact) { if (impact > 3f) sfx?.Play("land", P, Mathf.InverseLerp(3f, 12f, impact) * .6f + .4f); }
        private void OnFootstep(bool left, float intensity) => sfx?.Play("footstep", P, .45f + .55f * intensity);
    }
}
