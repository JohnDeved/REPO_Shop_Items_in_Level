using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
namespace REPO_Shop_Items_in_Level;

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

        SpawnUpgradeItems = Config.Bind("UpgradeItems", "SpawnUpgradeItems", true, "Whether upgrade items can spawn in levels");
        UpgradeItemSpawnChance = Config.Bind("UpgradeItems", "UpgradeItemSpawnChance", 5f, "Percentage chance (0-100) for an upgrade item to spawn. Default is 5%.");
    }

    private static bool GetRandomItemOfType(SemiFunc.itemType itemType, out Item item)
    {
        var possibleItems = StatsManager.instance.itemDictionary.Values.Where(item => item.itemType == itemType).ToList();
        if (possibleItems.Count == 0)
        {
            Logger.LogWarning($"Failed to get random item of type {SemiFunc.itemType.item_upgrade}");
            item = null;
            return false;
        }

        item = possibleItems[Random.Range(0, possibleItems.Count)];
        return true;
    }

    private static bool ShouldReplaceValuable(ValuableVolume volume, out SemiFunc.itemType? itemType)
    {
        itemType = null;
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
    
    [HarmonyPatch(typeof(ValuableDirector), "Spawn")]
    [HarmonyPrefix]
    public static bool ValuableDirector_Spawn_Prefix(ref GameObject _valuable, ValuableVolume _volume, string _path)
    {
        // check if we should replace the valuable
        if (!ShouldReplaceValuable(_volume, out var itemType)) return true;

        // check if itemType is null
        if (!itemType.HasValue) return true;

        // get a random item of the type
        if (!GetRandomItemOfType(itemType.Value, out var item)) return true;

        // we override the valuable with the item
        _valuable = item.prefab;

        // check if we are in multiplayer mode
        if (GameManager.instance.gameMode != 0) {
            // if we are in multiplayer mode, we have to handle the network spawn ourselves
            PhotonNetwork.Instantiate("Items/" + _valuable.name, _volume.transform.position, _volume.transform.rotation, 0);
            Logger.LogInfo($"Highjacking Spawn for: {_valuable.name} with volume {_volume} at path {_path}");
            return false; // we do not want the original function to run, as we handled the spawn ourselves
        }

        Logger.LogInfo($"ValuableDirector spawning: {_valuable.name} with volume {_volume} at path {_path}");
        return true;
    }
}
