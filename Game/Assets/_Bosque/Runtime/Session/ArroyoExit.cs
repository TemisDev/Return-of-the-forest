using UnityEngine;

namespace Bosque.Session
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ArroyoExit : MonoBehaviour
    {
        [SerializeField] private ArroyoSession session;
        private BoxCollider volume;
        public void Configure(ArroyoSession owner) => session = owner;
        private void Awake() => volume = GetComponent<BoxCollider>();
        private void Update()
        {
            if (session == null || session.Player == null || session.Progress == null ||
                !session.Progress.IsRestored || session.Progress.IsCompleted || !volume.enabled) return;
            // Query occupancy directly: CharacterController trigger notifications were not reliable
            // after teleports in the integration test. This also handles restarting inside a volume.
            var point = session.Player.transform.position + Vector3.up * .9f;
            if ((volume.ClosestPoint(point) - point).sqrMagnitude < .000001f)
                session.TryExit(session.Player);
        }
    }
}
