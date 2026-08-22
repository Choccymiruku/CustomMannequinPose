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
    //[BepInDependency("com.wtt.contentbackport", BepInDependency.DependencyFlags.SoftDependency)]
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
            new InterceptUpdateAnimator().Enable();
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
#if DEBUG
                Logger.LogInfo($"Load {Path.GetFileName(file)}!");
#endif
                foreach (AnimationClip clip in bundle.LoadAllAssets<AnimationClip>())
                {
                    AnimPoses[clip.name] = clip;
                }
                bundle.Unload(false);
#if DEBUG
                Logger.LogInfo("Bundle Loading pass");
#endif                
            }
        }
    
        public static void LoadJson()
        {
            var dllPath = Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(dllPath);
            var posePath = Path.Combine(path, "JsonPoseLists");
            var compiledPoseList = new List<CustomPose>();
            NameCheck.ClipName.Clear();
            if (Directory.Exists(posePath))
            {
                string[] files = Directory.GetFiles(posePath, "*.json", SearchOption.AllDirectories);
                
                foreach (string filepath in files)
                {
                    string json = File.ReadAllText(filepath);
                    var poseData = JsonConvert.DeserializeObject<CustomPose>(json);
                    
                    if (poseData == null)
                    {
                        Logger.LogWarning($"Unable to read Json {posePath}!");
                        return;
                    }
                    
                    if (poseData.CustomPoseLists == null)
                    {
                        Logger.LogWarning("There's no data inside the Json file");
                        return;
                    }
                    
                    foreach (var (id, name) in poseData.CustomPoseLists)
                    {
                        if (id == null || name == null)
                        {
                            return;
                        }
                        
                        NameCheck.ClipName.Add(id, name);
#if DEBUG
                        Logger.LogInfo($"{id} = {name} to ClipName");
#endif
                    }
                   
#if DEBUG
                    Logger.LogInfo($"{NameCheck.ClipName.Count} CustomPoseLists");
                    Logger.LogInfo("JsonLoadingPass");
#endif
                }
            }
            Logger.LogWarning($"Unable to Find Folder!");      
        }
    }
    
    public class CustomPose
    {
        public Dictionary<string, string> CustomPoseLists;
    }
    
    /*public class PostList
    {
        [JsonProperty("CustomPoseLists")]
        public List<CustomPose> CustomPoseLists {get; set; }
    }*/
    // not even used
    /*public class SelectedPose
    {
        public string EquipmentId;
        public MongoID PoseId;
        public string PoseName;
    }*/
    //Do a comparison check
    public class NameCheck
    {
        public static Dictionary<string, string> ClipName { get; set; } = new Dictionary<string, string>();
        //public static Dictionary<string, SelectedPose> CachedPose { get; } = new Dictionary<string, SelectedPose>();

        public static bool IsSimilarPose(string poseName)
        {
            return ClipName.Values.Contains(poseName);
        }

        public static bool IsSimilarId(string poseId)
        {
            return ClipName.Keys.Contains(poseId);
        }
    
        /*public static void SaveName(string equipmentId, MongoID poseId, string poseName)
        {
            CachedPose[equipmentId] = new SelectedPose()
            {
                PoseId = poseId,
                PoseName = poseName,
                EquipmentId = equipmentId
            };
        }*/
    }
}