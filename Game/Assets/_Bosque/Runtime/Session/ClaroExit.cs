using UnityEngine;

namespace Bosque.Session
{
    /// <summary>Volumen de salida del claro (misma consulta directa de ocupación que la salida del arroyo).</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ClaroExit : MonoBehaviour
    {
        [SerializeField] private ClaroSession session;
        private BoxCollider volume;
        public void Configure(ClaroSession owner) => session = owner;
        private void Awake() => volume = GetComponent<BoxCollider>();
        private void Update()
        {
            if (session == null || session.Player == null || session.Progress == null ||
                !session.Progress.IsRestored || session.Progress.IsCompleted || !volume.enabled) return;
            var point = session.Player.transform.position + Vector3.up * .9f;
            if ((volume.ClosestPoint(point) - point).sqrMagnitude < .000001f) session.TryExit(session.Player);
        }
    }
}
