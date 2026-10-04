using DreamArena.Modules;
using UnityEngine;

namespace DreamArena.Robots
{
    /// <summary>
    /// NORA from primitives, with her real sensor layout: four HC-SR04s (front,
    /// back, left, right), the 3-channel line sensor under her nose, and four
    /// mecanum wheels. About 25 x 30 cm and 1.2 kg.
    /// </summary>
    public static class NoraBuilder
    {
        public class Parts
        {
            public GameObject root;
            public Rigidbody body;
            public MecanumDrive drive;
            public UltrasonicSensor front, right, back, left;   // NORA's DIR_FRONT/RIGHT/BACK/LEFT order
            public LineSensor lineL, lineM, lineR;
        }

        public static Parts Build(Transform parent, Vector3 position, Quaternion rotation)
        {
            var p = new Parts();
            p.root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.root.name = "NORA";
            p.root.transform.SetParent(parent, true);
            p.root.transform.SetPositionAndRotation(position + Vector3.up * 0.06f, rotation);
            p.root.transform.localScale = Vector3.one;
            Object.DestroyImmediate(p.root.GetComponent<BoxCollider>());
            var mesh = p.root.GetComponent<MeshFilter>();
            Object.DestroyImmediate(mesh.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(mesh);

            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(p.root.transform, false);
            chassis.transform.localScale = new Vector3(0.25f, 0.08f, 0.30f);
            chassis.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.39f, 0.59f, 0.9f));   // NORA's accent blue
            // the wheels have no colliders: the chassis slides, and MecanumDrive supplies the grip
            chassis.GetComponent<Collider>().sharedMaterial = new PhysicsMaterial("Slide")
                { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
            foreach (var (x, z) in new[] { (-0.14f, 0.1f), (0.14f, 0.1f), (-0.14f, -0.1f), (0.14f, -0.1f) })
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                Object.DestroyImmediate(wheel.GetComponent<Collider>());   // the chassis box does the colliding
                wheel.transform.SetParent(p.root.transform, false);
                wheel.transform.localPosition = new Vector3(x, -0.02f, z);
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
                wheel.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
                wheel.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.1f, 0.1f, 0.1f));
            }

            p.body = p.root.AddComponent<Rigidbody>();
            p.body.mass = 1.2f;
            p.body.linearDamping = 0.5f;
            p.body.angularDamping = 2f;
            p.body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            p.body.interpolation = RigidbodyInterpolation.Interpolate;
            p.drive = p.root.AddComponent<MecanumDrive>();

            p.front = Sonar(p.root, "Sonar F", new Vector3(0, 0.03f, 0.155f), 0);
            p.right = Sonar(p.root, "Sonar R", new Vector3(0.13f, 0.03f, 0), 90);
            p.back = Sonar(p.root, "Sonar B", new Vector3(0, 0.03f, -0.155f), 180);
            p.left = Sonar(p.root, "Sonar L", new Vector3(-0.13f, 0.03f, 0), -90);
            p.lineL = Line(p.root, "Line L", new Vector3(-0.02f, -0.035f, 0.13f));
            p.lineM = Line(p.root, "Line M", new Vector3(0, -0.035f, 0.13f));
            p.lineR = Line(p.root, "Line R", new Vector3(0.02f, -0.035f, 0.13f));
            return p;
        }

        static UltrasonicSensor Sonar(GameObject root, string name, Vector3 pos, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return go.AddComponent<UltrasonicSensor>();
        }

        static LineSensor Line(GameObject root, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            return go.AddComponent<LineSensor>();
        }

        internal static Material Mat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { color = c };
        }
    }
}
