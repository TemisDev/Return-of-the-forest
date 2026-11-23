using System;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>
    /// Retorno de la fauna (GDD 2.3): el animal recorre una ruta corta predefinida hasta el refugio y se queda
    /// observando. No hay ecosistema autónomo: solo puntos de paso, giro suave y animación por velocidad.
    /// </summary>
    public sealed class FaunaRoute : MonoBehaviour
    {
        public static readonly int SpeedId = Animator.StringToHash("Speed");
        [SerializeField] private GameObject animal;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float walkSpeed = 1.05f;
        [SerializeField] private float turnDegreesPerSecond = 200f;
        [SerializeField] private float pauseAtStart = .9f;

        private int next;
        private float speed, waited;
        private readonly RaycastHit[] hits = new RaycastHit[8];

        public bool Running { get; private set; }
        public bool Arrived { get; private set; }
        public float CurrentSpeed => speed;
        /// <summary>Solo pruebas: acelera el recorrido sin cambiar la animación.</summary>
        public float TimeScale { get; set; } = 1f;
        public GameObject Animal => animal;
        public Transform[] Waypoints => waypoints;
        public event Action Started;
        public event Action ArrivedAtRefuge;

        public void Configure(GameObject body, Animator anim, Transform[] points, float walk)
        {
            animal = body; animator = anim; waypoints = points; walkSpeed = walk;
            if (animal != null) animal.SetActive(false);
        }

        public float RouteLength
        {
            get
            {
                float d = 0; if (waypoints == null) return 0;
                for (int i = 1; i < waypoints.Length; i++) d += Vector3.Distance(waypoints[i - 1].position, waypoints[i].position);
                return d;
            }
        }

        public void Begin()
        {
            if (Running || Arrived || animal == null || waypoints == null || waypoints.Length < 2) return;
            var start = waypoints[0].position;
            var look = waypoints[1].position - start; look.y = 0;
            animal.transform.SetPositionAndRotation(Ground(start), Quaternion.LookRotation(look));
            animal.SetActive(true);
            next = 1; speed = 0; waited = 0; Running = true;
            Started?.Invoke();
        }

        public void ResetRoute()
        {
            Running = false; Arrived = false; speed = 0;
            if (animal != null) animal.SetActive(false);
        }

        private void Update()
        {
            if (!Running) return;
            float dt = Time.deltaTime * TimeScale;
            if (waited < pauseAtStart) { waited += dt; SetAnim(0); return; }
            var tr = animal.transform;
            var target = waypoints[next].position;
            var to = target - tr.position; to.y = 0;
            float remaining = to.magnitude;
            for (int i = next + 1; i < waypoints.Length; i++) remaining += Vector3.Distance(waypoints[i - 1].position, waypoints[i].position);
            // Arranca y se detiene con suavidad; frena al final del recorrido.
            float desired = walkSpeed * Mathf.Clamp01(remaining / 1.2f + .15f);
            speed = Mathf.MoveTowards(speed, desired, walkSpeed * 1.5f * dt);
            bool last = next == waypoints.Length - 1;
            if (to.magnitude <= (last ? Mathf.Max(.05f, speed * dt) : .4f))
            {
                if (last) { tr.position = Ground(new Vector3(target.x, tr.position.y, target.z)); Finish(); return; }
                next++;
                return;
            }
            var dir = to.normalized;
            tr.rotation = Quaternion.RotateTowards(tr.rotation, Quaternion.LookRotation(dir), turnDegreesPerSecond * dt);
            // Avanza hacia donde mira (curvas naturales); el último tramo va directo para llegar exacto al refugio.
            var forward = last ? dir : Vector3.Slerp(tr.forward, dir, .35f); forward.y = 0; forward.Normalize();
            tr.position = Ground(tr.position + forward * Mathf.Min(speed * dt, to.magnitude));
            SetAnim(speed);
        }

        private void Finish()
        {
            Running = false; Arrived = true; speed = 0; SetAnim(0);
            ArrivedAtRefuge?.Invoke();
        }

        private void SetAnim(float v)
        {
            if (animator != null && animator.isActiveAndEnabled) animator.SetFloat(SpeedId, v);
        }

        private Vector3 Ground(Vector3 p)
        {
            int n = Physics.RaycastNonAlloc(p + Vector3.up * 1.5f, Vector3.down, hits, 4f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue, y = p.y;
            for (int i = 0; i < n; i++)
            {
                if (animal != null && hits[i].transform.IsChildOf(animal.transform)) continue;
                if (hits[i].collider.GetComponent<CharacterController>() != null) continue;
                if (hits[i].distance < best) { best = hits[i].distance; y = hits[i].point.y; }
            }
            return new Vector3(p.x, y, p.z);
        }
    }
}
