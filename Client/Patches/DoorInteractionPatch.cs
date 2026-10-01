using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using TacticalDoorWedgeClient.Services;

namespace TacticalDoorWedgeClient.Patches;

public class DoorInteractionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GetActionsClass).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(GetActionsClass.GetAvailableActions) &&
                                 m.GetParameters().Length == 2 &&
                                 m.GetParameters()[0].ParameterType == typeof(GamePlayerOwner));
    }

    [PatchPostfix]
    private static void PatchPostfix(GamePlayerOwner owner, GInterface177 interactive, ref ActionsReturnClass __result)
    {
        if (interactive is not Door door || owner == null)
        {
            return;
        }

        Player player = owner.Player;
        if (player == null || !player.IsYourPlayer)
        {
            return;
        }

        if (__result == null)
        {
            __result = new ActionsReturnClass();
        }

        if (__result.Actions == null)
        {
            __result.Actions = new List<ActionsTypesClass>();
        }

        // Check if the door is already wedged
        if (DoorWedgeService.IsWedged(door))
        {
            __result.Actions.Add(new ActionsTypesClass
            {
                Name = "Remove Wedge",
                Action = () => DoorWedgeService.RemoveWedge(door, player),
                Disabled = false
            });
        }
        else if (door.DoorState == EDoorState.Shut && DoorWedgeService.HasWedgeInInventory(player))
        {
            __result.Actions.Add(new ActionsTypesClass
            {
                Name = "Wedge Door",
                Action = () => DoorWedgeService.WedgeDoor(door, player),
                Disabled = false
            });
        }
    }
}
