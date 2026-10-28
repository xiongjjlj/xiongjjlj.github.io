using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

// Only install in the task's isolated copy. Never install into a live game.
[BepInPlugin("local.codex.boomerang.nativeprobe", "Native parity probe", "0.1.0")]
public sealed class NativeProbe : BaseUnityPlugin {
    [Serializable] public class Report {
        public string unity, gameDirectory, error = "";
        public int processId;
        public List<RaySample> rays = new List<RaySample>();
        public List<RotationSample> rotations = new List<RotationSample>();
        public List<string> oracles = new List<string>();
    }
    [Serializable] public class RaySample {
        public string name, kind;
        public float[] direction, point, normal, centroid;
        public float requestedDistance, distance, fraction;
        public bool hit;
    }
    [Serializable] public class RotationSample {
        public float angle;
        public float[] quaternion, input, output;
    }
    static float[] V(Vector2 v) => new[] { v.x, v.y };
    static string J(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + "\"";
    static string N(float f) => f.ToString("R", CultureInfo.InvariantCulture);
    static string A(float[] a) => "[" + string.Join(",", Array.ConvertAll(a, N)) + "]";
    static string Serialize(Report r) {
        // JsonUtility omits the dynamically loaded plugin's generic list fields
        // in this player; serialize explicitly so missing cases cannot look valid.
        var rays = r.rays.ConvertAll(x => "{\"name\":" + J(x.name) + ",\"kind\":" + J(x.kind) +
            ",\"direction\":" + A(x.direction) + ",\"requestedDistance\":" + N(x.requestedDistance) +
            ",\"hit\":" + (x.hit ? "true" : "false") + ",\"distance\":" + N(x.distance) +
            ",\"fraction\":" + N(x.fraction) + ",\"point\":" + A(x.point) +
            ",\"normal\":" + A(x.normal) + ",\"centroid\":" + A(x.centroid) + "}");
        var rotations = r.rotations.ConvertAll(x => "{\"angle\":" + N(x.angle) +
            ",\"quaternion\":" + A(x.quaternion) + ",\"input\":" + A(x.input) + ",\"output\":" + A(x.output) + "}");
        return "{\"unity\":" + J(r.unity) + ",\"gameDirectory\":" + J(r.gameDirectory) +
            ",\"error\":" + J(r.error) + ",\"processId\":" + r.processId +
            ",\"rays\":[" + string.Join(",", rays) + "],\"rotations\":[" + string.Join(",", rotations) +
            "],\"oracles\":[" + string.Join(",", r.oracles.ConvertAll(J)) + "]}";
    }
    void Awake() {
        if (Path.GetFileName(Paths.GameRootPath) != "codex-boomerang-native-20261027") {
            Logger.LogError("Native probe refuses to run outside its isolated copy.");
            return;
        }
        var report = new Report { unity = Application.unityVersion, gameDirectory = Paths.GameRootPath,
            processId = System.Diagnostics.Process.GetCurrentProcess().Id };
        string coroutineOracle = Environment.GetEnvironmentVariable("BOOMERANG_NATIVE_ORACLE");
        if (coroutineOracle == "SceneVisualOracle.Native.dll" || coroutineOracle == "PlayerAiButtonsOracle.Native.dll" || coroutineOracle == "PlayerAiLoopOracle.Native.dll") {
            StartCoroutine(RunCoroutineOracle(report,coroutineOracle));
            return;
        }
        try {
            var scene = SceneManager.CreateScene("CodexNativeProbe", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            var physics = scene.GetPhysicsScene2D();
            var obstacle = new GameObject("Probe obstacle");
            SceneManager.MoveGameObjectToScene(obstacle, scene);
            obstacle.layer = 8;
            obstacle.transform.position = new Vector3(0, 2.05f, 0);
            obstacle.AddComponent<BoxCollider2D>().size = new Vector2(1, .02f);
            Physics2D.queriesStartInColliders = false;
            Physics2D.SyncTransforms();
            physics.Simulate(.016666f);
            var spatial = new Vector3(0, 2, .75f);
            var directions = new[] { Vector2.up, Vector2.up * .5f, Vector2.up * 2,
                (Vector2)spatial.normalized, Vector2.up * 1e-7f, Vector2.zero };
            var names = new[] { "unit", "half", "double", "spatial", "tiny", "zero" };
            for (int i = 0; i < directions.Length; ++i) {
                var d = directions[i];
                foreach (bool circle in new[] { false, true }) {
                    var hit = circle ? physics.CircleCast(Vector2.zero, .1f, d, spatial.magnitude, 1 << 8)
                                     : physics.Raycast(Vector2.zero, d, spatial.magnitude, 1 << 8);
                    report.rays.Add(new RaySample { name = names[i], kind = circle ? "circle" : "ray",
                        direction = V(d), requestedDistance = spatial.magnitude, hit = hit.collider != null,
                        distance = hit.distance, fraction = hit.fraction, point = V(hit.point),
                        normal = V(hit.normal), centroid = V(hit.centroid) });
                }
            }
            foreach (float angle in new[] { -180f, -90f, -45f, 0f, 45f, 90f, 180f, 23.456f }) {
                var q = Quaternion.AngleAxis(angle, Vector3.forward);
                foreach (var input in new[] { new Vector2(0, -20), new Vector2(-13.7f, 25.15f) }) {
                    report.rotations.Add(new RotationSample { angle = angle, input = V(input),
                        quaternion = new[] { q.x, q.y, q.z, q.w }, output = V(q * input) });
                }
            }
            string oraclePath = Path.Combine(Paths.GameRootPath, "NativeOracles", "DiscOracle.Native.dll");
            if (File.Exists(oraclePath)) {
                var assembly = System.Reflection.Assembly.LoadFrom(oraclePath);
                var run = assembly.GetType("OracleEntry", true).GetMethod("Run");
                foreach (string mode in new[] { "math3", "parry", "height", "clash" }) {
                    string file = "disc-native-" + mode + ".json";
                    run.Invoke(null, new object[] { new[] { Path.Combine(Paths.GameRootPath,file), mode } });
                    report.oracles.Add(file);
                }
            }
            RunOracle(report,"ActorOracle.Native.dll","actor-native",new[] { "actor", "melee" });
            RunOracle(report,"TimekeeperOracle.Native.dll","timekeeper-native",new[] { "clock" });
            RunOracle(report,"PowerOracle.Native.dll","power-native",new[] { "lifecycle" });
            RunOracle(report,"CameraOracle.Native.dll","camera-native",new[] { "framing", "tracking" });
            RunOracle(report,"GroundOracle.Native.dll","ground-native",new[] { "queries" });
            RunOracle(report,"MovingFloorOracle.Native.dll","moving-floor-native",new[] { "motion" });
            RunOracle(report,"FallOracle.Native.dll","fall-native",new[] { "lifecycle" });
            RunOracle(report,"DashOracle.Native.dll","dash-native",new[] { "walls" });
            RunOracle(report,"TimeimmuneOracle.Native.dll","timeimmune-native",new[] { "lifecycle" });
            RunOracle(report,"MultidiscOracle.Native.dll","multidisc-native",new[] { "lifecycle" });
            RunOracle(report,"ControlledOracle.Native.dll","controlled-native",new[] { "lifecycle" });
            RunOracle(report,"TeleportOracle.Native.dll","teleport-native",new[] { "lifecycle" });
            RunOracle(report,"VisualOracle.Native.dll","visual-native",new[] { "capabilities" });
            RunOracle(report,"ShaderOracle.Native.dll","shader-native",new[] { "disassembly" });
            RunOracle(report,"ExplosiveOracle.Native.dll","explosive-native",new[] { "lifecycle" });
            RunOracle(report,"CellOracle.Native.dll","cell-native",new[] { "grid" });
            RunOracle(report,"DisguiseOracle.Native.dll","disguise-native",new[] { "lifecycle" });
            RunOracle(report,"BoundaryOracle.Native.dll","boundary-native",new[] { "lifecycle" });
            RunOracle(report,"NavigationOracle.Native.dll","navigation-native",new[] { "paths" });
            RunOracle(report,"DecoyOracle.Native.dll","decoy-native",new[] { "lifecycle","random" });
            RunOracle(report,"DecoyAiOracle.Native.dll","decoy-ai-native",new[] { "decisions" });
            RunOracle(report,"AudioOracle.Native.dll","audio-native",new[] { "catalog" });
            RunOracle(report,"PlayerAiOracle.Native.dll","player-ai-native",new[] { "policy" });
            RunOracle(report,"FleeNavigationOracle.Native.dll","flee-navigation-native",new[] { "paths" });
        } catch (Exception ex) {
            report.error = ex.ToString();
        } finally {
            File.WriteAllText(Path.Combine(Paths.GameRootPath, "native-probe.json"), Serialize(report));
            Application.Quit(0);
        }
    }
    IEnumerator RunCoroutineOracle(Report report,string assemblyName) {
        string file = Environment.GetEnvironmentVariable("BOOMERANG_SCENE_OUTPUT") ?? "scene-visual-native-level.json";
        if (Path.GetFileName(file) != file) throw new Exception("Scene output must be a basename");
        IEnumerator routine = null;
        try {
            var assembly = System.Reflection.Assembly.LoadFrom(Path.Combine(Paths.GameRootPath,"NativeOracles",assemblyName));
            routine = (IEnumerator)assembly.GetType("OracleEntry",true).GetMethod("Run").Invoke(null,new object[] { new[] { Path.Combine(Paths.GameRootPath,file) } });
        } catch(Exception ex) { report.error = ex.ToString(); }
        while(routine != null) {
            bool more = false;
            try { more = routine.MoveNext(); } catch(Exception ex) { report.error = ex.ToString(); }
            if(!more) break;
            yield return routine.Current;
        }
        (routine as IDisposable)?.Dispose();
        if(report.error == "" && File.Exists(Path.Combine(Paths.GameRootPath,file))) report.oracles.Add(file);
        File.WriteAllText(Path.Combine(Paths.GameRootPath,"native-probe.json"),Serialize(report));
        Application.Quit(0);
    }
    static void RunOracle(Report report, string assemblyName, string prefix, string[] modes) {
        string selected = Environment.GetEnvironmentVariable("BOOMERANG_NATIVE_ORACLE");
        if (!string.IsNullOrEmpty(selected) && selected != assemblyName) return;
        string path = Path.Combine(Paths.GameRootPath,"NativeOracles",assemblyName);
        if (!File.Exists(path)) return;
        var run = System.Reflection.Assembly.LoadFrom(path).GetType("OracleEntry",true).GetMethod("Run");
        foreach(string mode in modes) {
            string file = prefix + "-" + mode + ".json";
            run.Invoke(null,new object[] { new[] { Path.Combine(Paths.GameRootPath,file),mode } });
            report.oracles.Add(file);
        }
    }
}
