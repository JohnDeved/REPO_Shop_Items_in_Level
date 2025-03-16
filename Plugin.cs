using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
namespace REPO_Shop_Items_in_Level;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("REPO.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
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
    }

    [HarmonyPatch(typeof(ValuableDirector), nameof(ValuableDirector.SetupHost))]
    [HarmonyPrefix]
    public static void ValuableDirector_SetupHost(ValuableDirector __instance)
    {
        // if the level has no enemies, we can skip
        if (!LevelGenerator.Instance.Level.HasEnemies)
        {
            return;
        }

        // check if the level has valuables
        if (LevelGenerator.Instance.Level.ValuablePresets.Count == 0)
        {
            Logger.LogInfo("ValuablePresets is 0");
            return;
        }

        LevelValuables valuablePreset = LevelGenerator.Instance.Level.ValuablePresets[0];

        Logger.LogInfo($"Valuable preset: {valuablePreset.name}");

        foreach (var item in StatsManager.instance.itemDictionary.Values)
        {
            // lets hardcode the item upgrade for now, later we support more with config
            if (item.itemType == SemiFunc.itemType.item_upgrade)
            {
                switch (item.itemVolume)
                {
                    case SemiFunc.itemVolume.upgrade:
                        // we check if the item is already in the list
                        if (valuablePreset.tiny.Contains(item.prefab))
                        {
                            Logger.LogWarning($"Item {item.prefab.name} is already in the list");
                            continue;
                        }
                        valuablePreset.tiny.Add(item.prefab);
                        break;
                    
                    default:
                        Logger.LogWarning($"Unsupported itemVolume: {item.itemVolume}");
                        continue;
                }

                Logger.LogInfo($"Added {item.prefab.name} to {valuablePreset.name}");
            }
        }
    }
    
    [HarmonyPatch(typeof(ValuableDirector), "Spawn")]
    [HarmonyPrefix]
    public static bool ValuableDirector_Spawn_Prefix(GameObject _valuable, ValuableVolume _volume, string _path)
    {
        // check if we are in multiplayer mode
        if (GameManager.instance.gameMode != 0) {
            // check if the item is part of the itemDictionary, if so, we have to handle multiplayer spawn ourselves
            if (StatsManager.instance.itemDictionary.ContainsKey(_valuable.name))
            {
                Logger.LogInfo($"Highjacking Spawn for: {_valuable.name} with volume {_volume} at path {_path}");

                PhotonNetwork.Instantiate("Items/" + _valuable.name, _volume.transform.position, _volume.transform.rotation, 0);
                // we do not want the original function to run
                return false;
            }
        }

        Logger.LogInfo($"ValuableDirector spawning: {_valuable.name} with volume {_volume} at path {_path}");
        return true;
    }
}
