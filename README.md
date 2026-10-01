# Tactical Door Wedge Mod

A tactical mod for SPTarkov that introduces deployable physical door wedges to barricade doors, manage room defense, and counter aggressive enemy breaches.

---

## Features

### 1. The Tactical Door Wedge Item
* **Item Name**: Tactical Door Wedge
* **Stack Size**: 2
* **Inventory Size**: 1x1 grid cell
* **Trader Sales**:
  * **Skier Level 3**: 15,000 Roubles
  * **Skier Level 2**: Barter for 1x Pack of nails
* **Hideout Craft**:
  * **Station**: Workbench Level 2
  * **Required Tool (Retained)**: 1x Toolset
  * **Consumed Ingredients**: 1x Piece of plexiglass, 1x Metal spare parts
  * **Duration**: 30 minutes
  * **Yield**: 2x Tactical Door Wedge
* **World Loot**:
  * Toolboxes
  * Technical supply crates
  * Jackets

---

## 2. In-Game Controls & Mechanics

* **Securing a Door**:
  * Approach any closed interior door within 2.5 meters.
  * Press **L** (configurable in F12 menu) with a wedge in your inventory.
  * The door locks shut and cannot be opened with the handle.
* **Retrieving a Wedge**:
  * Look at the wedged door and press **L**.
  * The wedge returns to your inventory and the door unlocks.
* **Breaching a Wedged Door**:
  * Kicking or breaching a wedged door applies damage to the wedge durability.
  * By default (100 HP wedge / 51 kick damage), a wedged door takes exactly 2 kicks to breach open.

---

## 3. F12 BepInEx Configuration

Press **F12** in-game to configure:
* **Wedge Health**: Durability of a wedged door (Range: 1 – 1000, Default: 100).
* **Kick Damage**: Damage dealt to wedge durability per kick (Range: 1 – 200, Default: 51).
* **Wedge / Retrieve Hotkey**: Keyboard shortcut to wedge/unwedge doors (Default: `L`).

---

## 4. Bot AI Intelligence

* **Regular Scavs**:
  * Kick a wedged door once.
  * If the door holds, scavs give up immediately.
  * The scav clears memory tracking of the player and returns to cautious wandering.
* **PMCs and Bosses**:
  * Kick a wedged door once.
  * If the door holds, the bot evaluates if an alternate route to the player exists.
  * If an alternate route exists: the bot flanks around.
  * If no alternate route exists: the bot kicks through until the wedge breaks.

---

## Installation

1. Copy `Server/bin/Debug/TacticalDoorWedgeServer/` to `user/mods/TacticalDoorWedge/` (or your SPT server mods directory).
2. Copy `Client/bin/Debug/TacticalDoorWedgeClient.dll` to `BepInEx/plugins/TacticalDoorWedge/`.
