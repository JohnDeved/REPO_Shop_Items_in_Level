# 🛒 REPO Shop Items Spawn in Level

![R E P O](https://github.com/user-attachments/assets/11f842b2-cf3f-4f8f-9df7-52eefbc8cdf7)

## ✨ Overview
This mod enhances gameplay by allowing upgrade items and drones (normally only available in shops) to spawn throughout levels as findable loot. Hunt for valuable upgrade items as you explore!

Enemies now also have a chance to drop a health upgrade when killed.

## 🕹️ Multiplayer Friendly
This mod is designed to work seamlessly in multiplayer. Only the host needs to have the mod installed for all players to benefit from the new Features that it brings. This means you can enjoy the excitement of hunting for rare upgrade items together with your friends, without requiring everyone to install the mod.

## 📦 Extraction Point Integration
Found a valuable upgrade or drone during your run? Bring it to any extraction point, and it will be sucked up and automatically added to your bought Items! The item will be waiting for you in the Lobby Level to use in your next run.

![image](https://github.com/user-attachments/assets/6a612b10-398c-45fe-b5bb-c5261456531e)

## ⚙️ Configuration
Configure the mod through the BepInEx config file.
It is recommended to use the [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) UI for easier configuration in-game.

![image](https://github.com/user-attachments/assets/75043405-666a-404f-ab73-e073d63ff673)

### Upgrade Items
| Setting | Description | Default |
|---------|-------------|---------|
| `SpawnUpgradeItems` | Enable/disable upgrade items spawning in levels | `true` |
| `MapHideShopUpgradeItems` | Whether upgrade items are hidden on the map (Client Only) | `true` |
| `UpgradeItemSpawnChance` | Percentage chance (0-100) for upgrade items to spawn on a valid spawn point | `2.5%` |
| `UseShopPriceForItemSelection` | If ON: Cheaper upgrade items appear more often. If OFF: All upgrade items have equal chance | `true` |

### Drone Items
| Setting | Description | Default |
|---------|-------------|---------|
| `SpawnDroneItems` | Enable/disable drone items spawning in levels | `true` |
| `MapHideDroneItems` | Whether drone items are hidden on the map (Client Only) | `true` |
| `DroneItemsSpawnChance` | Percentage chance (0-100) for drone items to spawn on a valid spawn point | `0.95%` |
| `UseShopPriceForItemSelection` | If ON: Cheaper drone items appear more often. If OFF: All drone items have equal chance | `true` |

### Item Allow List
The mod supports controlling which specific upgrade items can spawn in levels. In the config file, you'll find two sections:
- `[AllowedItems Upgrades]` - Controls which upgrade items can spawn
- `[AllowedItems Drones]` - Controls which drone items can spawn

Each item has its own toggle to enable or disable spawning. These lists include all available items (including modded items) that could possibly spawn.

Please note that you have to start the game with the mod installed at least once to generate the configuration options.

## ⚠️ Known Issues
- Currently only supports item upgrades and drones (more item types coming in future updates)
- Please report any issues [here](https://github.com/JohnDeved/REPO_Shop_Items_in_Level/issues)

## ❓ Troubleshooting

### [No config file](https://github.com/JohnDeved/REPO_Shop_Items_in_Level/issues/7)
If you do not see the configuration file, follow these steps:

1. Start the game with the mod installed at least once to generate the configuration file.
2. Check the `\BepInEx\config` directory for the file named `REPO_Shop_Items_in_Level.cfg`.
3. Ensure you are using mod version 1.1.2 or higher, as the configuration file is only generated for these versions.
4. If the configuration file is still not generated, verify the mod version and update if necessary.

## 📝 Version History
- **1.7.19**: Fixed extraction point freeze issue
- **1.7.18**: Updated for 0.3 Monster update
- **1.7.15**: Added extraction point integration - items are now properly processed and saved when brought to extraction points within the level
- **1.6.15**: Added per-item-type configuration for item selection method (shop price-based or equal chance)
- **1.5.15**: fixed bug where cart would not show up in the map anymore
- **1.5.13**: implemented support for drone items
- **1.4.9**: implemented configs for item spawn allow list (supports modded items aswell)
- **1.4.8**: implemented config for hiding items on the map
- **1.4.6**: Added many new possible spawn locations and refactored spawn logic to be non-destructive (will not replace valuable items anymore)
- **1.3.5**: Fixed issue where Upgrades would occasionally spawn in inaccessible locations
- **1.2.5**: Added support for [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) ui
- **1.1.2**: Added configurable spawn rates for upgrade items
- **1.0.0**: Initial release

## 👤 Credits
Created by JohnDeved (undefined)  
Discord: can_not_read_properties_of

## 🔗 Links
- [GitHub Repository](https://github.com/JohnDeved/REPO_Shop_Items_in_Level)
- [Thunderstore](https://thunderstore.io/c/repo/p/itsUndefined/Shop_Items_Spawn_in_Level/)
- [Bug Reports](https://github.com/JohnDeved/REPO_Shop_Items_in_Level/issues)
