using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using TacticalDoorWedgeClient.Services;

namespace TacticalDoorWedgeClient.Patches;

public class DoorOpenPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Door), nameof(Door.Interact));
    }

    [PatchPrefix]
    private static bool PatchPrefix(Door __instance, InteractionResult interactionResult)
    {
        if (__instance == null || interactionResult == null)
        {
            return true;
        }

        // Only intercept if the door is wedged
        if (!DoorWedgeService.IsWedged(__instance))
        {
            return true;
        }

        // Allow breach interactions to be handled by DoorBreachPatch
        if (interactionResult.InteractionType == EInteractionType.Breach)
        {
            return true;
        }

        // Block normal opening, unlocking, or turning of wedged doors
        if (interactionResult.InteractionType == EInteractionType.Open ||
            interactionResult.InteractionType == EInteractionType.Unlock)
        {
            NotificationManagerClass.DisplayMessageNotification("The door is wedged shut and cannot be opened with the handle.");
            return false;
        }

        return true;
    }
}
