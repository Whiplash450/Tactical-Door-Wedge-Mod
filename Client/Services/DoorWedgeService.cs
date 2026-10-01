using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using TacticalDoorWedgeClient.Configuration;
using UnityEngine;

namespace TacticalDoorWedgeClient.Services;

public static class DoorWedgeService
{
    public const string WedgeTemplateId = "tactical_door_wedge";

    public class DoorWedgeData
    {
        public int Health;
        public string PlacedByProfileId;
        public Vector3 PlacedPosition;
        public Vector3 WedgedFacingDirection;
        public int KickCount;
    }

    private static readonly Dictionary<string, DoorWedgeData> WedgedDoors = new();

    public static void Clear()
    {
        WedgedDoors.Clear();
    }

    public static string GetDoorKey(Door door)
    {
        if (door == null) return string.Empty;
        return string.IsNullOrEmpty(door.Id) ? door.transform.position.ToString() : door.Id;
    }

    public static bool IsWedged(Door door)
    {
        if (door == null) return false;
        return WedgedDoors.ContainsKey(GetDoorKey(door));
    }

    public static DoorWedgeData GetWedgeData(Door door)
    {
        if (door == null) return null;
        WedgedDoors.TryGetValue(GetDoorKey(door), out var data);
        return data;
    }

    public static bool CanWedge(Door door, Player player)
    {
        if (door == null || player == null) return false;

        // Can only wedge shut doors
        if (door.DoorState != EDoorState.Shut) return false;

        // Cannot wedge an already wedged door
        if (IsWedged(door)) return false;

        // Player must have a wedge in inventory
        return HasWedgeInInventory(player);
    }

    public static bool WedgeDoor(Door door, Player player)
    {
        if (!CanWedge(door, player)) return false;

        if (player.IsYourPlayer && !ConsumeWedge(player))
        {
            return false;
        }

        string key = GetDoorKey(door);
        Vector3 forward = door.transform.forward;

        WedgedDoors[key] = new DoorWedgeData
        {
            Health = ModConfig.WedgeHealth.Value,
            PlacedByProfileId = player.ProfileId,
            PlacedPosition = door.transform.position,
            WedgedFacingDirection = forward,
            KickCount = 0
        };

        if (player.IsYourPlayer)
        {
            NotificationManagerClass.DisplayMessageNotification("Door secured with Tactical Door Wedge.");
        }

        return true;
    }

    public static bool RemoveWedge(Door door, Player player)
    {
        if (door == null || !IsWedged(door)) return false;

        string key = GetDoorKey(door);
        WedgedDoors.Remove(key);

        if (player != null && player.IsYourPlayer)
        {
            ReturnWedge(player);
            NotificationManagerClass.DisplayMessageNotification("Tactical Door Wedge retrieved.");
        }

        return true;
    }

    public static bool DamageWedge(Door door, int damage, Player attacker, out bool breached)
    {
        breached = false;
        if (door == null || !IsWedged(door)) return false;

        string key = GetDoorKey(door);
        var data = WedgedDoors[key];
        data.KickCount++;
        data.Health -= damage;

        if (data.Health <= 0)
        {
            breached = true;
            WedgedDoors.Remove(key);

            // Force door open
            try
            {
                door.DoorState = EDoorState.Shut;
                door.Interact(new InteractionResult(EInteractionType.Breach));
            }
            catch
            {
                door.Open();
            }

            if (attacker != null && attacker.IsYourPlayer)
            {
                NotificationManagerClass.DisplayMessageNotification("Door wedge destroyed!");
            }
        }

        return true;
    }

    public static bool HasWedgeInInventory(Player player)
    {
        if (player?.Profile?.Inventory == null) return false;

        var items = player.Profile.Inventory.GetPlayerItems();
        return items.Any(i => i.TemplateId == WedgeTemplateId);
    }

    public static bool ConsumeWedge(Player player)
    {
        if (player?.Profile?.Inventory == null) return false;

        var items = player.Profile.Inventory.GetPlayerItems();
        var wedgeItem = items.FirstOrDefault(i => i.TemplateId == WedgeTemplateId);
        if (wedgeItem == null) return false;

        if (wedgeItem.StackObjectsCount > 1)
        {
            wedgeItem.StackObjectsCount--;
        }
        else
        {
            wedgeItem.StackObjectsCount = 0;
        }

        return true;
    }

    public static void ReturnWedge(Player player)
    {
        if (player?.Profile?.Inventory == null) return;

        var items = player.Profile.Inventory.GetPlayerItems();
        var wedgeItem = items.FirstOrDefault(i => i.TemplateId == WedgeTemplateId);
        if (wedgeItem != null)
        {
            wedgeItem.StackObjectsCount++;
        }
    }
}
