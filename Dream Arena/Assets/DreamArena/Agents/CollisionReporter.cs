using UnityEngine;

namespace DreamArena.Agents
{
    /// <summary>Calls onHit when this body touches something: anything but the floor
    /// (NORA: walls, boxes), or the floor too (WHIP's belly).</summary>
    public class CollisionReporter : MonoBehaviour
    {
        public System.Action onHit;
        public bool ignoreFloor = true;

        void OnCollisionEnter(Collision c)
        {
            if (ignoreFloor && c.gameObject.name == "Floor") return;
            onHit?.Invoke();
        }
    }
}
