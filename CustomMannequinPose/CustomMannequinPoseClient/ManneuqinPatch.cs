using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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
    /*public class OnPoseChangedPatch : ModulePatch
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
    }*/
    // Overrides the animatorcontroller whenever mannequin got updated with poses from our list
    // I don't think i need this patch given the update animator async patch
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
            if (!NameCheck.IsSimilarPose(poseAnimationClipName)) return true;
            if (!Plugin.AnimPoses.TryGetValue(poseAnimationClipName, out var animClip)) return true;
            //copy the runtimeanimatorcontroller from our mannequin and store it
            if (Plugin.AnimatorController == null)
            {
                Plugin.AnimatorController = mannequinAnimator.runtimeAnimatorController;
            }
            //create a override controller basing the target from our saved runtime controller
            var OverrideController = new AnimatorOverrideController(Plugin.AnimatorController);
            //override the animation state "standing" with our animation clip we got from previous
            OverrideController["standing"] = animClip;
            //replace the controller with override
            mannequinAnimator.runtimeAnimatorController = OverrideController;
            //force play the modified state. 
            mannequinAnimator.Play("standing");
            return false;
        }
    }

    public class InterceptUpdateAnimator : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryEquipmentStashLoader), "UpdateAnimator");
        }

        [PatchPostfix]
        public static void PatchPostfix(InventoryEquipmentStashLoader __instance, Item item, Dictionary<string, MongoID> ____mannequinPoses, RuntimeAnimatorController ____mannequinAnimatorController)
        {
            //Update animator always keeps running as an async task. So, we grab the animator controller from all mannequin
            //grab the MongoID of the pose from all mannequin, i think
            var playerAnimatorController = __instance.LoadedPlayerModels[item].ModelPlayerPoser.PlayerAnimatorController;
            ____mannequinPoses.TryGetValue(item.Id, out var selectedId);

            //check if the ID being used as the pose matches with ours
            if (!NameCheck.IsSimilarId(selectedId))
            {
#if DEBUG
                Logger.LogInfo($"{selectedId} Does not have corresponding value in pose lists");
#endif
                return;
            }

            //passes the check, so we try to grab the pose name that is linked to the MongoID
            if (!NameCheck.ClipName.TryGetValue(selectedId, out var clipName))
            {
#if DEBUG
                Logger.LogInfo($"Cannot find clip name from ID {selectedId}");
#endif
                return;
            }
            //grab the name of the pose from our stored animation clip
            //and override the controller with Animator Override Controller
            Plugin.AnimPoses.TryGetValue(clipName, out var animClip);
            if (Plugin.AnimatorController == null)
            {
                Plugin.AnimatorController = ____mannequinAnimatorController;
            }
            var overrideController = new AnimatorOverrideController(Plugin.AnimatorController);
            overrideController["standing"] = animClip;
            playerAnimatorController.runtimeAnimatorController = overrideController;
            //force play Overriden State
            playerAnimatorController.Play("standing");
        }
    }
}