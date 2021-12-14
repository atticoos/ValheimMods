# LicenseToKill

Prevents skill drain from a PVP death.

This works by tracking the most recent hits received by the player, and determining if one of the hits was dealt by another player.

Note: If a player already has a No Skill Drain buff, it will be removed upon a PVP death.

## Installation
- Un-zip `LicenseToKill.dll` to your `/Valheim/BepInEx/plugins/` folder

## Configuration
- `isModEnabled`
  - Globally enable or disable this mod (restart required)