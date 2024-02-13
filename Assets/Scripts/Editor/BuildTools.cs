#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Unity.EditorCoroutines.Editor;
using UnityEngine.SceneManagement;

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

        BuildServer = EditorGUILayout.Toggle(BuildServer, "Build Server additive");

        // display the build targets
        int numEnabled = 0;
        foreach(var target in AvailableTargets)
        {
            TargetsToBuild[target] = EditorGUILayout.Toggle(target.ToString(), TargetsToBuild[target]);

            if (TargetsToBuild[target])
            {
                numEnabled++;
            }
        }

        if (numEnabled > 0)
        {
            // attempt to build?
            string prompt = numEnabled == 1 ? "Build 1 Platform" : $"Build {numEnabled} Platforms";
            if (GUILayout.Button(prompt))
            {
                StartBuild();
            }
        }
    }

    private void StartBuild()
    {
        string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
       
    }

    void PerformBuild(BuildTarget target, string path, StandaloneBuildSubtarget standaloneBuildSubtarget, BuildOptions options = BuildOptions.None)
    {

        #error FIX THIS YOU LIZZY FAT ASS
        
        BuildPlayerOptions buildPlayerOptions = new()
        {
            scenes = GetAllScenesNames(),
            locationPathName = path,
            target = target,
            subtarget = (int) standaloneBuildSubtarget,  
            options = options,
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    bool BuildIndividualTarget(BuildTarget target)
    {
        BuildPlayerOptions options = new BuildPlayerOptions();

        // get the list of scenes
        List<string> scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            scenes.Add(scene.path);

        // configure the build
        options.scenes = scenes.ToArray();
        options.target = target;
        options.targetGroup = GetTargetGroupForTarget(target);

        // set the location path name
        if (target == BuildTarget.Android)
        {
            string apkName = PlayerSettings.productName + ".apk";
            options.locationPathName = System.IO.Path.Combine("Builds", target.ToString(), apkName);
        }else if (target == BuildTarget.StandaloneWindows64)
        {
            options.locationPathName = System.IO.Path.Combine("Builds", target.ToString(), PlayerSettings.productName+".exe");
        }else if (target == BuildTarget.StandaloneLinux64)
        {
            options.locationPathName = System.IO.Path.Combine("Builds", target.ToString(), PlayerSettings.productName+".x86_64");
        }
        else
            options.locationPathName = System.IO.Path.Combine("Builds", target.ToString(), PlayerSettings.productName);

        if (BuildPipeline.BuildCanBeAppended(target, options.locationPathName) == CanAppendBuild.Yes)
            options.options = BuildOptions.AcceptExternalModificationsToPlayer;
        else
            options.options = BuildOptions.None;

        // start the build
        BuildReport report = BuildPipeline.BuildPlayer(options);

        // was the build successful?
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build for {target.ToString()} completed in {report.summary.totalTime.Seconds} seconds");
            return true;
        }

        Debug.LogError($"Build for {target.ToString()} failed");
        
        return false;
    }

    private string[] GetAllScenesNames()
    {
        var result = new string[SceneManager.sceneCount];

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            result[i] = SceneManager.GetSceneAt(i).name;
        }

        return result;
    }
}

#endif