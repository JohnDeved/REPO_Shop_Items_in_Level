﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
using System.Collections;
namespace REPO_Shop_Items_in_Level;

public class SwitchItemTracker : MonoBehaviour {}
public class UsedVolumeTracker : MonoBehaviour {}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("REPO.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> SpawnUpgradeItems;
    internal static ConfigEntry<float> UpgradeItemSpawnChance;

    private Harmony harmony;

    // Static instance for script access
    public static Plugin Instance { get; private set; }

    private void Awake()
    {
        // Set instance for script access
        Instance = this;

        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Initialize and apply Harmony patches
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(Plugin));

        Logger.LogInfo("Harmony patches applied!");

        // Updated config entries with proper descriptions for config UI mod
        SpawnUpgradeItems = Config.Bind("UpgradeItems", "SpawnUpgradeItems", true, new ConfigDescription("Whether upgrade items can spawn in levels"));
        UpgradeItemSpawnChance = Config.Bind("UpgradeItems", "UpgradeItemSpawnChance", 5f, new ConfigDescription("% chance for an upgrade item to spawn", new AcceptableValueRange<float>(0f, 100f)));
    }

    private static bool GetRandomItemOfType(SemiFunc.itemType itemType, out Item item)
    {
        var possibleItems = StatsManager.instance.itemDictionary.Values.Where(item => item.itemType == itemType).ToList();
        if (possibleItems.Count == 0)
        {
            Logger.LogWarning($"Failed to get random item of type {itemType}");
            item = null;
            return false;
        }

        item = possibleItems[Random.Range(0, possibleItems.Count)];
        return true;
    }

    private static bool HasValuablePropSwitch(ValuableVolume volume)
    {
        return volume.transform.GetComponentInParent<ValuablePropSwitch>() != null;
    }

    private static bool ShouldSpawnItem(ValuableVolume volume, out SemiFunc.itemType? itemType, out bool hasSwitch)
    {
        itemType = null;
        hasSwitch = HasValuablePropSwitch(volume);
        if (hasSwitch) return false; // sanity check - we do not support switches yet (sync issues)

        switch (volume.VolumeType)
        {
            // tiny space = item upgrades
            case ValuableVolume.Type.Tiny:
                if (!SpawnUpgradeItems.Value) return false;
                itemType = SemiFunc.itemType.item_upgrade;
                return Random.Range(0f, 100f) <= UpgradeItemSpawnChance.Value;

            default:
                return false;
        }
    }

    private static bool RandomItemSpawn(ValuableVolume volume)
    {
        // check if we should replace the valuable
        if (!ShouldSpawnItem(volume, out var itemType, out var hasSwitch)) return false;

        // check if itemType is null
        if (!itemType.HasValue) return false;

        // get a random item of the type
        if (!GetRandomItemOfType(itemType.Value, out var item)) return false;

        if (SemiFunc.IsMultiplayer())
        {
            PhotonNetwork.Instantiate("Items/" + item.name, volume.transform.position, volume.transform.rotation, 0);
        } else {
            Object.Instantiate(item.prefab, volume.transform.position, volume.transform.rotation);
        }

        Logger.LogInfo($"Spawned {item.name} with volume {volume.name}");
        return true;
    }

    [HarmonyPatch(typeof(ValuableDirector), "Spawn")]
    [HarmonyPrefix]
    public static void ValuableDirector_Spawn_Prefix(GameObject _valuable, ValuableVolume _volume, string _path)
    {
        _volume.gameObject.AddComponent<UsedVolumeTracker>();
    }

    [HarmonyPatch(typeof(ValuableDirector), nameof(ValuableDirector.VolumesAndSwitchSetup))]
    [HarmonyPostfix]
    public static void ValuableDirector_SetupHost_Postfix(ValuableDirector __instance)
    {
        var volumes = Object.FindObjectsOfType<ValuableVolume>(includeInactive: false).ToList()
            // only consider volumes that have not been used yet
            .Where(volume => volume.gameObject.GetComponent<UsedVolumeTracker>() == null)
            // only consider volumes that are not switches
            .Where(volume => !HasValuablePropSwitch(volume))
            // only consider tiny volumes for now
            .Where(volume => volume.VolumeType == ValuableVolume.Type.Tiny);

        Logger.LogInfo($"Found {volumes.Count()} potential volumes to spawn items in");

        int spawnedItems = 0;
        foreach (var volume in volumes)
        {
            if (RandomItemSpawn(volume)) spawnedItems++;
        }

        Logger.LogInfo($"Spawned {spawnedItems} items in total");
    }
}
