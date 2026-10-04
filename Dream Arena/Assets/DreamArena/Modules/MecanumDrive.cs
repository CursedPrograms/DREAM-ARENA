using UnityEngine;

namespace DreamArena.Modules
{
    /// <summary>
    /// Four mecanum wheels on a Rigidbody (NORA's chassis). Each wheel takes a
    /// command -1..1 like an L298N channel (direction + PWM); the body moves the
    /// way mecanum kinematics says those four wheels push it. The same
    /// Forward / Strafe / Turn helpers as NORA's firmware, plus Drive(vx, vy, w)
    /// for a learning agent.
    /// Wheel order is NORA's: M0 front-left, M1 front-right, M2 rear-left, M3 rear-right.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MecanumDrive : MonoBehaviour
    {
        [Tooltip("Top speed with every wheel at 1, m/s")] public float maxSpeed = 0.6f;
        [Tooltip("Top turn rate, deg/s")] public float maxTurn = 180f;
        [Tooltip("How hard the wheels pull toward the commanded speed")] public float grip = 12f;
        [Range(0, 1)] public float speedScale = 1f;   // NORA's speed slider

        public readonly float[] wheels = new float[4];
        Rigidbody body;

        void Awake() => body = GetComponent<Rigidbody>();

        public void SetWheels(float m0, float m1, float m2, float m3)
        {
            wheels[0] = Mathf.Clamp(m0, -1, 1); wheels[1] = Mathf.Clamp(m1, -1, 1);
            wheels[2] = Mathf.Clamp(m2, -1, 1); wheels[3] = Mathf.Clamp(m3, -1, 1);
        }

        /// <summary>vx forward, vy right, w turn right, each -1..1 -> the four wheels.</summary>
        public void Drive(float vx, float vy, float w)
        {
            float fl = vx + vy + w, fr = vx - vy - w, rl = vx - vy + w, rr = vx + vy - w;
            float m = Mathf.Max(1f, Mathf.Abs(fl), Mathf.Abs(fr), Mathf.Abs(rl), Mathf.Abs(rr));
            SetWheels(fl / m, fr / m, rl / m, rr / m);
        }

        public void Forward(float s = 1) => Drive(s, 0, 0);
        public void Backward(float s = 1) => Drive(-s, 0, 0);
        public void StrafeLeft(float s = 1) => Drive(0, -s, 0);
        public void StrafeRight(float s = 1) => Drive(0, s, 0);
        public void TurnLeft(float s = 1) => Drive(0, 0, -s);
        public void TurnRight(float s = 1) => Drive(0, 0, s);
        public void Stop() => SetWheels(0, 0, 0, 0);

        void FixedUpdate()
        {
            // inverse mecanum kinematics: what the four wheels add up to
            float vx = (wheels[0] + wheels[1] + wheels[2] + wheels[3]) / 4f;
            float vy = (wheels[0] - wheels[1] - wheels[2] + wheels[3]) / 4f;
            float w = (wheels[0] - wheels[1] + wheels[2] - wheels[3]) / 4f;
            float s = speedScale;
            var want = transform.TransformDirection(new Vector3(vy, 0, vx)) * (maxSpeed * s);
            var have = body.linearVelocity;
            var pull = new Vector3(want.x - have.x, 0, want.z - have.z) * grip;
            body.AddForce(pull, ForceMode.Acceleration);
            float wantW = w * maxTurn * s * Mathf.Deg2Rad;
            body.AddTorque(Vector3.up * (wantW - body.angularVelocity.y) * grip, ForceMode.Acceleration);
        }
    }
}
