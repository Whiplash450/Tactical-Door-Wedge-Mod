using BepInEx.Configuration;
using UnityEngine;

namespace TacticalDoorWedgeClient.Configuration;

public static class ModConfig
{
    public static ConfigEntry<int> WedgeHealth { get; private set; }
    public static ConfigEntry<int> KickDamage { get; private set; }

    public static void Init(ConfigFile config)
    {
        WedgeHealth = config.Bind(
            "Door Wedge Settings",
            "Wedge Health",
            100,
            new ConfigDescription(
                "Maximum durability of a wedged door. Kicks reduce this durability.",
                new AcceptableValueRange<int>(1, 1000)
            )
        );

        KickDamage = config.Bind(
            "Door Wedge Settings",
            "Kick Damage",
            51,
            new ConfigDescription(
                "Damage dealt to the door wedge durability per kick.",
                new AcceptableValueRange<int>(1, 200)
            )
        );
    }
}
