using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using TacticalDoorWedgeClient.Configuration;
using TacticalDoorWedgeClient.Services;
using UnityEngine;

namespace TacticalDoorWedgeClient.Patches;

public class DoorBreachPatch : ModulePatch
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

        if (interactionResult.InteractionType != EInteractionType.Breach)
        {
            return true;
        }

        if (!DoorWedgeService.IsWedged(__instance))
        {
            return true;
        }

        // Door is wedged: apply kick damage
        var player = Singleton<GameWorld>.Instance?.MainPlayer;
        int damage = ModConfig.KickDamage.Value;

        DoorWedgeService.DamageWedge(__instance, damage, player, out bool breached);

        if (breached)
        {
            // Wedge destroyed: allow door to swing open
            return true;
        }

        // Wedge held: door remains shut
        var data = DoorWedgeService.GetWedgeData(__instance);
        int remainingHp = data != null ? data.Health : 0;

        NotificationManagerClass.DisplayMessageNotification($"Wedge absorbed the kick! (Remaining: {remainingHp} HP)");
        return false;
    }
}
