using System.Collections.Generic;
using Lycea.Modules;
using UnityEngine;

namespace Lycea.Arena
{
    /// <summary>
    /// A room to learn in: floor, walls, a scatter of boxes that moves every
    /// episode, and (for line following) a black tape loop on the floor.
    /// Everything is built from code, so a training scene is just a few of
    /// these side by side. 1 unit = 1 m.
    /// </summary>
    public class ArenaBuilder : MonoBehaviour
    {
        public float size = 4f;
        public float wallHeight = 0.3f;
        public int obstacles = 8;
        public bool lineLoop;
        public float lineWidth = 0.02f;   // electrical tape
        [Tooltip("No obstacles this close to the spawn point")] public float spawnClear = 0.6f;

        readonly List<GameObject> props = new List<GameObject>();
        bool built;
        Material floorMat, wallMat, boxMat, lineMat;

        public Vector3 SpawnPoint => transform.position + new Vector3(0, 0, lineLoop ? -size * 0.3f : 0);

        void Build()
        {
            if (built) return;
            built = true;
            floorMat = Mat(new Color(0.55f, 0.52f, 0.48f));
            wallMat = Mat(new Color(0.25f, 0.28f, 0.35f));
            boxMat = Mat(new Color(0.75f, 0.45f, 0.25f));
            lineMat = Mat(Color.black);
            Block("Floor", new Vector3(0, -0.05f, 0), new Vector3(size, 0.1f, size), floorMat);
            float h = size / 2f;
            Block("Wall N", new Vector3(0, wallHeight / 2, h), new Vector3(size, wallHeight, 0.05f), wallMat);
            Block("Wall S", new Vector3(0, wallHeight / 2, -h), new Vector3(size, wallHeight, 0.05f), wallMat);
            Block("Wall E", new Vector3(h, wallHeight / 2, 0), new Vector3(0.05f, wallHeight, size), wallMat);
            Block("Wall W", new Vector3(-h, wallHeight / 2, 0), new Vector3(0.05f, wallHeight, size), wallMat);
            if (lineLoop) BuildLine();
        }

        /// <summary>A new layout. Returns where the robot should start.</summary>
        public Vector3 ResetArena()
        {
            Build();
            foreach (var p in props) Destroy(p);
            props.Clear();
            float h = size / 2f - 0.25f;
            for (int i = 0, tries = 0; i < obstacles && tries < 200; tries++)
            {
                var pos = new Vector3(Random.Range(-h, h), 0, Random.Range(-h, h));
                if (Vector3.Distance(transform.position + pos, SpawnPoint) < spawnClear) continue;
                if (lineLoop && NearLine(pos)) continue;
                var s = new Vector3(Random.Range(0.08f, 0.4f), Random.Range(0.1f, wallHeight), Random.Range(0.08f, 0.4f));
                var box = Block($"Box {i}", pos + Vector3.up * s.y / 2, s, boxMat);
                box.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 90f), 0);
                props.Add(box);
                i++;
            }
            return SpawnPoint;
        }

        // an oval of tape: radius rx by rz, the robot starts on its south edge facing east
        float Rx => size * 0.32f;
        float Rz => size * 0.3f;

        void BuildLine()
        {
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2 / segments, a1 = (i + 1) * Mathf.PI * 2 / segments;
                var p0 = new Vector3(Mathf.Cos(a0) * Rx, 0, Mathf.Sin(a0) * Rz);
                var p1 = new Vector3(Mathf.Cos(a1) * Rx, 0, Mathf.Sin(a1) * Rz);
                var seg = Block($"Line {i}", (p0 + p1) / 2 + Vector3.up * 0.001f,
                                new Vector3(lineWidth, 0.002f, Vector3.Distance(p0, p1) + lineWidth), lineMat);
                seg.transform.localRotation = Quaternion.LookRotation(p1 - p0);
                seg.GetComponent<Collider>().isTrigger = true;   // tape: seen, not bumped into
                seg.AddComponent<LineMarker>();
            }
        }

        bool NearLine(Vector3 local)
        {
            float r = Mathf.Sqrt(local.x * local.x / (Rx * Rx) + local.z * local.z / (Rz * Rz));
            return Mathf.Abs(r - 1f) < 0.35f;
        }

        /// <summary>Which way the line runs at the spawn point (for line-following starts).</summary>
        public Quaternion SpawnRotation => lineLoop ? Quaternion.LookRotation(transform.right) : transform.rotation;

        GameObject Block(string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Material Mat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { color = c };
            return m;
        }
    }
}
