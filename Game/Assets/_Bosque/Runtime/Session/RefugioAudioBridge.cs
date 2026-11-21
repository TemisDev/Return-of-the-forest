using Bosque.Audio;
using Bosque.Character;
using Bosque.Interaction;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    /// <summary>Adaptador de "El refugio perdido": eventos reales → audio y gestos de la Guardiana.</summary>
    public sealed class RefugioAudioBridge : MonoBehaviour
    {
        [SerializeField] private RefugioSession session;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private AmbienceController ambience;
        [SerializeField] private MusicPlayer music;
        [SerializeField] private CharacterAnimationDriver character;
        private ItemCarrier carrier;
        private bool bound, wasRestored, wasCompleted;

        public void Configure(RefugioSession owner, SfxPlayer player, AmbienceController amb, MusicPlayer mus, CharacterAnimationDriver driver)
        { session = owner; sfx = player; ambience = amb; music = mus; character = driver; }

        private void Start() => Bind();
        private void OnDisable() => Unbind();

        public void Bind()
        {
            if (bound || session == null) return;
            session.InitializeFromScene();
            bound = true;
            session.StateChanged += OnState;
            session.ItemPlaced += OnPlaced;
            session.PlantGrew += OnGrew;
            session.AttemptRejected += OnRejected;
            var motor = session.Player;
            carrier = motor.GetComponent<ItemCarrier>();
            if (carrier != null) { carrier.PickedUp += OnPicked; carrier.Dropped += OnDropped; }
            motor.Jumped += OnJumped; motor.Landed += OnLanded;
            if (character != null) character.Footstep += OnFootstep;
            wasRestored = session.Progress.IsRestored; wasCompleted = session.Progress.IsCompleted;
            ambience?.SetRestored(wasRestored, true); music?.SetRestored(wasRestored, true);
        }

        private void Unbind()
        {
            if (!bound) return;
            bound = false;
            session.StateChanged -= OnState; session.ItemPlaced -= OnPlaced; session.PlantGrew -= OnGrew; session.AttemptRejected -= OnRejected;
            if (carrier != null) { carrier.PickedUp -= OnPicked; carrier.Dropped -= OnDropped; }
            if (session.Player != null) { session.Player.Jumped -= OnJumped; session.Player.Landed -= OnLanded; }
            if (character != null) character.Footstep -= OnFootstep;
        }

        private Vector3 PlayerPos => session.Player.transform.position;
        private void OnPicked(RefugeItem item) => sfx?.Play("interact", item.transform.position);
        private void OnDropped(RefugeItem item) => sfx?.Play("land", item.transform.position, .5f);
        private void OnPlaced(RefugeSpot spot, RefugeItem item)
        {
            sfx?.Play(item.SfxCue, spot.transform.position);
            character?.PlayRestore();
        }
        private void OnGrew(GrowthPoint p) { sfx?.Play("grow", p.transform.position); character?.PlayRestore(); }
        private void OnRejected(string hint) => sfx?.Play("grow.fail", PlayerPos);

        private void OnState()
        {
            var pr = session.Progress;
            if (pr.IsRestored && !wasRestored) { sfx?.Play("restore", PlayerPos); ambience?.SetRestored(true); music?.SetRestored(true); }
            if (!pr.IsRestored && wasRestored) { ambience?.SetRestored(false); music?.SetRestored(false); sfx?.StopAll(); character?.ResetPose(); }
            if (pr.IsCompleted && !wasCompleted) sfx?.Play("complete", PlayerPos);
            wasRestored = pr.IsRestored; wasCompleted = pr.IsCompleted;
        }

        private void OnJumped() => sfx?.Play("jump", PlayerPos);
        private void OnLanded(float impact) { if (impact > 3f) sfx?.Play("land", PlayerPos, Mathf.InverseLerp(3f, 12f, impact) * .6f + .4f); }
        private void OnFootstep(bool left, float intensity) => sfx?.Play("footstep", PlayerPos, .45f + .55f * intensity);
    }
}
