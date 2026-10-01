using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using TacticalDoorWedgeClient.Configuration;
using TacticalDoorWedgeClient.Services;
using UnityEngine;
using UnityEngine.AI;

namespace TacticalDoorWedgeClient.Patches;

public class BotDoorBreachPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotDoorOpener), "Interact")
            ?? AccessTools.GetDeclaredMethods(typeof(BotDoorOpener)).FirstOrDefault(m => m.GetParameters().Any(p => p.ParameterType == typeof(Door)));
    }

    [PatchPrefix]
    private static bool PatchPrefix(BotDoorOpener __instance, Door door)
    {
        if (__instance == null || door == null)
        {
            return true;
        }

        if (!DoorWedgeService.IsWedged(door))
        {
            return true;
        }

        // Get BotOwner from BotDoorOpener
        FieldInfo botOwnerField = AccessTools.Field(typeof(BotDoorOpener), "_owner")
            ?? AccessTools.Field(typeof(BotDoorOpener), "botOwner_0")
            ?? AccessTools.GetDeclaredFields(typeof(BotDoorOpener)).FirstOrDefault(f => f.FieldType == typeof(BotOwner));

        BotOwner bot = botOwnerField?.GetValue(__instance) as BotOwner;
        if (bot == null)
        {
            return false;
        }

        var wedgeData = DoorWedgeService.GetWedgeData(door);
        if (wedgeData == null)
        {
            return true;
        }

        Player botPlayer = bot.GetPlayer;
        bool isScav = !IsEliteOrBoss(bot);

        if (isScav)
        {
            // Scav behaviour: kick once, then give up and lose player tracking
            if (wedgeData.KickCount == 0)
            {
                int damage = ModConfig.KickDamage.Value;
                DoorWedgeService.DamageWedge(door, damage, botPlayer, out bool breached);

                if (breached)
                {
                    return true;
                }

                // Door held: scav gives up and loses target
                ResetScavTracking(bot);
                return false;
            }
            else
            {
                // Scav has already kicked and given up on this door
                ResetScavTracking(bot);
                return false;
            }
        }
        else
        {
            // PMC and Boss behaviour: kick once, check alternate routes
            if (wedgeData.KickCount == 0)
            {
                int damage = ModConfig.KickDamage.Value;
                DoorWedgeService.DamageWedge(door, damage, botPlayer, out bool breached);

                if (breached)
                {
                    return true;
                }

                // Door held: check if alternative route exists to target
                var targetPerson = bot.Memory?.GoalEnemy?.Person;
                if (targetPerson != null && HasAlternateRoute(bot.Position, targetPerson.Position))
                {
                    // Alternate route exists: bot aborts door and flanks
                    bot.StopMove();
                    bot.Memory.GoalEnemy = null;
                    return false;
                }

                // No alternative route: bot will kick through on subsequent attempts
                return false;
            }
            else
            {
                // Subsequent attempt: no alternate route, kick through until broken
                int damage = ModConfig.KickDamage.Value;
                DoorWedgeService.DamageWedge(door, damage, botPlayer, out bool breached);
                return breached;
            }
        }
    }

    private static bool IsEliteOrBoss(BotOwner bot)
    {
        if (bot?.Profile?.Info?.Settings == null) return false;

        WildSpawnType role = bot.Profile.Info.Settings.Role;
        string roleName = role.ToString().ToLower();

        return roleName.Contains("pmc") 
            || roleName.Contains("boss") 
            || roleName.Contains("follower") 
            || roleName.Contains("arena");
    }

    private static void ResetScavTracking(BotOwner bot)
    {
        if (bot == null) return;

        try
        {
            bot.StopMove();
            bot.Memory.GoalEnemy = null;
        }
        catch
        {
            // Fallback safety
        }
    }

    private static bool HasAlternateRoute(Vector3 start, Vector3 end)
    {
        try
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
            {
                return path.status == NavMeshPathStatus.PathComplete;
            }
        }
        catch
        {
            // Navigation check fallback
        }

        return false;
    }
}
