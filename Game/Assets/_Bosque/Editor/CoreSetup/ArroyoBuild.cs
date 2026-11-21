using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bosque.CoreSetup
{
    public static class ArroyoBuild
    {
        public static void BuildWindowsBatch() => BuildWindows("ArroyoV2");
        public static void BuildVisualWindowsBatch() => BuildWindows("ArroyoV3");
        public static void BuildPolishedWindowsBatch() => BuildWindows("ArroyoV3Polish");
        public static void BuildV4WindowsBatch() => BuildWindows("ArroyoV4");
        public static void BuildRC1WindowsBatch() => BuildCandidate(false);
        public static void BuildRC1DiagnosticsBatch() => BuildCandidate(true);
        public static void BuildV6WindowsBatch() => BuildCandidate(false, "V6");
        public static void BuildV6DiagnosticsBatch() => BuildCandidate(true, "V6-Diagnostics");
        public static void BuildV7WindowsBatch() => BuildCandidate(false, "V7-Character-Audio");
        public static void BuildV8WindowsBatch() => BuildCandidate(false, "V8-Zona2-Claro");
        public static void BuildV9WindowsBatch() => BuildCandidate(false, "V9-Zona3-Refugio");
        public static void BuildV10WindowsBatch() => BuildCandidate(false, "V10-Presentacion");
        public static void BuildV11WindowsBatch() => BuildCandidate(false, "V11-Valle");
        [Serializable] private sealed class CandidateInfo
        {
            public string identifier, commit, unity, builtAtUtc, target, scene, options;
            public bool development, optionalQaVerifier;
            public int errors, warnings;
            public long totalBytes;
            public double buildSeconds;
        }
        private static void BuildCandidate(bool diagnostics, string name = null)
        {
            try
            {
                string commit = Environment.GetEnvironmentVariable("BOSQUE_BUILD_COMMIT");
                if (commit == null || !Regex.IsMatch(commit, "^[0-9a-f]{40}$"))
                    throw new InvalidOperationException("Generar con Tools/Build-ArroyoRC1.ps1 desde una copia limpia.");
                string identifier = name ?? (diagnostics ? "RC1-Diagnostics" : "RC1");
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Windows/" + identifier));
                if (File.Exists(Path.Combine(folder, "build-info.json")))
                    throw new InvalidOperationException("Ya existe una build identificada; conservarla antes de generar otra candidata.");
                Directory.CreateDirectory(folder);
                var options = diagnostics ? BuildOptions.Development : BuildOptions.None;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    // Todas las zonas activas en Build Settings (Arroyo primero); antes solo el arroyo.
                    scenes = Array.FindAll(EditorBuildSettings.scenes, s => s.enabled).Length > 0
                        ? Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path)
                        : new[] { ArroyoPlayableSetup.ScenePath },
                    locationPathName = Path.Combine(folder, "BosqueDeRegreso.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = options,
                    extraScriptingDefines = new[] { "BOSQUE_RC1_QA" }
                });
                var info = new CandidateInfo { identifier = identifier, commit = commit, unity = Application.unityVersion,
                    builtAtUtc = DateTime.UtcNow.ToString("o"), target = "StandaloneWindows64", scene = ArroyoPlayableSetup.ScenePath,
                    options = options.ToString(), development = diagnostics, optionalQaVerifier = true,
                    errors = (int)report.summary.totalErrors, warnings = (int)report.summary.totalWarnings,
                    totalBytes = (long)report.summary.totalSize, buildSeconds = report.summary.totalTime.TotalSeconds };
                if (report.summary.result == BuildResult.Succeeded)
                    File.WriteAllText(Path.Combine(folder, "build-info.json"), JsonUtility.ToJson(info, true));
                Debug.Log("[" + identifier + "] Build " + report.summary.result + ": " + info.errors + " errores; commit=" + commit);
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static void BuildWindows(string name)
        {
            try
            {
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/" + name));
                Directory.CreateDirectory(folder);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ArroyoPlayableSetup.ScenePath },
                    locationPathName = Path.Combine(folder, "BosqueDeRegreso.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                Debug.Log("[" + name + "] Build " + report.summary.result + ": " + report.summary.totalErrors + " errores.");
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
