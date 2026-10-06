# LYCEA

The fleet's school, named after the Lyceum - Aristotle's school, which had its own gymnasium: learning and training in one place. A Unity project where the DREAM Robotics robots learn in simulation before trying it for real. It's built from **module scripts** that behave like the real parts (ultrasonic sensors, line sensors, servos, mecanum wheels, an IMU), and uses **Unity ML-Agents** to teach the robots: WHIP to walk, and NORA to drive without hitting things and to follow a line.

> **Status:** compiles in Unity 6000.6.2f1 with ML-Agents 4.1.0 (4.0.0 doesn't build on Unity 6.6). The three training scenes are built, and **LYCEA › Self-test** passes: NORA's sonar reads a wall at the right distance, her line sensors see tape, she drives and strafes straight, and WHIP stands level on his 18 servos and follows them. Training itself hasn't been run yet.

---

## What's in it

### Modules (`Assets/Lycea/Modules`)
| Script | Real part | Behaves like |
| :--- | :--- | :--- |
| `UltrasonicSensor` | HC-SR04 | 2–400 cm, a ~15° cone, -1 for "no echo" (the firmware's convention), angled surfaces lose the echo, a little noise |
| `LineSensor` + `LineMarker` | 3-channel line tracker | 1 over tape, 0 over floor |
| `MecanumDrive` | 4 mecanum wheels + 2× L298N | per-wheel commands -1..1 and NORA's `Forward/Strafe/Turn` helpers; the body moves by mecanum kinematics |
| `ServoJoint` | hobby servo (MG996R) | `Write(degrees)` like `Servo.write()`, speed- and torque-limited |
| `ImuSensor` | MPU6050 | pitch, roll, tilt, gyro, body-frame velocity |

### Robots (`Assets/Lycea/Robots`), built from primitives
- **NORA**: 25 × 30 cm, 1.2 kg, sensors where the real ones are: four HC-SR04s (front, right, back, left) and the line sensor under her nose.
- **WHIP**: a hexapod with 18 servos (coxa, femur, tibia per leg) as one ArticulationBody tree, the MPU6050 in the body and an HC-SR04 at the front. Built standing: every servo at 90°.

### Arena (`Assets/Lycea/Arena/ArenaBuilder`)
A 4 × 4 m room, walls, boxes that move every episode, and optionally a black-tape oval for line following.

### Agents (`Assets/Lycea/Agents`)
| Behaviour | Robot | Sees | Does | Learns |
| :--- | :--- | :--- | :--- | :--- |
| `NoraAvoid` | NORA | 4 distances, 3 line sensors, her motion | forward / strafe / turn | roam without touching anything (a better Auto mode) |
| `NoraLine` | NORA | the same | the same | go round the tape loop (a better Line mode) |
| `WhipWalk` | WHIP | tilt, gyro, motion, 18 servo angles, distance ahead | 18 servo targets | walk forwards, staying upright, without thrashing |

Without a trained model the agents run their **heuristic**: NORA drives with W/S/A/D/Q/E, and WHIP plays a simple tripod gait.

## Training

1. In Unity: **LYCEA › Build NORA arena (obstacle avoidance)** (or line following, or WHIP). This builds a scene with six arenas side by side in `Assets/Scenes`.
2. Run `train.bat NoraAvoid` (or `NoraLine`, `WhipWalk`). The first run uses [uv](https://docs.astral.sh/uv/) to set up Python 3.10 and `mlagents` 1.1.0 in `venv\`.
3. When it says it's listening, press **Play** in Unity.
4. The model lands in `results\<name>\<name>.onnx`. Drop it on the agent's **Behavior Parameters › Model**. Running `train.bat` again resumes where it stopped.

Training settings are in `config/*.yaml` (PPO).

## Next

- [Unity Robotics Hub](https://github.com/Unity-Technologies/Unity-Robotics-Hub)'s **URDF Importer**, to bring in exact models (the ARM's 6-DOF frame, for instance) instead of primitives.
- Run a trained NORA policy on the real NORA: the inputs are her real sensor readings, so the `.onnx` could run on the PC and drive her over WiFi.

<br>
<div align="center">
© Cursed Entertainment 2026
</div>
<br>
<div align="center">
<a href="https://cursed-entertainment.itch.io/" target="_blank">
    <img src="https://github.com/CursedPrograms/cursedentertainment/raw/main/images/logos/logo-wide-grey.png"
        alt="CursedEntertainment Logo" style="width:250px;">
</a>
</div>
<br>
<div align="center">
  <a href="https://github.com/SynthWomb" target="_blank">
    <img src="https://github.com/SynthWomb/synth.womb/blob/main/logos/synthwomb07.png" alt="SynthWomb" style="width:200px;"/>
  </a>
</div>

---

<!-- DREAM-ECOSYSTEM:START -->
## The DREAM Robotics ecosystem

**Minds** — [DREAM](https://github.com/CursedPrograms/DREAM) companion · [TINA](https://github.com/CursedPrograms/TINA) engineer · [NINA](https://github.com/CursedPrograms/NINA) caretaker · [RIFT](https://github.com/CursedPrograms/RIFT) fleet hub · [LYCEA](https://github.com/CursedPrograms/LYCEA) training school  
**Robots** — [NORA](https://github.com/CursedPrograms/NORA-Robot-v00) · [MILA](https://github.com/CursedPrograms/MILA-Robot-v00) · [WHIP](https://github.com/CursedPrograms/WHIP-Robot-v00) · [KIDA-00](https://github.com/CursedPrograms/KIDA-Robot-v00) · [KIDA-01](https://github.com/CursedPrograms/KIDA-Robot-v01) · [IDA](https://github.com/CursedPrograms/IDA-Robot-v00) · [ARM](https://github.com/CursedPrograms/ARM-Robot-v01)  
**Tools** — [CRUSH](https://github.com/CursedPrograms/CRUSH) compression · [Image-Generator](https://github.com/CursedPrograms/Image-Generator) · [GloriosaAI](https://github.com/CursedPrograms/GloriosaAI) · [SynthWomb](https://github.com/CursedPrograms/SynthWomb) · [PyVitals](https://github.com/CursedPrograms/PyVitals)  
<!-- DREAM-ECOSYSTEM:END -->
