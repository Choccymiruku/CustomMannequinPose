using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using SPT.Reflection;
using UnityEngine;

namespace MorePoses;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;
    // public static AnimationClip Clip;
    public static RuntimeAnimatorController AnimatorController;

    private void Awake()
    {
        // Plugin startup logic
        Logger = new ManualLogSource(nameof(Plugin));
        new ManneuqinPatch().Enable();
        LoadBundle();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

    }

    public static void LoadBundle()
    {
        var dllPath = Assembly.GetExecutingAssembly().Location;
        var path = Path.GetDirectoryName(dllPath);
        var bundlePath = Path.Combine(path, "custom_mannequin_test.bundle");

        var bundle = AssetBundle.LoadFromFile(bundlePath);

        if (bundle == null)
        {
            Logger.LogError($"Failed to load custom_mannequin_test.bundle!");
            return;
        }

        AnimatorController = LoadAsset<RuntimeAnimatorController>(bundle, "assets/custom script/custom_pose.controller");
        Logger.LogInfo("Its loaded I G");

    }

    public static T LoadAsset<T>(AssetBundle bundle, string assetPath) where T : UnityEngine.Object
    {
        T asset = bundle.LoadAsset<T>(assetPath);

        if (asset == null)
        {
            throw new Exception($"Error loading asset {assetPath}");
        }

        DontDestroyOnLoad(asset);
        return asset;
    }
}