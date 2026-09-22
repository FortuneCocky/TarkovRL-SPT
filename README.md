# TarkovRL — Tarkov Real Life

A BepInEx plugin for **SPT 4.1.x** (Single Player Tarkov) that adds realistic weapon handling: procedural sway, aim/hip deadzones, weapon inertia, momentum, camera lag, turn lean, shot misalignment, reload efficiency, and leg cripple effects.

> **Inspired by and based on [TarkovIRL](https://github.com/crm85/TarkovIRL-public) by adishee.**
> The weapon deadzone system and several handling concepts were ported from TarkovIRL's Weapons Handling Mod. Full credit to adishee for the original work. This is a derivative port tuned for SPT 4.1.x with personal config defaults.

## What it does

TarkovRL overhauls weapon handling to feel more realistic and weighty. It replaces the vanilla rigid weapon pose with procedural motion driven by weapon weight, ergonomics, stamina, and player movement.

### Core systems

| System | Description |
|--------|-------------|
| **Aiming** | Scales ADS speed by weapon weight, ergonomics, and hands stamina. Light weapons aim faster, heavy weapons slower. |
| **Sway & Inertia** | Weapon sways based on weight, ergonomics, and stamina. Includes breathing sway, walk bob, fatigue micro-sway, muzzle tip amplification, inertia overshoot, and turn muzzle lead. |
| **ADS Deadzone** | Camera lags behind the gun while aiming. The weapon tracks the mouse inside a cone; the camera only follows once the weapon exits the cone. |
| **Hip Deadzone** | Procedural non-ADS deadzone with organic drift, mouse whip, directional tilt, strafe wrist roll, acceleration sway, idle wrist roll, and shoulder pivot tracking. |
| **Shot Misalignment** | Every shot nudges the weapon out of alignment, scaled by weight and ergonomics. Drained stamina increases misalignment. |
| **Camera Lag** | The head/camera trails the weapon's sway instead of sitting rigidly on it. |
| **Turn Lean** | Turning with the mouse leans the body into the turn. Camera and weapon bank instead of rotating level. |
| **Procedural Q/E Lean** | Adds extra camera and weapon roll when leaning with Q/E for a more pronounced body-lean feel. |
| **Momentum** | Weight and inertia-driven camera/weapon sway. Includes turn momentum, stop delay, and walk forward/side lean. |
| **Reload Efficiency** | Reload speed scales with ergonomics. Sprint reload penalty configurable. | | Forces crouch/crawl when leg health drops below a threshold. Applies to player and/or bots. |
| **Sprint Cancel** | Right-click while sprinting cancels sprint and immediately aims down sights. |

### Key features

- **Weapon deadzone** — direct camera cone-clamp deadzone ported from Tarkov Real Life. Camera stays locked inside a cone and follows the mouse only at the edge.
- **Procedural hip motion** — organic drift, mouse whip, directional tilt, strafe wrist roll, acceleration sway, idle wrist roll, and shoulder pivot tracking.
- **Enhanced sway layers** — breathing sway, walk bob rotation, fatigue micro-sway, muzzle tip amplification, inertia overshoot, and turn muzzle lead.
- **All values configurable** — every parameter has a config entry with acceptable value ranges.

## Installation

1. Download `TarkovRL-1.0.0.zip`
2. Extract the `BepInEx` folder into your SPT install root (merge with existing `BepInEx` folder)
3. Launch the game — config will be generated at `BepInEx/config/com.tarkovrl.cfg`

## Build

```bash
# From the project folder
dotnet build -c Release
```

The `.csproj` uses relative paths to reference SPT assemblies at `..\..\SPT 4.1.5\`. Adjust the paths if your SPT install is elsewhere.

Output: `bin/Release/netstandard2.1/TarkovRL.dll`

## Compatibility

- **SPT 4.1.x** (tested on 4.1.5)
- **BepInEx** with HarmonyX
- Client-only plugin — no server mod required
- Compatible with **TacticalStances** (TacticalStances layers on top of TarkovRL's procedural motion)

## Plugin metadata

```text
GUID:    com.tarkovrl
Name:    Tarkov Real Life
Version: 1.0.0
```

## Credits

- **adishee** ([github.com/crm85](https://github.com/crm85/TarkovIRL-public)) — Original author of [TarkovIRL - Weapons Handling Mod](https://forge.sp-tarkov.com/mod/1459/tarkovirl-weapons-handling-mod). The weapon deadzone system and several handling concepts in this mod were ported from TarkovIRL. Full credit for the original implementation goes to adishee.
- **SPT Team** — Single Player Tarkov, without which none of this would be possible.
- **BepInEx / HarmonyX** — The plugin and patching frameworks this mod builds on.

## License

MIT — see [LICENSE](LICENSE). The original TarkovIRL code ported into this project retains its attribution to adishee.
