using Bosque.ArtV6;
using Bosque.Session;
using UnityEngine;

namespace Bosque.Levels
{
    /// <summary>
    /// Une la zona 4 con el ambiente V6 (que vive en el ensamblado por defecto): cada sector limpio aclara un poco
    /// el filtro degradado (cielo, niebla, color) antes de la recuperación completa del valle.
    /// </summary>
    public sealed class ValleAmbienceLink : MonoBehaviour
    {
        [SerializeField] private BosqueV6Ambience ambience;
        [SerializeField] private ValleSession session;
        [SerializeField, Range(0f, .25f)] private float perSector = .2f;

        public void Configure(BosqueV6Ambience amb, ValleSession owner) { ambience = amb; session = owner; }

        private void Update()
        {
            if (ambience == null || session == null) return;
            ambience.partial = session.IsRestored ? 1f : session.SectorsClean * perSector;
        }
    }
}
