using System;
using System.Collections;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>
    /// Lecho donde la habilidad de crecimiento puede actuar. Solo prospera si el suelo es adecuado (húmedo);
    /// si no, devuelve una pista y puede volver a intentarse en otro sitio. Crece una sola vez por reinicio.
    /// Reutilizable en cualquier zona: no conoce la sesión que lo usa.
    /// </summary>
    public sealed class GrowthPoint : MonoBehaviour
    {
        [SerializeField] private string pointId;
        [SerializeField, Tooltip("Suelo con humedad: aquí sí crece la planta nativa.")] private bool suitable = true;
        [SerializeField, TextArea] private string hint = "El suelo está seco y agrietado. Busca tierra oscura y húmeda.";
        [SerializeField] private GameObject plant;
        [SerializeField] private float growSeconds = 1.6f;
        [SerializeField] private string prompt = "Hacer crecer";

        private Coroutine growing;
        public string PointId => pointId;
        public bool Suitable => suitable;
        public bool Grown { get; private set; }
        public string Hint => hint;
        public string Prompt => prompt;
        public GameObject Plant => plant;
        /// <summary>Crecimiento aceptado (una vez por reinicio).</summary>
        public event Action<GrowthPoint> Grew;

        public void Configure(string id, bool isSuitable, string failureHint, GameObject plantRoot)
        {
            pointId = id; suitable = isSuitable; hint = failureHint; plant = plantRoot;
            if (plant != null) plant.SetActive(false);
        }

        /// <summary>Intenta hacer crecer la planta. Devuelve false si ya creció o si el suelo no sirve.</summary>
        public bool TryGrow()
        {
            if (Grown || !suitable || !isActiveAndEnabled) return false;
            Grown = true;
            if (plant != null)
            {
                plant.SetActive(true);
                if (growing != null) StopCoroutine(growing);
                growing = StartCoroutine(GrowthReveal.Animate(plant.transform, growSeconds));
            }
            Grew?.Invoke(this);
            return true;
        }

        /// <summary>Vuelve al estado inicial (reinicio de la zona).</summary>
        public void ResetPoint()
        {
            if (growing != null) { StopCoroutine(growing); growing = null; }
            Grown = false;
            if (plant != null) { plant.transform.localScale = Vector3.one; plant.SetActive(false); }
        }
    }

    /// <summary>Animación de crecimiento: escala desde casi cero con un pequeño rebote final.</summary>
    public static class GrowthReveal
    {
        public static IEnumerator Animate(Transform target, float seconds)
        {
            var full = Vector3.one;
            float t = 0f;
            target.localScale = full * .02f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float eased = 1f - Mathf.Pow(1f - k, 3f);
                float overshoot = 1f + .06f * Mathf.Sin(k * Mathf.PI);
                target.localScale = full * Mathf.Max(.02f, eased * overshoot);
                yield return null;
            }
            target.localScale = full;
        }
    }
}
