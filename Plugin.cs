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
public class SpawnedItemTracker : MonoBehaviour { }

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("REPO.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    internal static ConfigEntry<bool> SpawnUpgradeItems;
    internal static ConfigEntry<bool> MapHideUpgradeItems;
    internal static ConfigEntry<float> UpgradeItemSpawnChance;
    internal static ConfigEntry<bool> UseShopPriceForUpgradeItems;

    internal static ConfigEntry<bool> SpawnDroneItems;
    internal static ConfigEntry<bool> MapHideDroneItems;
    internal static ConfigEntry<float> DroneItemSpawnChance;
    internal static ConfigEntry<bool> UseShopPriceForDroneItems;

    internal static List<ConfigEntry<bool>> DisallowedItems;

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
        // harmony.PatchAll(typeof(DespawnPatch));

        Logger.LogInfo("Harmony patches applied!");

        SpawnUpgradeItems = Config.Bind("UpgradeItems", "SpawnUpgradeItems", true, new ConfigDescription("Whether upgrade items can spawn in levels"));
        MapHideUpgradeItems = Config.Bind("UpgradeItems", "MapHideShopUpgradeItems", true, new ConfigDescription("(Client) Whether upgrade items are hidden on the map"));
        UpgradeItemSpawnChance = Config.Bind("UpgradeItems", "UpgradeItemSpawnChance", 2.5f, new ConfigDescription("% chance for an upgrade item to spawn", new AcceptableValueRange<float>(0.0f, 100.0f)));
        UseShopPriceForUpgradeItems = Config.Bind("UpgradeItems", "UseShopPriceForItemSelection", true, new ConfigDescription("If ON: Cheaper upgrade items appear more often. If OFF: All upgrade items have equal chance."));

        SpawnDroneItems = Config.Bind("DroneItems", "SpawnDroneItems", true, new ConfigDescription("Whether drone items can spawn in levels"));
        MapHideDroneItems = Config.Bind("DroneItems", "MapHideDroneItems", true, new ConfigDescription("(Client) Whether drone items are hidden on the map"));
        DroneItemSpawnChance = Config.Bind("DroneItems", "DroneItemsSpawnChance", 0.95f, new ConfigDescription("% chance for a drone item to spawn", new AcceptableValueRange<float>(0.0f, 100.0f)));
        UseShopPriceForDroneItems = Config.Bind("DroneItems", "UseShopPriceForItemSelection", true, new ConfigDescription("If ON: Cheaper drone items appear more often. If OFF: All drone items have equal chance."));
    }

    // [HarmonyPatch(typeof(StatsManager), "LoadItemsFromFolder")]
    // changed to mainmenu load because of modded items
    [HarmonyPatch(typeof(MainMenuOpen), "Awake")]
    [HarmonyPostfix]
    public static void MainMenuOpen_Awake_Postfix(StatsManager __instance)
    {
        if (DisallowedItems != null) return;
        Logger.LogInfo("Initializing disallowed items list");
        DisallowedItems = new List<ConfigEntry<bool>>();
        foreach (var item in StatsManager.instance.itemDictionary.Values)
        {
            ConfigEntry<bool> configEntry;
            switch (item.itemType)
            {
                case SemiFunc.itemType.item_upgrade:
                    // check if config entry already exists
                    configEntry = Instance.Config.Bind("AllowedItems Upgrades", item.name, true,
                        new ConfigDescription("Whether this upgrade item can spawn in levels"));
                    break;
                case SemiFunc.itemType.drone:
                    configEntry = Instance.Config.Bind("AllowedItems Drones", item.name, true,
                        new ConfigDescription("Whether this drone item can spawn in levels"));
                    break;
                default:
                    continue; // Skip other item types
            }

            if (!configEntry.Value) DisallowedItems.Add(configEntry);
        }
    }

    private static bool GetRandomItemOfType(SemiFunc.itemType itemType, out Item item)
    {
        item = null;

        // --- 1. Filter Candidate Items ---
        var possibleItems = StatsManager.instance.itemDictionary.Values
            .Where(i => i.itemType == itemType) // Type check
            .Where(i => i.value != null && i.value.valueMin > 0f) // Validity & Value check
            .Where(i => !DisallowedItems.Any(cfg => cfg.Definition.Key == i.name && !cfg.Value)) // Blacklist
            .ToList();

        // --- 2. Handle No Valid Items Found ---
        if (possibleItems.Count == 0)
        {
            Logger.LogWarning($"GetRandomItemOfType: No valid items found for type {itemType} after filtering.");
            return false;
        }

        bool useWeightedSelection = false;

        // Determine which weighted selection setting to use based on item type
        switch (itemType)
        {
            case SemiFunc.itemType.item_upgrade:
                useWeightedSelection = UseShopPriceForUpgradeItems.Value;
                break;
            case SemiFunc.itemType.drone:
                useWeightedSelection = UseShopPriceForDroneItems.Value;
                break;
        }

        if (useWeightedSelection)
        {
            // --- Weighted Selection based on shop price ---

            // --- 3. Calculate Total Weight ---
            float totalWeight = possibleItems.Sum(i => 1.0f / i.value.valueMin);

            // --- 4. Handle Invalid Total Weight (Edge Case) ---
            if (totalWeight <= 0f || float.IsNaN(totalWeight) || float.IsInfinity(totalWeight))
            {
                Logger.LogWarning($"GetRandomItemOfType: Invalid total weight {totalWeight} for type {itemType}. " +
                                  "This may indicate an issue with item values or weights.");
                return false;
            }

            // --- 5. Perform Weighted Random Selection ---
            float randomRoll = Random.Range(0f, totalWeight);

            foreach (var currentItem in possibleItems)
            {
                float itemWeight = 1.0f / currentItem.value.valueMin;
                randomRoll -= itemWeight;

                if (randomRoll <= 0f)
                {
                    item = currentItem;
                    break;
                }
            }

            // --- 6. Handle Float Precision (Edge Case) ---
            if (item == null)
            {
                Logger.LogWarning($"GetRandomItemOfType: Weighted selection loop for type {itemType} completed unexpectedly " +
                                  "without selecting an item. This may indicate a precision issue.");
                return false;
            }

            // Calculate the probability for the selected item
            // Probability = (Item's Weight) / (Total Weight)
            float itemChancePercent = (1.0f / item.value.valueMin) / totalWeight * 100.0f;
            Logger.LogInfo($"Selecting {item.name} at a chance of {itemChancePercent:F2}% compared to others of type {itemType} (based on shop price)");
        }
        else
        {
            // --- Equal chance for all items ---
            int randomIndex = Random.Range(0, possibleItems.Count);
            item = possibleItems[randomIndex];
            Logger.LogInfo($"Selecting {item.name} at a chance of {100.0f / possibleItems.Count:F2}% compared to others of type {itemType} (equal chance)");
        }

        return true; // Successfully selected an item
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

            case ValuableVolume.Type.Small:
                if (!SpawnDroneItems.Value) return false;
                itemType = SemiFunc.itemType.drone;
                return Random.Range(0f, 100f) <= DroneItemSpawnChance.Value;

            default:
                return false;
        }
    }

    private static GameObject SpawnItem(Item item, Vector3 position, Quaternion rotation)
    {
        GameObject spawnedObj;
        
        if (SemiFunc.IsMultiplayer())
        {
            spawnedObj = PhotonNetwork.Instantiate("Items/" + item.name, position, rotation, 0);
        }
        else
        {
            var prefab = Resources.Load<GameObject>("Items/" + item.name);
            if (prefab == null) return null;
            spawnedObj = Object.Instantiate(prefab, position, rotation);
        }
        
        // Add tracker component to the spawned item
        spawnedObj.AddComponent<SpawnedItemTracker>();
        return spawnedObj;
    }

    private static bool RandomItemSpawn(ValuableVolume volume)
    {
        // check if we should replace the valuable
        if (!ShouldSpawnItem(volume, out var itemType, out var hasSwitch)) return false;

        // check if itemType is null
        if (!itemType.HasValue) return false;

        // get a random item of the type
        if (!GetRandomItemOfType(itemType.Value, out var item)) return false;

        SpawnItem(item, volume.transform.position, volume.transform.rotation);

        return true;
    }

    // [HarmonyPatch(typeof(EnemyParent), "Despawn")]
    // class DespawnPatch
    // {
    //     static int GetEnemySpawnValuableCurrent(EnemyParent enemyParent)
    //     {
    //         // Access Enemy
    //         var enemy = AccessTools.Field(typeof(EnemyParent), "Enemy")?.GetValue(enemyParent) as Enemy;
    //         if (enemy == null)
    //         {
    //             Logger.LogWarning($"Failed to access Enemy from EnemyParent {enemyParent.name}.");
    //             return 0;
    //         }

    //         // Access Health
    //         var health = AccessTools.Field(typeof(Enemy), "Health")?.GetValue(enemy) as EnemyHealth;
    //         if (health == null)
    //         {
    //             Logger.LogWarning($"Failed to access Health from Enemy {enemyParent.name}.");
    //             return 0;
    //         }

    //         // Access spawnValuableCurrent
    //         var spawnValuableField = AccessTools.Field(typeof(EnemyHealth), "spawnValuableCurrent");
    //         if (spawnValuableField == null)
    //         {
    //             Logger.LogWarning($"Field spawnValuableCurrent not found in EnemyHealth for enemy {enemyParent.name}.");
    //             return 0;
    //         }

    //         return (int)spawnValuableField.GetValue(health);
    //     }

    //     // Store valuable count before method execution
    //     static void Prefix(EnemyParent __instance, out int __state)
    //     {
    //         __state = GetEnemySpawnValuableCurrent(__instance);
    //     }

    //     // Check if valuable count increased after method execution
    //     static void Postfix(EnemyParent __instance, int __state)
    //     {
    //         if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
    //         // If valuable spawned (count increased), spawn our additional item
    //         if (GetEnemySpawnValuableCurrent(__instance) > __state)
    //         {
    //             Logger.LogInfo($"Enemy {__instance.name} spawned a valuable!");
    //             if (GetRandomItemOfType(SemiFunc.itemType.healthPack, out var item)) {
    //                 // Spawn the item at the enemy's position
    //                 var position = __instance.transform.position + new Vector3(0, 1, 0);
    //                 var rotation = Quaternion.identity;
    //                 SpawnItem(item, position, rotation);
    //             }
    //         }
    //     }
    // }

    [HarmonyPatch(typeof(ValuableDirector), "Spawn")]
    [HarmonyPrefix]
    public static void ValuableDirector_Spawn_Prefix(GameObject _valuable, ValuableVolume _volume, string _path)
    {
        _volume.gameObject.AddComponent<UsedVolumeTracker>();
    }

    [HarmonyPatch(typeof(ValuableDirector), nameof(ValuableDirector.VolumesAndSwitchSetup))]
    [HarmonyPostfix]
    public static void ValuableDirector_VolumesAndSwitchSetup_Postfix(ValuableDirector __instance)
    {
        if (!SemiFunc.RunIsLevel()) return;

        var volumes = Object.FindObjectsOfType<ValuableVolume>(includeInactive: false).ToList()
            // only consider volumes that have not been used yet
            .Where(volume => volume.gameObject.GetComponent<UsedVolumeTracker>() == null)
            // only consider volumes that are not switches
            .Where(volume => !HasValuablePropSwitch(volume));

        Logger.LogInfo($"Found {volumes.Count()} potential volumes to spawn items in");
        Logger.LogInfo($"Upgrade item spawn chance: {UpgradeItemSpawnChance.Value}% on {volumes.Where(volume => volume.VolumeType == ValuableVolume.Type.Tiny).Count()} tiny volumes");
        Logger.LogInfo($"Drone item spawn chance: {DroneItemSpawnChance.Value}% on {volumes.Where(volume => volume.VolumeType == ValuableVolume.Type.Small).Count()} small volumes");

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

        // Only hide map entities for specific item types if configured
        switch (itemAttributes.item.itemType)
        {
            case SemiFunc.itemType.item_upgrade when MapHideUpgradeItems.Value:
            case SemiFunc.itemType.drone when MapHideDroneItems.Value:
                mapCustom.mapCustomEntity.gameObject.SetActive(false);
                break;
        }
    }

    [HarmonyPatch(typeof(ExtractionPoint), "DestroyAllPhysObjectsInHaulList")]
    [HarmonyPostfix]
    public static void ExtractionPoint_DestroyAllPhysObjectsInHaulList_Postfix(ExtractionPoint __instance)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

        // get all spawned items
        var spawnedItemGameObjects = Object.FindObjectsOfType<SpawnedItemTracker>(includeInactive: false)
            .Select(tracker => tracker.gameObject)
            .ToList();

        foreach (var gameObject in spawnedItemGameObjects)
        {
            var roomVolumeCheck = gameObject.GetComponent<RoomVolumeCheck>();
            if (roomVolumeCheck == null) continue;
            if (roomVolumeCheck.CurrentRooms.Any(room => room.Extraction)) {
                var itemAttr = gameObject.GetComponent<ItemAttributes>();

                Logger.LogInfo($"Adding item {gameObject.name} to purchased items");
                StatsManager.instance.ItemPurchase(itemAttr.item.name);

                Logger.LogInfo($"Destroying spawned item {gameObject.name} in extraction point {__instance.name}");
                gameObject.GetComponent<PhysGrabObject>().DestroyPhysGrabObject();
            }
        }
    }
}

