using DreamArena.Agents;
using DreamArena.Arena;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DreamArena.EditorTools
{
    /// <summary>
    /// Builds the training scenes: several arenas side by side (ML-Agents learns
    /// from all of them at once), each with its robot's agent already wired up.
    /// Menu: DREAM ARENA > Build ... , or from the command line:
    ///   Unity -batchmode -projectPath "Dream Arena" -executeMethod DreamArena.EditorTools.ArenaSetup.BuildAll -quit
    /// </summary>
    public static class ArenaSetup
    {
        const int Copies = 6;
        const float Spacing = 6f;

        [MenuItem("DREAM ARENA/Build NORA arena (obstacle avoidance)")]
        public static void NoraAvoid() => Build("NORA_Avoid", NoraAgent.Task.Avoid);

        [MenuItem("DREAM ARENA/Build NORA arena (line following)")]
        public static void NoraLine() => Build("NORA_Line", NoraAgent.Task.FollowLine);

        [MenuItem("DREAM ARENA/Build WHIP arena (walking)")]
        public static void Whip() => Build("WHIP_Walk", null);

        public static void BuildAll()
        {
            NoraAvoid();
            NoraLine();
            Whip();
            Debug.Log("DREAM ARENA: scenes built in Assets/Scenes");
        }

        static void Build(string name, NoraAgent.Task? noraTask)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(Spacing, 9f, -6f);
                cam.transform.rotation = Quaternion.Euler(55, 0, 0);
            }
            for (int i = 0; i < Copies; i++)
            {
                var root = new GameObject($"Arena {i}");
                root.transform.position = new Vector3(i % 3 * Spacing, 0, i / 3 * Spacing);
                var arena = root.AddComponent<ArenaBuilder>();
                var agentGo = new GameObject(noraTask.HasValue ? "NORA Agent" : "WHIP Agent");
                agentGo.transform.SetParent(root.transform, false);

                var bp = agentGo.AddComponent<BehaviorParameters>();
                bp.BehaviorName = noraTask.HasValue ? (noraTask == NoraAgent.Task.Avoid ? "NoraAvoid" : "NoraLine") : "WhipWalk";
                bp.BrainParameters.VectorObservationSize = noraTask.HasValue ? NoraAgent.Observations : WhipAgent.Observations;
                bp.BrainParameters.NumStackedVectorObservations = noraTask.HasValue ? 2 : 1;
                bp.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(noraTask.HasValue ? NoraAgent.Actions : WhipAgent.Actions);
                bp.BehaviorType = BehaviorType.Default;   // trains when mlagents-learn is listening, else Heuristic

                if (noraTask.HasValue)
                {
                    arena.lineLoop = noraTask == NoraAgent.Task.FollowLine;
                    arena.obstacles = arena.lineLoop ? 4 : 8;
                    var agent = agentGo.AddComponent<NoraAgent>();
                    agent.task = noraTask.Value;
                    agent.arena = arena;
                    agent.MaxStep = 3000;
                }
                else
                {
                    arena.obstacles = 0;
                    var agent = agentGo.AddComponent<WhipAgent>();
                    agent.arena = arena;
                    agent.MaxStep = 4000;
                }
                var dr = agentGo.AddComponent<DecisionRequester>();
                dr.DecisionPeriod = noraTask.HasValue ? 5 : 2;
                dr.TakeActionsBetweenDecisions = true;
            }
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{name}.unity");
        }
    }
}
