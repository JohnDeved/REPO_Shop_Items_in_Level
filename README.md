# 🛒 REPO Shop Items Spawn in Level

![R E P O](https://github.com/user-attachments/assets/11f842b2-cf3f-4f8f-9df7-52eefbc8cdf7)

## ✨ Overview
This mod enhances gameplay by allowing upgrade items and drones (normally only available in shops) to spawn throughout levels as findable loot. Hunt for valuable upgrades as you explore!

## 🔑 Key Features
- **Multiplayer Friendly**: Only the host needs the mod installed
- **Configurable**: Adjust spawn rates and toggle features to your preference

## ⚙️ Configuration
Configure the mod through the BepInEx config file:

## ⚙️ Configuration
Configure the mod through the BepInEx config file:

### Upgrade Items
| Setting | Description | Default |
|---------|-------------|---------|
| `SpawnUpgradeItems` | Enable/disable upgrade items spawning in levels | `true` |
| `MapHideShopUpgradeItems` | Whether upgrade items are hidden on the map (Client Only) | `true` |
| `UpgradeItemSpawnChance` | Percentage chance (0-100) for upgrade items to spawn on a valid spawn point | `2.5%` |

### Drone Items
| Setting | Description | Default |
|---------|-------------|---------|
| `SpawnDroneItems` | Enable/disable drone items spawning in levels | `true` |
| `MapHideDroneItems` | Whether drone items are hidden on the map (Client Only) | `true` |
| `DroneItemsSpawnChance` | Percentage chance (0-100) for drone items to spawn on a valid spawn point | `0.95%` |

### Item Allow List
The mod supports controlling which specific upgrade items can spawn in levels. In the config file, you'll find two sections:
- `[AllowedItems Upgrades]` - Controls which upgrade items can spawn
- `[AllowedItems Drones]` - Controls which drone items can spawn

Each item has its own toggle to enable or disable spawning. These lists include all available items (including modded items) that could possibly spawn.

Please note that you have to start the game with the mod installed at least once to generate the configuration options.

## 🔮 Planned Features
- Support for additional item types
- Custom item distributions based on level difficulty
- Additional configuration options

## ⚠️ Known Issues
- Currently only supports item upgrades and drones (more item types coming in future updates)
- Please report any issues [here](https://github.com/JohnDeved/REPO_Shop_Items_in_Level/issues)

## 🛠️ Troubleshooting

### [No config file](https://github.com/JohnDeved/REPO_Shop_Items_in_Level/issues/7)
If you do not see the configuration file, follow these steps:

1. Start the game with the mod installed at least once to generate the configuration file.
2. Check the `\BepInEx\config` directory for the file named `REPO_Shop_Items_in_Level.cfg`.
3. Ensure you are using mod version 1.1.2 or higher, as the configuration file is only generated for these versions.
4. If the configuration file is still not generated, verify the mod version and update if necessary.

## 📝 Version History
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
