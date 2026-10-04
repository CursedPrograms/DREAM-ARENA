using System.Collections.Generic;
using DreamArena.Modules;
using UnityEngine;

namespace DreamArena.Robots
{
    /// <summary>
    /// WHIP from primitives: a hexapod with three servos per leg (coxa swings
    /// the leg forward/back, femur lifts it, tibia bends the foot), 18 in all,
    /// an MPU6050 in the body and an HC-SR04 on the front. Built standing:
    /// every servo at 90 is the standing pose. One ArticulationBody tree, so
    /// the joints are stiff and stable enough to learn on.
    /// Legs in order: front-left, middle-left, rear-left, front-right, middle-right, rear-right.
    /// </summary>
    public static class WhipBuilder
    {
        public class Parts
        {
            public GameObject root;
            public ArticulationBody body;
            public readonly List<ServoJoint> servos = new List<ServoJoint>();   // leg by leg: coxa, femur, tibia
            public readonly List<ArticulationBody> feet = new List<ArticulationBody>();
            public ImuSensor imu;
            public UltrasonicSensor sonar;
        }

        const float Coxa = 0.04f, Femur = 0.08f, Tibia = 0.10f;
        const float BodyW = 0.20f, BodyL = 0.30f, BodyH = 0.05f;

        public static Parts Build(Transform parent, Vector3 position, Quaternion rotation)
        {
            var p = new Parts();
            p.root = Box("WHIP", null, Vector3.zero, new Vector3(BodyW, BodyH, BodyL), new Color(0.75f, 0.3f, 0.35f));
            p.root.transform.SetParent(parent, true);
            p.root.transform.SetPositionAndRotation(position + Vector3.up * (Tibia + 0.01f), rotation);
            p.body = p.root.AddComponent<ArticulationBody>();
            p.body.mass = 1.0f;

            var imu = new GameObject("MPU6050");
            imu.transform.SetParent(p.root.transform, false);
            p.imu = imu.AddComponent<ImuSensor>();
            var sonar = new GameObject("HC-SR04");
            sonar.transform.SetParent(p.root.transform, false);
            sonar.transform.localPosition = new Vector3(0, 0, BodyL / 2 + 0.01f);
            p.sonar = sonar.AddComponent<UltrasonicSensor>();

            float[] zs = { BodyL * 0.4f, 0, -BodyL * 0.4f };
            foreach (int side in new[] { -1, 1 })
                foreach (float z in zs)
                    Leg(p, side, new Vector3(side * BodyW / 2, 0, z));
            return p;
        }

        static void Leg(Parts p, int side, Vector3 hip)
        {
            // coxa: swings around the vertical axis
            var coxa = Box("Coxa", p.root.transform, hip + new Vector3(side * Coxa / 2, 0, 0), new Vector3(Coxa, 0.02f, 0.02f), Color.gray);
            var cj = Joint(coxa, new Vector3(-side * Coxa / 2, 0, 0), Quaternion.Euler(0, 0, 90), 0.05f);
            p.servos.Add(Servo(cj, 50, 130, side < 0));
            // femur: lifts the leg (axis along the body)
            var femur = Box("Femur", coxa.transform, new Vector3(side * (Coxa / 2 + Femur / 2), 0, 0), new Vector3(Femur, 0.02f, 0.02f), Color.white);
            var fj = Joint(femur, new Vector3(-side * Femur / 2, 0, 0), Quaternion.Euler(0, -90, 0), 0.06f);
            p.servos.Add(Servo(fj, 30, 150, side > 0));
            // tibia: hangs straight down from the femur's end
            var tibia = Box("Tibia", femur.transform, new Vector3(side * Femur / 2, -Tibia / 2, 0), new Vector3(0.018f, Tibia, 0.018f), new Color(0.2f, 0.2f, 0.2f));
            var tj = Joint(tibia, new Vector3(0, Tibia / 2, 0), Quaternion.Euler(0, -90, 0), 0.05f);
            p.servos.Add(Servo(tj, 30, 150, side > 0));
            p.feet.Add(tj);
        }

        static ArticulationBody Joint(GameObject link, Vector3 anchor, Quaternion axis, float mass)
        {
            var ab = link.AddComponent<ArticulationBody>();
            ab.jointType = ArticulationJointType.RevoluteJoint;
            ab.anchorPosition = anchor;
            ab.anchorRotation = axis;   // the joint turns about the anchor's X axis
            ab.twistLock = ArticulationDofLock.LimitedMotion;
            ab.mass = mass;
            ab.jointFriction = 0.05f;
            return ab;
        }

        static ServoJoint Servo(ArticulationBody ab, float min, float max, bool invert)
        {
            var s = ab.gameObject.AddComponent<ServoJoint>();
            s.minAngle = min; s.maxAngle = max; s.rest = 90; s.invert = invert;
            s.stiffness = 60f; s.damping = 3f; s.forceLimit = 1.2f;   // ~MG996R: about 1 N m
            s.speed = 450f;
            s.Configure();
            return s;
        }

        static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            // scale the mesh child, not the link, so child links aren't stretched
            var mf = go.GetComponent<MeshFilter>();
            var vis = new GameObject("Mesh");
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = size;
            vis.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            vis.AddComponent<MeshRenderer>().sharedMaterial = NoraBuilder.Mat(c);
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(mf);
            go.GetComponent<BoxCollider>().size = size;
            return go;
        }
    }
}
