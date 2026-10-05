using System.Collections.Generic;
using System.Reflection;
using Lycea.Modules;
using Lycea.Robots;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lycea.EditorTools
{
    /// <summary>
    /// Checks the modules and robots actually behave, by stepping the physics by
    /// hand in an empty scene: NORA's sonar sees a wall at the right distance and
    /// her line sensor sees tape, driving forward moves her forward, and WHIP
    /// stands on his own servos. Menu: LYCEA > Self-test, or
    ///   Unity -batchmode -projectPath "Lycea" -executeMethod Lycea.EditorTools.ArenaSelfTest.Run -quit
    /// (exits with code 1 if anything failed).
    /// </summary>
    public static class ArenaSelfTest
    {
        static int failures;
        static readonly List<MonoBehaviour> stepped = new List<MonoBehaviour>();

        [MenuItem("LYCEA/Self-test")]
        public static void Run()
        {
            failures = 0;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var oldMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                Floor();
                TestNora();
                TestWhip();
            }
            finally
            {
                Physics.simulationMode = oldMode;
            }
            Debug.Log(failures == 0 ? "LYCEA self-test: ALL PASSED" : $"LYCEA self-test: {failures} FAILED");
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static void Check(bool ok, string what)
        {
            Debug.Log($"{(ok ? "PASS" : "FAIL")}  {what}");
            if (!ok) failures++;
        }

        static void Floor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.05f, 0);
            floor.transform.localScale = new Vector3(20, 0.1f, 20);
        }

        // MonoBehaviour.FixedUpdate isn't called when we step physics ourselves, so call it
        static void Step(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var mb in stepped)
                    mb.GetType().GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(mb, null);
                Physics.Simulate(Time.fixedDeltaTime);
            }
        }

        static void Awake(MonoBehaviour mb)
        {
            // edit mode doesn't run Awake either
            mb.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(mb, null);
        }

        static void TestNora()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.position = new Vector3(0, 0.15f, 1.5f);
            wall.transform.localScale = new Vector3(2, 0.3f, 0.05f);
            var tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tape.name = "Tape";
            tape.transform.position = new Vector3(0, 0.001f, 0.5f);
            tape.transform.localScale = new Vector3(0.02f, 0.002f, 2f);
            tape.GetComponent<Collider>().isTrigger = true;
            tape.AddComponent<LineMarker>();

            var nora = NoraBuilder.Build(null, Vector3.zero, Quaternion.identity);
            Awake(nora.drive);
            stepped.Add(nora.drive);
            Physics.SyncTransforms();
            Step(25);   // settle onto the floor

            float front = nora.front.Read();
            float expect = (1.5f - 0.025f - nora.front.transform.position.z) * 100f;
            Check(Mathf.Abs(front - expect) < 5f, $"NORA's front sonar sees the wall: {front:F1} cm (expected ~{expect:F1})");
            Check(nora.back.Read() < 0, $"nothing behind her reads as no echo (-1): {nora.back.Cm:F1}");
            Check(nora.lineM.Read() == 1 && nora.lineL.Read() == 0 && nora.lineR.Read() == 0,
                  $"line sensors over the tape read L/M/R = {nora.lineL.Value}{nora.lineM.Value}{nora.lineR.Value} (expected 010)");

            float z0 = nora.body.position.z;
            nora.drive.Forward(0.5f);
            Step(50);   // 1 s
            float moved = nora.body.position.z - z0;
            Check(moved > 0.15f && moved < 0.45f, $"Forward(0.5) for 1 s moves her {moved * 100:F0} cm forward (top speed 60 cm/s)");
            float drift = Mathf.Abs(nora.body.position.x);
            Check(drift < 0.03f, $"...in a straight line ({drift * 100:F1} cm sideways)");

            var x0 = nora.body.position.x;
            nora.drive.StrafeRight(0.5f);
            Step(50);
            Check(nora.body.position.x - x0 > 0.1f, $"StrafeRight moves her right ({(nora.body.position.x - x0) * 100:F0} cm)");
            nora.drive.Stop();
            stepped.Clear();
            Object.DestroyImmediate(nora.root);
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(tape);
        }

        static void TestWhip()
        {
            var whip = WhipBuilder.Build(null, new Vector3(3, 0, 0), Quaternion.identity);
            foreach (var s in whip.servos) stepped.Add(s);
            Physics.SyncTransforms();
            Step(150);   // 3 s standing still
            float height = whip.body.transform.position.y;
            Check(whip.servos.Count == 18, $"WHIP has 18 servos ({whip.servos.Count})");
            Check(height > 0.06f, $"WHIP stands on his servos: body at {height * 100:F1} cm");
            Check(Vector3.Angle(whip.body.transform.up, Vector3.up) < 15f,
                  $"...level: tilted {Vector3.Angle(whip.body.transform.up, Vector3.up):F1} deg");
            foreach (var s in whip.servos) s.Write(120);
            Step(50);
            float moved = Mathf.Abs(whip.servos[0].Angle - 90f);
            Check(moved > 10f, $"servos follow Write(): coxa 0 moved {moved:F0} deg toward 120");
            stepped.Clear();
            Object.DestroyImmediate(whip.root);
        }
    }
}
