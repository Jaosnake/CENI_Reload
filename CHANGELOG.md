# Changelog

## 1.0.2

- Browse and recover the previous checkpoint backup, check missing content before loading, and inspect a report of restored or skipped data.
- Prepare a co-op lobby and start with 3/4, 2/4 or only the host. Saved friends can join later once each, while new players start fresh; late joins use the current stage spawn.
- New saves retain more run random-generator state, health, shield, barrier and local skill charges/cooldowns. Remote skill state remains native in this version; older saves cannot recover data they never recorded.

## 1.0.1

- New checkpoints record player and ally positions and stage generation state. Loading reuses existing allies, leaves DroneTech skill drones to the game, and uses safe placement for allies in older saves; older saves cannot recover positions they never recorded.
- Loading resumes the saved loop and restores item quantities without retaining extra starting items. Missing survivor mods belonging only to absent co-op players no longer block the players present in the lobby.
- Local checkpoint updates preserve the previous file until replacement succeeds and retain a `.bak` copy, including when changing a slot's lock.

## 1.0.0 — Initial Release of CENI_RELOAD

- **100% written from scratch** by Jaosnake under the **MIT License** — brand-new standalone project, codebase (`Jaosnake.CeniReload`), repository (`Jaosnake/CENI_Reload`), and package (`Jaosnake-CENI_RELOAD`).
- Standalone HarmonyX architecture with a single dependency (`bbepis-BepInExPack-5.4.2121`).
- Two-column **Checkpoint Vault** card UI accessible from both the **Main Menu** and the **Character Select lobby**, featuring **survivor portraits**, survivor/all filter, live slot inspector, auto-scrolling card list, and inline delete confirmation.
- **Missing Survivor Protection**: automatically detects checkpoints whose modded survivor is no longer installed, displays a clear warning badge, and disables loading to prevent broken runs.
- Full controller support with a quick lobby **Vault shortcut (`Y` / `Triangle`)**, dynamic controller glyphs (Xbox, PS4, PS5, Switch), dedicated modal `UILayerKey` focus isolation, and instant close via `ESC` / `B` / `Circle`.
- Independent `.cenireload.json` checkpoint format with name-based permanent & **DLC3 temporary item** serialization, equipment slots/sets, **loop counters & run event flags**, allies/drones, and **5 rotating slots per survivor** (plus **5 dedicated Multiplayer Co-op slots**).
- **Stage 2+ Auto-Save with Loop Protection**: skips initial Stage 1 so testing survivors, mods, or artifacts never clutters your slots, while always auto-saving from Stage 2 onward and across all Loops (Stage 6+).
- **Slot Lock Padlock (`X` / `Square` / Right-Click)**: lock any checkpoint with a glowing golden RoR2-style 2D padlock icon to prevent overwrite or deletion, with an explicit Vault & chat warning if all 5 slots are locked.
- **Host-Only Multiplayer Co-op & Optional Cloud Storage**: matches co-op players by SteamID/EOS identity (absent players are skipped safely, new players start fresh, Inspector shows live lobby presence), plus opt-in **Steam / Epic Cloud Storage** (`UseCloudStorage` & `CloudStorageSubDirectory`).
- **Extra Modes, Artifacts & Color-Coded Vault UI**: saves and restores **Simulacrum** waves/enemy inventory, **Artifact of Evolution** monster items, and **Artifact of Devotion** minion inventory; Vault list and cards automatically color-code runs (**Blue** = Singleplayer, **Red** = Multiplayer, **Green** = Simulacrum / Evolution / Devotion).
- Built-in English (`en`) and Brazilian Portuguese (`pt-BR`) localization (`CENIRELOAD_*` keys) with optional external JSON overrides (`ceni_reload.json`).
