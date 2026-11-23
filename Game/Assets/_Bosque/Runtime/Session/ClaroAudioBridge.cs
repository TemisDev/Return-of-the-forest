using Bosque.Abilities;
using Bosque.Audio;
using Bosque.Character;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    /// <summary>Adaptador de la zona "El claro vacío": eventos reales → audio y gestos de la Guardiana.</summary>
    public sealed class ClaroAudioBridge : MonoBehaviour
    {
        [SerializeField] private ClaroSession session;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private AmbienceController ambience;
        [SerializeField] private MusicPlayer music;
        [SerializeField] private CharacterAnimationDriver character;
        private GrowthAbility ability;
        private bool bound, wasRestored, wasCompleted;

        public void Configure(ClaroSession owner, SfxPlayer player, AmbienceController amb, MusicPlayer mus, CharacterAnimationDriver driver)
        { session = owner; sfx = player; ambience = amb; music = mus; character = driver; }

        private void Start() => Bind();
        private void OnDisable() => Unbind();

        public void Bind()
        {
            if (bound || session == null) return;
            session.InitializeFromScene();
            bound = true;
            session.StateChanged += OnState;
            session.PlantGrew += OnGrew;
            session.AttemptRejected += OnRejected;
            var motor = session.Player;
            ability = motor.GetComponent<GrowthAbility>();
            motor.Jumped += OnJumped; motor.Landed += OnLanded;
            if (character != null) character.Footstep += OnFootstep;
            wasRestored = session.Progress.IsRestored; wasCompleted = session.Progress.IsCompleted;
            ambience?.SetRestored(wasRestored, true); music?.SetRestored(wasRestored, true);
        }

        private void Unbind()
        {
            if (!bound) return;
            bound = false;
            session.StateChanged -= OnState; session.PlantGrew -= OnGrew; session.AttemptRejected -= OnRejected;
            if (session.Player != null) { session.Player.Jumped -= OnJumped; session.Player.Landed -= OnLanded; }
            if (character != null) character.Footstep -= OnFootstep;
        }

        private void OnGrew(GrowthPoint p)
        {
            sfx?.Play("grow", p.transform.position);
            character?.PlayRestore();
        }

        private void OnRejected(GrowthPoint p, string hint) => sfx?.Play("grow.fail", p.transform.position);

        private void OnState()
        {
            var pr = session.Progress;
            if (pr.IsRestored && !wasRestored)
            {
                sfx?.Play("bridge", session.RootBridge.transform.position);
                sfx?.Play("restore", session.Player.transform.position);
                ambience?.SetRestored(true); music?.SetRestored(true);
            }
            if (!pr.IsRestored && wasRestored) { ambience?.SetRestored(false); music?.SetRestored(false); sfx?.StopAll(); character?.ResetPose(); }
            if (pr.IsCompleted && !wasCompleted) sfx?.Play("complete", session.Player.transform.position);
            wasRestored = pr.IsRestored; wasCompleted = pr.IsCompleted;
        }

        private void OnJumped() => sfx?.Play("jump", session.Player.transform.position);
        private void OnLanded(float impact) { if (impact > 3f) sfx?.Play("land", session.Player.transform.position, Mathf.InverseLerp(3f, 12f, impact) * .6f + .4f); }
        private void OnFootstep(bool left, float intensity) => sfx?.Play("footstep", session.Player.transform.position, .45f + .55f * intensity);
    }
}
