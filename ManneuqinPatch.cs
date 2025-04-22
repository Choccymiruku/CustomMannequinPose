using System.Reflection;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MorePoses;

// Change to start as a script that waits 2 seconds then changes the animator controller
public class ManneuqinPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(HideoutAreaStashController), nameof(HideoutAreaStashController.Init));
    }

    [PatchPostfix]
    public static void PatchPostfix(ref HideoutAreaStashController __instance)
    {
        __instance._inventoryEquipmentStashLoader.runtimeAnimatorController_0 = Plugin.AnimatorController;
    }
}