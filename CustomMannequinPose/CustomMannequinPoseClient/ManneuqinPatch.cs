using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Customization;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace CustomMannequinPose
{
    //OnPoseChangedPatch is not needed since this is like the surface level method that taps the UpdateMannequinPose
    public class OnPoseChangedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EquipmentPresetsPanel), "OnPoseChanged");
        }

        [PatchPostfix]
        public static void PatchPostfix(EquipmentPresetsPanel __instance, HideoutAreaStashController ____stashController,
            Item equipment, CustomizationHideoutMannequinPose pose)
        {
            //Gets the stashcontroller
            //Check if the name of the pose is similar to that of ours, if not then do nothing if yes do our stuff
            if (____stashController == null)
            {
                Logger.LogInfo("Stashcontroller is null");
                return;
            }

            if (!NameCheck.IsSimilarCheck(pose.PoseName))
            {
                Logger.LogInfo($"{pose.PoseName} is not part of custom poses list");
                return;
            }
            //Comparison below, something is wrong if they are different in a way
            Logger.LogInfo($"{pose.PoseName}");
            Logger.LogInfo($"{NameCheck.CachedPose[equipment.Id].PoseName}");
            //below should do the following, force the mannequin to play Standing animation instead
            ____stashController.UpdateMannequinPose(equipment, "standing");
        }
    }
    // Overrides the animatorcontroller whenever mannequin got updated with poses from our list
    public class UpdateMannequinPosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryEquipmentStashLoader), "UpdateMannequinPose");
        }

        [PatchPrefix]
        public static bool PatchPrefix(InventoryEquipmentStashLoader __instance, Item equipment, string poseAnimationClipName)
        {
            //grab the mannequin that is currently being selected for changing pose
            if (!__instance.LoadedPlayerModels.TryGetValue(equipment, out var playerModelLoader)) return true;
            var mannequinAnimator = playerModelLoader.ModelPlayerPoser.PlayerAnimatorController;
            
            //check if the mannequinPoseName is similar to ours, if it is grab the animation with said name
            //from our stored array of animation in AnimPoses
            if (!NameCheck.IsSimilarCheck(poseAnimationClipName)) return true;
            if (!Plugin.AnimPoses.TryGetValue(poseAnimationClipName, out var animClip)) return true;
            //copy the runtimeanimatorcontroller from our mannequin and store it
            if (Plugin.AnimatorController == null)
            {
                Plugin.AnimatorController = mannequinAnimator.runtimeAnimatorController;
            }
            //create a override controller basing the target from our saved runtime controller
            var OverrideController = new  AnimatorOverrideController(Plugin.AnimatorController);
            //override the animation state "standing" with our animation clip we got from previous
            OverrideController["standing"] = animClip;
            //replace the controller with override
            mannequinAnimator.runtimeAnimatorController = OverrideController;
            //force play the modified state. 
            mannequinAnimator.Play("standing");
            return false;
        }
    }
}