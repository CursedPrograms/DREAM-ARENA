using DreamArena.Arena;
using DreamArena.Robots;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DreamArena.Agents
{
    /// <summary>
    /// NORA learning to drive. She sees exactly what the real one does - four
    /// ultrasonic distances and three line sensors - plus how she's moving, and
    /// answers with forward / strafe / turn, like her mecanum wheels can.
    ///   Avoid       roam the room without touching anything (a better Auto mode)
    ///   FollowLine  stay on the tape loop and go round it (a better Line mode)
    /// Heuristic (no model): W/S forward, A/D strafe, Q/E turn.
    /// </summary>
    public class NoraAgent : Agent
    {
        public enum Task { Avoid, FollowLine }
        public Task task = Task.Avoid;
        public ArenaBuilder arena;

        public const int Observations = 12;   // keep in step with ArenaSetup's BehaviorParameters
        public const int Actions = 3;

        NoraBuilder.Parts nora;
        bool crashed;
        int offLineSteps;
        readonly float[] last = new float[Actions];

        public override void Initialize()
        {
            if (arena == null) arena = GetComponentInParent<ArenaBuilder>();
        }

        public override void OnEpisodeBegin()
        {
            var spawn = arena.ResetArena();
            var rot = task == Task.FollowLine ? arena.SpawnRotation : Quaternion.Euler(0, Random.Range(0, 360f), 0);
            if (nora == null)
            {
                nora = NoraBuilder.Build(arena.transform, spawn, rot);
                nora.root.AddComponent<CollisionReporter>().onHit = () => crashed = true;
            }
            nora.body.linearVelocity = Vector3.zero;
            nora.body.angularVelocity = Vector3.zero;
            nora.body.position = spawn + Vector3.up * 0.06f;
            nora.body.rotation = rot;
            nora.root.transform.SetPositionAndRotation(spawn + Vector3.up * 0.06f, rot);
            nora.drive.Stop();
            crashed = false;
            offLineSteps = 0;
            System.Array.Clear(last, 0, last.Length);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            foreach (var s in new[] { nora.front, nora.right, nora.back, nora.left })
            {
                s.Read();
                sensor.AddObservation(s.Normalized);
            }
            sensor.AddObservation(nora.lineL.Read());
            sensor.AddObservation(nora.lineM.Read());
            sensor.AddObservation(nora.lineR.Read());
            var v = nora.root.transform.InverseTransformDirection(nora.body.linearVelocity) / nora.drive.maxSpeed;
            sensor.AddObservation(v.z);
            sensor.AddObservation(v.x);
            sensor.AddObservation(nora.body.angularVelocity.y / (nora.drive.maxTurn * Mathf.Deg2Rad));
            sensor.AddObservation(last[0]);
            sensor.AddObservation(last[1]);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var a = actions.ContinuousActions;
            float vx = Mathf.Clamp(a[0], -1, 1), vy = Mathf.Clamp(a[1], -1, 1), w = Mathf.Clamp(a[2], -1, 1);
            nora.drive.Drive(vx, vy, w);
            last[0] = vx; last[1] = vy; last[2] = w;

            if (crashed)
            {
                SetReward(-1f);
                EndEpisode();
                return;
            }
            var v = nora.root.transform.InverseTransformDirection(nora.body.linearVelocity) / nora.drive.maxSpeed;
            AddReward(-0.0005f * (w * w + vy * vy));   // smooth driving
            if (task == Task.Avoid)
            {
                AddReward(0.01f * Mathf.Max(0, v.z));     // going somewhere, forwards
                float closest = Mathf.Min(Mathf.Min(nora.front.Normalized, nora.left.Normalized),
                                          Mathf.Min(nora.right.Normalized, nora.back.Normalized));
                if (closest < 0.05f) AddReward(-0.005f);   // under 20 cm: too close
            }
            else
            {
                bool on = nora.lineL.Value + nora.lineM.Value + nora.lineR.Value > 0;
                if (on)
                {
                    offLineSteps = 0;
                    AddReward(0.01f * Mathf.Max(0, v.z) + (nora.lineM.Value == 1 ? 0.002f : 0));
                }
                else if (++offLineSteps > 60)
                {
                    SetReward(-0.5f);
                    EndEpisode();
                }
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var a = actionsOut.ContinuousActions;
            a[0] = Key(KeyCode.W) - Key(KeyCode.S);
            a[1] = Key(KeyCode.D) - Key(KeyCode.A);
            a[2] = Key(KeyCode.E) - Key(KeyCode.Q);
        }

        static float Key(KeyCode k)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(k) ? 1f : 0f;
#else
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return 0f;
            return k switch
            {
                KeyCode.W => kb.wKey.isPressed ? 1 : 0, KeyCode.S => kb.sKey.isPressed ? 1 : 0,
                KeyCode.A => kb.aKey.isPressed ? 1 : 0, KeyCode.D => kb.dKey.isPressed ? 1 : 0,
                KeyCode.Q => kb.qKey.isPressed ? 1 : 0, KeyCode.E => kb.eKey.isPressed ? 1 : 0,
                _ => 0f,
            };
#endif
        }
    }
}
