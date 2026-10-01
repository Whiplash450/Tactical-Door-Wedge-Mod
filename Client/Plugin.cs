using System;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using TacticalDoorWedgeClient.Configuration;
using TacticalDoorWedgeClient.Patches;
using TacticalDoorWedgeClient.Services;
using UnityEngine;

namespace TacticalDoorWedgeClient;

[BepInPlugin("com.hugh.tacticaldoorwedge", "Tactical Door Wedge Mod", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource LogSource;
    public static Plugin Instance;

    private const float MaxDoorInteractionDistance = 2.5f;

    private void Awake()
    {
        Instance = this;
        LogSource = Logger;

        // Initialize F12 settings
        ModConfig.Init(Config);

        // Enable Harmony patches
        new DoorOpenPatch().Enable();
        new DoorBreachPatch().Enable();
        new BotDoorBreachPatch().Enable();

        LogSource.LogInfo("Tactical Door Wedge Mod 1.0.0 client initialized successfully.");
    }

    private void Update()
    {
        if (ModConfig.WedgeHotkey == null || !ModConfig.WedgeHotkey.Value.IsDown())
        {
            return;
        }

        var player = Singleton<GameWorld>.Instance?.MainPlayer;
        if (player == null || Camera.main == null)
        {
            return;
        }

        // Raycast from camera center to find door
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, MaxDoorInteractionDistance, LayerMaskClass.InteractiveMask))
        {
            Door door = hit.collider.GetComponentInParent<Door>();
            if (door != null)
            {
                if (DoorWedgeService.IsWedged(door))
                {
                    DoorWedgeService.RemoveWedge(door, player);
                }
                else
                {
                    if (door.DoorState == EDoorState.Shut)
                    {
                        if (DoorWedgeService.HasWedgeInInventory(player))
                        {
                            DoorWedgeService.WedgeDoor(door, player);
                        }
                        else
                        {
                            NotificationManagerClass.DisplayMessageNotification("No Tactical Door Wedge in inventory.");
                        }
                    }
                    else
                    {
                        NotificationManagerClass.DisplayMessageNotification("Door must be completely closed to wedge.");
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        DoorWedgeService.Clear();
    }
}
