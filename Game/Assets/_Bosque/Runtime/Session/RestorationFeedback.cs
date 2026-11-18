using UnityEngine;

namespace Bosque.Session
{
    public sealed class RestorationFeedback : MonoBehaviour
    {
        [SerializeField] private ArroyoSession session;
        [SerializeField] private ParticleSystem particles;
        private ArroyoSession subscribed;
        private bool wasRestored;
        public int PresentationCount { get; private set; }
        public void Configure(ArroyoSession owner, ParticleSystem effect)
        {
            Unsubscribe(); session = owner; particles = effect;
            if (isActiveAndEnabled) Subscribe();
        }
        private void OnEnable() => Subscribe();
        private void OnDisable()
        {
            Unsubscribe();
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void Subscribe()
        {
            if (session == null || subscribed != null) return;
            subscribed = session;
            wasRestored = session.Progress != null && session.Progress.IsRestored;
            subscribed.StateChanged += OnStateChanged;
        }
        private void Unsubscribe()
        {
            if (subscribed != null) subscribed.StateChanged -= OnStateChanged;
            subscribed = null;
        }
        private void OnStateChanged()
        {
            if (!isActiveAndEnabled) return;
            bool restored = session.Progress != null && session.Progress.IsRestored;
            if (restored && !wasRestored && particles != null)
            {
                particles.transform.position = session.Player.transform.position + Vector3.up * .5f;
                particles.Play(true); PresentationCount++;
            }
            else if (!restored && particles != null)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            wasRestored = restored;
        }
    }
}
