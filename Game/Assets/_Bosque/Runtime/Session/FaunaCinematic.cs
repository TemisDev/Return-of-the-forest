using Bosque.Player;
using Bosque.World;
using UnityEngine;

namespace Bosque.Session
{
    /// <summary>
    /// Plano de cierre: mientras el animal recorre el corredor, la cámara deja de seguir a la Guardiana y lo
    /// acompaña desde un punto de vista fijo. Al reiniciar la zona devuelve el control a la cámara de juego.
    /// </summary>
    public sealed class FaunaCinematic : MonoBehaviour
    {
        [SerializeField] private FaunaRoute route;
        [SerializeField] private ThirdPersonCamera rig;
        [SerializeField] private Transform vantage;
        [SerializeField] private Transform player;
        [SerializeField] private float blend = 1.6f;

        private Vector3 velocity, lookPoint;
        public bool Active { get; private set; }

        public void Configure(FaunaRoute fauna, ThirdPersonCamera camera, Transform view, Transform follow)
        {
            route = fauna; rig = camera; vantage = view; player = follow;
        }

        private void OnEnable() { if (route != null) route.Started += Begin; }
        private void OnDisable() { if (route != null) route.Started -= Begin; }

        public void Begin()
        {
            if (rig == null || vantage == null) return;
            Active = true; velocity = Vector3.zero;
            rig.enabled = false;
            lookPoint = rig.transform.position + rig.transform.forward * 4f;
        }

        public void Stop()
        {
            if (!Active) return;
            Active = false;
            if (rig != null) { rig.enabled = true; if (player != null) rig.Configure(player); }
        }

        private void LateUpdate()
        {
            if (!Active || route == null || route.Animal == null) return;
            var cam = rig.transform;
            cam.position = Vector3.SmoothDamp(cam.position, vantage.position, ref velocity, blend);
            var target = route.Animal.transform.position + Vector3.up * .35f;
            lookPoint = Vector3.Lerp(lookPoint, target, 1f - Mathf.Exp(-3f * Time.deltaTime));
            cam.rotation = Quaternion.LookRotation(lookPoint - cam.position);
        }
    }
}
