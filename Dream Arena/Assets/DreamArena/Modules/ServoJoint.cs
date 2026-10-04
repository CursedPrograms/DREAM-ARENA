using UnityEngine;

namespace DreamArena.Modules
{
    /// <summary>
    /// A hobby servo on an ArticulationBody revolute joint: you set an angle in
    /// the servo's own degrees (0..range, e.g. 0-180 for WHIP's legs, 0-270 for
    /// the ARM, with the centre at range/2), and it moves there no faster than a
    /// real servo would (~60 degrees per 0.12 s for an MG996R) with limited torque.
    /// </summary>
    [RequireComponent(typeof(ArticulationBody))]
    public class ServoJoint : MonoBehaviour
    {
        public float range = 180f;
        [Tooltip("Servo degrees of the joint's rest position")] public float rest = 90f;
        public float minAngle = 0f, maxAngle = 180f;
        [Tooltip("deg/s")] public float speed = 500f;
        public float stiffness = 4000f, damping = 80f, forceLimit = 60f;
        public bool invert;

        public float Target { get; private set; }
        float commanded;
        ArticulationBody ab;

        void Awake() => Configure();

        /// <summary>Apply the fields to the joint (call again after changing them from code).</summary>
        public void Configure()
        {
            ab = GetComponent<ArticulationBody>();
            Target = commanded = rest;
            var d = ab.xDrive;
            d.stiffness = stiffness; d.damping = damping; d.forceLimit = forceLimit;
            d.lowerLimit = JointDegrees(minAngle); d.upperLimit = JointDegrees(maxAngle);
            if (d.lowerLimit > d.upperLimit) (d.lowerLimit, d.upperLimit) = (d.upperLimit, d.lowerLimit);
            d.target = JointDegrees(rest);
            ab.xDrive = d;
        }

        float JointDegrees(float servo) => (servo - rest) * (invert ? -1 : 1);

        /// <summary>Servo angle, like Servo.write() / the PCA9685 sketches.</summary>
        public void Write(float servoDegrees) => commanded = Mathf.Clamp(servoDegrees, minAngle, maxAngle);

        /// <summary>-1..1 around rest, for a learning agent.</summary>
        public void WriteNormalized(float v) =>
            Write(v >= 0 ? Mathf.Lerp(rest, maxAngle, v) : Mathf.Lerp(rest, minAngle, -v));

        /// <summary>Where the joint actually is, servo degrees.</summary>
        public float Angle => rest + (ab.jointPosition.dofCount > 0 ? ab.jointPosition[0] * Mathf.Rad2Deg : 0f) * (invert ? -1 : 1);

        public float AngleNormalized => Angle >= rest ? (Angle - rest) / Mathf.Max(1e-3f, maxAngle - rest)
                                                      : -(rest - Angle) / Mathf.Max(1e-3f, rest - minAngle);

        void FixedUpdate()
        {
            Target = Mathf.MoveTowards(Target, commanded, speed * Time.fixedDeltaTime);
            var d = ab.xDrive;
            d.target = JointDegrees(Target);
            ab.xDrive = d;
        }
    }
}
