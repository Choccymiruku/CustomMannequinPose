using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT;
using SPT.Reflection;
using UnityEngine;
using Newtonsoft.Json;

namespace CustomMannequinPose
{
    [BepInPlugin("com.choccy.custommannequinpose", "com.choccy.custommannequinpose", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;
        // public static AnimationClip Clip;
        public static RuntimeAnimatorController AnimatorController;
        public static Dictionary<string, AnimationClip> AnimPoses = new Dictionary<string,AnimationClip>();
        public static AnimatorOverrideController OverrideController;
    
        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo("Plugin is loaded!");
            LoadBundle();
            LoadJson();
            
            //new OnPoseChangedPatch().Enable();
            new UpdateMannequinPosePatch().Enable();
        }
    
        public static void LoadBundle()
        {
            var dllPath = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(dllPath);
            var bundlePath = Path.Combine(path, "PoseBundle");
    
            foreach (string file in Directory.GetFiles(bundlePath, "*.bundle"))
            {
                var bundle = AssetBundle.LoadFromFile(file);
                
                if (bundle == null)
                {
                    Logger.LogError($"Failed to load {Path.GetFileName(file)}!");
                    continue;
                }
                Logger.LogInfo($"Load {Path.GetFileName(file)}!");
                foreach (AnimationClip clip in bundle.LoadAllAssets<AnimationClip>())
                {
                    AnimPoses[clip.name] = clip;
                }
                bundle.Unload(false);
                Logger.LogInfo("Bundle Loading pass");
            }
        }
    
        public static void LoadJson()
        {
            var dllPath = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(dllPath);
            var posePath = Path.Combine(path, "JsonPoseLists");
            var compiledPoseList = new List<PostList>();
            NameCheck.ClipName.Clear();
            if (Directory.Exists(posePath))
            {
                string[] files = Directory.GetFiles(posePath, "*.json", SearchOption.AllDirectories);
                
                foreach (string filepath in files)
                {
                    string json = File.ReadAllText(filepath);
                    var poseData = JsonConvert.DeserializeObject<PostList>(json);
                    if (poseData.CustomPoseLists != null)
                    {
                        compiledPoseList.Add(poseData);
                    }
                    if (poseData?.CustomPoseLists == null)
                    {
                        Logger.LogWarning($"Unable to read Json {posePath}!");
                        return;
                    }
                    foreach (CustomPose pose in poseData.CustomPoseLists)
                    {
                        if (string.IsNullOrEmpty(pose?.ClipName))
                        {
                            continue;
                        }
                        NameCheck.ClipName.Add(pose.ClipName);
                        Logger.LogInfo($"Added {pose.ClipName} to ClipName");
                        Logger.LogInfo($"{NameCheck.ClipName.Count} CustomPoseLists");
                    }
                    Logger.LogInfo("JsonLoadingPass");
                }
            }
        }
    }
    /// <summary>
    /// the name of the animation clip named used to referenced what animation state to play
    /// </summary>
    public class CustomPose
    {
        public string ClipName;
    }
    /// <summary>
    /// Custom list for storing clipName
    /// </summary>
    public class PostList
    {
        public List<CustomPose> CustomPoseLists {get; set;}
    }
    /// <summary>
    /// stores the relevant passed data from EquipmentPresetsPanel Show
    /// </summary>
    public class SelectedPose
    {
        public string EquipmentId;
        public MongoID PoseId;
        public string PoseName;
    }
    /// <summary>
    /// OverridePose does a comparison, if the poseName is not from CustomPoseLists it will return false
    /// SaveName saves the passed data of SelectedPose to cache it. Will be used to pass the poseName and override the pose with our custom ones
    /// </summary>
    public class NameCheck
    {
        public static List<string> ClipName { get; set; } = new List<string>();
        public static Dictionary<string, SelectedPose> CachedPose { get; } = new Dictionary<string, SelectedPose>();

        public static bool IsSimilarCheck(string poseName)
        {
            return ClipName.Contains(poseName);
        }
    
        public static void SaveName(string equipmentId, MongoID poseId, string poseName)
        {
            CachedPose[equipmentId] = new SelectedPose()
            {
                PoseId = poseId,
                PoseName = poseName,
                EquipmentId = equipmentId
            };
        }
    }
}