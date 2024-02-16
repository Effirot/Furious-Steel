#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Unity.EditorCoroutines.Editor;
using UnityEngine.SceneManagement;
using System.Linq;
using System.IO;

public class BuildTools : EditorWindow
{
    [MenuItem("Tools/Build Tools")]
    public static void OnShowTools()
    {
        EditorWindow.GetWindow<BuildTools>();
    }

    private BuildTargetGroup GetTargetGroupForTarget(BuildTarget target) => target switch
    {
        BuildTarget.StandaloneOSX => BuildTargetGroup.Standalone,
        BuildTarget.StandaloneWindows => BuildTargetGroup.Standalone,
        BuildTarget.iOS => BuildTargetGroup.iOS,
        BuildTarget.Android => BuildTargetGroup.Android,
        BuildTarget.StandaloneWindows64 => BuildTargetGroup.Standalone,
        BuildTarget.WebGL => BuildTargetGroup.WebGL,
        BuildTarget.StandaloneLinux64 => BuildTargetGroup.Standalone,
        _ => BuildTargetGroup.Unknown
    };

    private Dictionary<BuildTarget, bool> TargetsToBuild = new Dictionary<BuildTarget, bool>();
    private List<BuildTarget> AvailableTargets = new List<BuildTarget>();

    private bool BuildServer = false; 

    private void OnEnable()
    {
        AvailableTargets.Clear();
        var buildTargets = System.Enum.GetValues(typeof(BuildTarget));
        foreach(var buildTargetValue in buildTargets)
        {
            BuildTarget target = (BuildTarget)buildTargetValue;

            // skip if unsupported
            if (!BuildPipeline.IsBuildTargetSupported(GetTargetGroupForTarget(target), target))
                continue;

            AvailableTargets.Add(target);

            // add the target if not in the build list
            if (!TargetsToBuild.ContainsKey(target))
                TargetsToBuild[target] = false;
        }

        // check if any targets have gone away
        if (TargetsToBuild.Count > AvailableTargets.Count)
        {
            // build the list of removed targets
            List<BuildTarget> targetsToRemove = new List<BuildTarget>();
            foreach(var target in TargetsToBuild.Keys)
            {
                if (!AvailableTargets.Contains(target))
                    targetsToRemove.Add(target);
            }

            // cleanup the removed targets
            foreach(var target in targetsToRemove)
                TargetsToBuild.Remove(target);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Platforms to Build", EditorStyles.boldLabel);

        BuildServer = EditorGUILayout.Toggle("Build Server additive", BuildServer);
        
        EditorGUILayout.Space();
        
        int activeBuildTargetsCount = 0;
        foreach(var target in AvailableTargets)
        {
            TargetsToBuild[target] = EditorGUILayout.Toggle(target.ToString(), TargetsToBuild[target]);

            if (TargetsToBuild[target])
            {
                activeBuildTargetsCount++;
            }
        }

        if (activeBuildTargetsCount > 0)
        {            
            if (GUILayout.Button(activeBuildTargetsCount == 1 ? "Build 1 Platform" : $"Build {activeBuildTargetsCount} Platforms"))
            {
                StartBuild();
            }
        }
    }

    private void StartBuild()
    {
        
        var targets = TargetsToBuild.Where(KeyValue => KeyValue.Value).ToArray();
        foreach(var target in targets)
        {
            PerformBuild(target.Key, $"Build\\{target.Key.ToString()}", StandaloneBuildSubtarget.Player);

            if (BuildServer)
            {
                if (target.Key == BuildTarget.StandaloneWindows || 
                    target.Key == BuildTarget.StandaloneWindows64 ||
                    target.Key == BuildTarget.StandaloneLinux64)
                {
                    PerformBuild(target.Key, $"Build\\{target.Key.ToString()}\\Server", StandaloneBuildSubtarget.Server);
                }
            }
        }

        // System.Diagnostics.Process.Start("explorer.exe", path);
    }

    private void PerformBuild(BuildTarget target, string directory, StandaloneBuildSubtarget standaloneBuildSubtarget, BuildOptions options = BuildOptions.None)
    {        
        directory = Directory.GetCurrentDirectory() + "\\" + directory;

        Directory.CreateDirectory(directory);

        BuildPlayerOptions buildPlayerOptions = new()
        {
            scenes = GetAllScenesNames(),
            locationPathName = directory  + "\\" + Application.productName + ".exe",
            target = target,
            subtarget = (int) standaloneBuildSubtarget,  
            options = options,
        };

        Debug.Log($"Start {target} building process...\nScenes: {string.Join(", ", buildPlayerOptions.scenes)}");

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        Debug.Log($"Build was succesfully complete and saved in {directory}\n Scenes: {string.Join(", ", buildPlayerOptions.scenes)}");
    }

    private string[] GetAllScenesNames()
    {
        var scenesParameters = EditorBuildSettings.scenes;

        var result = new string[scenesParameters.Count()];
        
        for (int i = 0; i < scenesParameters.Count(); i++)
        {
            result[i] = scenesParameters[i].path;
        }

        return result;
    }
}

#endif