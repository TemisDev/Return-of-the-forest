using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bosque.CoreSetup
{
    public static class ArroyoBuild
    {
        public static void BuildWindowsBatch() => BuildWindows("ArroyoV2");
        public static void BuildVisualWindowsBatch() => BuildWindows("ArroyoV3");
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
