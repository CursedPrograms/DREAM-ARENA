using DreamArena.Arena;
using DreamArena.Robots;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DreamArena.Agents
{
    /// <summary>
    /// WHIP learning to walk. The real WHIP plays pre-tuned gait poses; this one
    /// starts standing and has to find a gait itself. It feels what the real
    /// one can (the MPU6050's tilt and gyro, the HC-SR04 ahead, where each servo
    /// is) and sets all 18 servos every decision. Rewarded for moving forward
    /// while staying upright, gently; the episode ends if it tips past 60 degrees
    /// or its belly hits the floor.
    /// </summary>
    public class WhipAgent : Agent
    {
        public ArenaBuilder arena;

        public const int Observations = 3 + 3 + 3 + 18 + 1;   // up, gyro, velocity, servos, sonar
        public const int Actions = 18;

        WhipBuilder.Parts whip;
        bool bellyDown;

        public override void Initialize()
        {
            if (arena == null) arena = GetComponentInParent<ArenaBuilder>();
        }

        public override void OnEpisodeBegin()
        {
            var spawn = arena.ResetArena();
            // articulations don't teleport cleanly with their joints bent, so start each walk with a fresh WHIP
            if (whip != null) Destroy(whip.root);
            whip = WhipBuilder.Build(arena.transform, spawn, Quaternion.Euler(0, Random.Range(0, 360f), 0));
            var belly = whip.root.AddComponent<CollisionReporter>();
            belly.ignoreFloor = false;
            belly.onHit = () => bellyDown = true;
            bellyDown = false;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (whip == null)
            {
                for (int i = 0; i < Observations; i++) sensor.AddObservation(0f);
                return;
            }
            sensor.AddObservation(whip.imu.UpLocal);
            sensor.AddObservation(whip.imu.Gyro / 360f);
            sensor.AddObservation(whip.imu.VelocityLocal);
            foreach (var s in whip.servos) sensor.AddObservation(s.AngleNormalized);
            whip.sonar.Read();
            sensor.AddObservation(whip.sonar.Normalized);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (whip == null) return;
            var a = actions.ContinuousActions;
            float effort = 0;
            for (int i = 0; i < Actions; i++)
            {
                float v = Mathf.Clamp(a[i], -1, 1);
                whip.servos[i].WriteNormalized(v);
                effort += v * v;
            }
            if (bellyDown || whip.imu.Tilt > 60f)
            {
                SetReward(-1f);
                EndEpisode();
                return;
            }
            var vel = whip.imu.VelocityLocal;
            AddReward(0.05f * vel.z                      // forward
                      - 0.02f * Mathf.Abs(vel.x)         // not crabwise
                      + 0.005f * whip.imu.UpLocal.y      // upright
                      - 0.0005f * effort);               // without thrashing
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // a simple tripod gait, so you can see the legs move without a model
            var a = actionsOut.ContinuousActions;
            float t = Time.time * 4f;
            for (int leg = 0; leg < 6; leg++)
            {
                float phase = (leg % 2 == (leg < 3 ? 0 : 1)) ? 0 : Mathf.PI;
                a[leg * 3] = 0.5f * Mathf.Sin(t + phase);
                a[leg * 3 + 1] = 0.4f * Mathf.Max(0, Mathf.Cos(t + phase));
                a[leg * 3 + 2] = 0;
            }
        }
    }
}
