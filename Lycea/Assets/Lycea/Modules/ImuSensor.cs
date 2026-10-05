using UnityEngine;

namespace Lycea.Modules
{
    /// <summary>
    /// MPU6050 (WHIP watches it for tipping): pitch and roll in degrees, the
    /// gyro's turn rates, and "up" in the body's own frame for a network.
    /// Reads whichever ArticulationBody or Rigidbody it sits on.
    /// </summary>
    public class ImuSensor : MonoBehaviour
    {
        ArticulationBody ab;
        Rigidbody rb;

        void Awake()
        {
            ab = GetComponentInParent<ArticulationBody>();
            rb = GetComponentInParent<Rigidbody>();
        }

        public float Pitch => Mathf.DeltaAngle(0, transform.eulerAngles.x);
        public float Roll => Mathf.DeltaAngle(0, transform.eulerAngles.z);
        public float Tilt => Vector3.Angle(transform.up, Vector3.up);

        /// <summary>World up, seen from the body (0,1,0 when level).</summary>
        public Vector3 UpLocal => transform.InverseTransformDirection(Vector3.up);

        /// <summary>Gyro, deg/s in the body frame.</summary>
        public Vector3 Gyro
        {
            get
            {
                var w = ab != null ? ab.angularVelocity : rb != null ? rb.angularVelocity : Vector3.zero;
                return transform.InverseTransformDirection(w) * Mathf.Rad2Deg;
            }
        }

        /// <summary>Velocity in the body frame, m/s.</summary>
        public Vector3 VelocityLocal
        {
            get
            {
                var v = ab != null ? ab.linearVelocity : rb != null ? rb.linearVelocity : Vector3.zero;
                return transform.InverseTransformDirection(v);
            }
        }
    }
}
