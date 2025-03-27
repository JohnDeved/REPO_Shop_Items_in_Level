using System.Collections.Generic;
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

public class UsedVolumeTracker : MonoBehaviour { }

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("REPO.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> SpawnUpgradeItems;
    internal static ConfigEntry<bool> MapHideShopUpgradeItems;
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
        MapHideShopUpgradeItems = Config.Bind("UpgradeItems", "MapHideShopUpgradeItems", true, new ConfigDescription("Whether upgrade items are hidden on the map"));
        UpgradeItemSpawnChance = Config.Bind("UpgradeItems", "UpgradeItemSpawnChance", 2.5f, new ConfigDescription("% chance for an upgrade item to spawn", new AcceptableValueRange<float>(0.0f, 100.0f)));
    }

    private static List<ConfigEntry<bool>> GetDisallowedItems()
    {
        var blackListedItems = new List<ConfigEntry<bool>>();
        foreach (var item in StatsManager.instance.itemDictionary.Values)
        {
            if (item.itemType != SemiFunc.itemType.item_upgrade) continue; // only consider upgrade items for now
            var configEntry = Instance.Config.Bind("AllowedItems", item.name, true, new ConfigDescription("Whether this item can spawn in levels"));
            if (!configEntry.Value) blackListedItems.Add(configEntry);
        }
        return blackListedItems;
    }

    [HarmonyPatch(typeof(StatsManager), "LoadItemsFromFolder")]
    [HarmonyPostfix]
    public static void StatsManager_LoadItemsFromFolder_Postfix(StatsManager __instance)
    {
        GetDisallowedItems(); // initialize the config entries
    }

    private static bool GetRandomItemOfType(SemiFunc.itemType itemType, out Item item)
    {
        var blackListedItems = GetDisallowedItems();
        var possibleItems = StatsManager.instance.itemDictionary.Values
            // only consider items of the given type
            .Where(item => item.itemType == itemType)
            // only consider items that are not blacklisted
            .Where(item => !blackListedItems.Any(configEntry => configEntry.Definition.Key == item.name && !configEntry.Value))
            .ToList();

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
        }
        else
        {
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
        if (!SemiFunc.RunIsLevel()) return;

        var volumes = Object.FindObjectsOfType<ValuableVolume>(includeInactive: false).ToList()
            // only consider volumes that have not been used yet
            .Where(volume => volume.gameObject.GetComponent<UsedVolumeTracker>() == null)
            // only consider volumes that are not switches
            .Where(volume => !HasValuablePropSwitch(volume))
            // only consider tiny volumes for now
            .Where(volume => volume.VolumeType == ValuableVolume.Type.Tiny);

        Logger.LogInfo($"Found {volumes.Count()} potential volumes to spawn items in");
        Logger.LogInfo($"Upgrade item spawn chance: {UpgradeItemSpawnChance.Value}%");

        int spawnedItems = 0;
        foreach (var volume in volumes)
        {
            if (RandomItemSpawn(volume)) spawnedItems++;
        }

        Logger.LogInfo($"Spawned {spawnedItems} items in total");
    }

    [HarmonyPatch(typeof(Map), nameof(Map.AddCustom))]
    [HarmonyPostfix]
    public static void Map_AddCustom_Postfix(MapCustom mapCustom)
    {
        if (!SemiFunc.RunIsLevel()) return;

        if (!mapCustom.gameObject.TryGetComponent<ItemAttributes>(out var itemAttributes)) return;

        // exit if we are not hiding upgrade items
        if (!MapHideShopUpgradeItems.Value && itemAttributes.item.itemType == SemiFunc.itemType.item_upgrade) return;

        mapCustom.mapCustomEntity.gameObject.SetActive(false);
    }
}
