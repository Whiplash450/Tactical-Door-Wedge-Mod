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
        new DoorInteractionPatch().Enable();

        LogSource.LogInfo("Tactical Door Wedge Mod 1.0.0 client initialized successfully.");
    }

    private void OnDestroy()
    {
        DoorWedgeService.Clear();
    }
}
