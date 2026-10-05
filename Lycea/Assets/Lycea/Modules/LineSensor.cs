using UnityEngine;

namespace Lycea.Modules
{
    /// <summary>
    /// One channel of a line tracking sensor (NORA has three: L / M / R on GPIO
    /// 34 / 35 / 39): 1 over the line, 0 over the floor, read straight down.
    /// Anything whose collider carries a LineMarker counts as line.
    /// </summary>
    public class LineSensor : MonoBehaviour
    {
        [Tooltip("How far below the sensor it can see, m")] public float range = 0.05f;

        public int Value { get; private set; }

        public int Read()
        {
            Value = Physics.Raycast(transform.position, -transform.up, out var hit, range, ~0, QueryTriggerInteraction.Collide)
                    && hit.collider.GetComponent<LineMarker>() != null ? 1 : 0;
            return Value;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Value == 1 ? Color.black : Color.white;
            Gizmos.DrawRay(transform.position, -transform.up * range);
        }
    }
}
