﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
namespace REPO_Shop_Items_in_Level;

public class SwitchPlayerUpgradeTracker: MonoBehaviour {}

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

    private static bool ShouldReplaceValuable(ValuableVolume volume, out SemiFunc.itemType? itemType)
    {
        itemType = null;
        if (HasValuablePropSwitch(volume)) return false;

        switch (volume.VolumeType)
        {
            // tiny space = item upgrades
            case ValuableVolume.Type.Tiny:
                if (!SpawnUpgradeItems.Value) return false;
                itemType = SemiFunc.itemType.item_upgrade;
                return Random.Range(0f, 100f) < UpgradeItemSpawnChance.Value;

            default:
                return false;
        }
    }


    private static bool HasValuablePropSwitch(ValuableVolume volume)
    {
        return volume.transform.GetComponentInParent<ValuablePropSwitch>() != null;
    }

    [HarmonyPatch(typeof(ValuableDirector), "Spawn")]
    [HarmonyPrefix]
    public static bool ValuableDirector_Spawn_Prefix(ref GameObject _valuable, ValuableVolume _volume, string _path)
    {
        // debug
        // var field = typeof(EnemyDirector).GetField("debugNoVision", BindingFlags.NonPublic | BindingFlags.Instance);
        // field.SetValue(EnemyDirector.instance, true);

        // check if we should replace the valuable
        if (!ShouldReplaceValuable(_volume, out var itemType)) return true;

        // check if itemType is null
        if (!itemType.HasValue) return true;

        // get a random item of the type
        if (!GetRandomItemOfType(itemType.Value, out var item)) return true;

        // we override the valuable with the item
        _valuable = item.prefab;

        if (SemiFunc.IsMultiplayer())
        {
            // if we are in multiplayer mode, we have to handle the network spawn ourselves
            PhotonNetwork.Instantiate("Items/" + _valuable.name, _volume.transform.position, _volume.transform.rotation, 0);
            Logger.LogInfo($"Highjacking Spawn for: {_valuable.name} with volume {_volume.name} at path {_path}");
            return false; // we do not want the original function to run, as we handled the spawn ourselves
        }

        Logger.LogInfo($"ValuableDirector spawning: {_valuable.name} with volume {_volume.name} at path {_path}");
        return true;
    }

    // todo: check if all players have the mod installed so we can add the tracker component
    private static void AddSwitchTrackerComponent(ValuableVolume volume)
    {
        var switches = volume.transform.GetComponentsInParent<ValuablePropSwitch>(true);

        foreach (var switchObj in switches)
        {
            Logger.LogInfo($"Switch: {switchObj.gameObject.name}");
            switchObj.gameObject.AddComponent<SwitchPlayerUpgradeTracker>();
        }
    }

    // dead code
    // [HarmonyPatch(typeof(ValuablePropSwitch), nameof(ValuablePropSwitch.Setup))]
    // [HarmonyPostfix]
    public static void ValuablePropSwitch_Setup_Posfix(ValuablePropSwitch __instance)
	{
        FieldInfo setupCompleteField = typeof(ValuablePropSwitch).GetField("SetupComplete", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        bool SetupComplete = (bool)setupCompleteField.GetValue(__instance);
        if (!SetupComplete) return;

		var switchPlayerUpgradeComponent = __instance.gameObject.GetComponent<SwitchPlayerUpgradeTracker>();
		if (!switchPlayerUpgradeComponent) return;
		
        __instance.PropParent.SetActive(value: false);
        __instance.ValuableParent.SetActive(value: true);
        
        Logger.LogInfo($"ValuablePropSwitch found UpgradeTracker: {__instance.gameObject.name}");
	}
}
