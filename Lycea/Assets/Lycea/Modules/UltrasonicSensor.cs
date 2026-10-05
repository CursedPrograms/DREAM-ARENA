using UnityEngine;

namespace Lycea.Modules
{
    /// <summary>
    /// HC-SR04 ultrasonic sensor, as the robots read it: distance in cm along
    /// the sensor's forward axis, or -1 for "no echo" (nothing within range,
    /// or the beam glanced off at an angle) - the same convention as NORA's
    /// and WHIP's firmware, so code written against this works on them.
    /// The beam is a ~15 degree cone, sampled with a few rays.
    /// </summary>
    public class UltrasonicSensor : MonoBehaviour
    {
        [Tooltip("HC-SR04: 2 cm to 400 cm")] public float minCm = 2f, maxCm = 400f;
        [Tooltip("Half-angle of the beam")] public float coneDegrees = 7.5f;
        [Tooltip("Reading noise, cm (1 sigma)")] public float noiseCm = 0.5f;
        [Tooltip("Surfaces hit at more than this angle reflect the echo away")] public float maxIncidence = 60f;
        public LayerMask mask = ~0;

        /// <summary>The last reading, cm (-1 = no echo).</summary>
        public float Cm { get; private set; } = -1f;

        static readonly Vector2[] Pattern = { Vector2.zero, Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        /// <summary>Ping once. Units: 1 Unity unit = 1 m.</summary>
        public float Read()
        {
            float best = float.MaxValue;
            foreach (var p in Pattern)
            {
                var dir = Quaternion.AngleAxis(p.x * coneDegrees, transform.up) *
                          Quaternion.AngleAxis(p.y * coneDegrees, transform.right) * transform.forward;
                if (Physics.Raycast(transform.position, dir, out var hit, maxCm / 100f, mask, QueryTriggerInteraction.Ignore)
                    && Vector3.Angle(-dir, hit.normal) <= maxIncidence)
                    best = Mathf.Min(best, hit.distance * 100f);
            }
            if (best == float.MaxValue || best < minCm) return Cm = -1f;
            return Cm = Mathf.Max(minCm, best + Gaussian() * noiseCm);
        }

        /// <summary>0 (touching) .. 1 (nothing in range): the reading as a network input.</summary>
        public float Normalized => Cm < 0 ? 1f : Mathf.Clamp01(Cm / maxCm);

        static float Gaussian()
        {
            float u = 1f - Random.value, v = Random.value;
            return Mathf.Sqrt(-2f * Mathf.Log(u)) * Mathf.Cos(2f * Mathf.PI * v);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Cm < 0 ? new Color(0.3f, 0.6f, 1f, 0.5f) : Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * (Cm < 0 ? maxCm : Cm) / 100f);
        }
    }
}
