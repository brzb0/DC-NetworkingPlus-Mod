# Networking Plus

A MelonLoader mod for [Data Center](https://store.steampowered.com/app/4170200/Data_Center/) that adds custom Router and Firewall devices with 32 QSFP+ ports.

**Repository:** https://github.com/brzb0/DC-NetworkingPlus-Mod

**Releases:** https://github.com/brzb0/DC-NetworkingPlus-Mod/releases

## Features

- **Router QSFP+** — Custom router device with routing table and ASN configuration
- **Firewall QSFP+** — Custom firewall device with filter rules and cluster IP support
- **Color-coded** — Router is green, Firewall is dark (#1B1B1B) for easy identification
- **Shop integration** — Buyable from the shop in the "HL Mods" section
- **Config menus** — Opens router/firewall configuration UI when clicking the device

## Requirements

- [MelonLoader](https://melonwiki.xyz/) v0.7.2 or newer
- Data Center (Unity/IL2CPP)

## Installation

1. Install MelonLoader for Data Center if you haven't already
2. Download `NetworkingPlus.dll` from [Releases](../../releases)
3. Drop `NetworkingPlus.dll` into your `Data Center/Mods/` folder
4. Launch the game — the devices appear in the shop immediately

## Shop Items

| Device         | Price  | Color     |
|----------------|--------|-----------|
| Router QSFP+   | $13,500 | Green    |
| Firewall QSFP+ | $13.500 | Dark (#1B1B1B) |

## Building from Source

```bash
dotnet build -c Release
```

Reference DLLs are in the `lib/` folder (MelonLoader, Harmony, Unity, game assemblies). The DLL is auto-copied to `Data Center/Mods/` after build when the game is installed locally.

## How It Works

The mod uses Harmony to patch the Data Center game at runtime:

- **`SpawnPhysicalItem`** — Replaces the vanilla Router/Firewall prefab with the custom device at spawn time
- **`ButtonBuyShopItem`** — Handles the full buy flow for custom items (spawn, cart, money)
- **`ButtonShowNetworkSwitchConfig`** — Redirects to router/firewall config UI
- **`SwitchInsertedInRack`** — Sets the device label after rack insertion
- **`UpdateDisplay`** — Fixes cart display for custom items (name, quantity, price)
- **`ButtonExtended`** — Disables `doSubmitOnSelect` to prevent duplicate spawning

Custom devices are registered in `routersPrefabs[]` and `firewallsPrefabs[]` arrays with inactive templates for save/load persistence.

## Project Structure

```
NetworkingPlus/
├── README.md
├── NetworkingPlus.sln
├── .gitignore
├── .github/workflows/build.yml    ← CI/CD
├── lib/                            ← Reference DLLs (MelonLoader, Unity, Game)
└── DataCenter-NetworkingPlus/
    ├── NetworkingPlus.csproj
    ├── Core.cs                     — Mod entry point, prefab building, shop injection, tinting
    ├── DeviceDefinition.cs         — Device definitions (names, colors, prices)
    ├── DeviceRegistry.cs           — Custom device ID registry
    └── Patches.cs                  — Harmony patches and CartButtonHandler
```

## Credits

- **Brzb02** — Author
- Repository: https://github.com/brzb0/DC-NetworkingPlus-Mod
- Built with [MelonLoader](https://melonwiki.xyz/) and [HarmonyLib](https://github.com/pardeike/Harmony)
