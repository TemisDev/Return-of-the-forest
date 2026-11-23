using UnityEngine;

namespace Bosque.ArtV6
{
    /// <summary>
    /// Efecto visual al retirar un obstáculo: cuando ArroyoSession desactiva el obstáculo,
    /// este componente (hijo visual del obstáculo) instancia una ráfaga de polvo y escombros.
    /// No decide nada de gameplay; solo reacciona a que el objeto se desactive durante el juego.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BosqueV6RemovalBurst : MonoBehaviour
    {
        public GameObject burstPrefab;
        public Vector3 offset = Vector3.zero;

        private static bool _quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetQuitFlag()
        {
            _quitting = false;
            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
        }

        private static void OnQuit() => _quitting = true;

        private void OnDisable()
        {
            if (!Application.isPlaying || _quitting || burstPrefab == null) return;
            if (!gameObject.scene.isLoaded) return;
            Instantiate(burstPrefab, transform.position + offset, Quaternion.identity);
        }
    }
}
