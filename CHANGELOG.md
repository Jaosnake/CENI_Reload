# Changelog

## 1.0.0 — Initial Release of CENI_RELOAD

- **100% written from scratch** by Jaosnake under the **MIT License** — brand-new standalone project, codebase (`Jaosnake.CeniReload`), repository (`Jaosnake/CENI_Reload`), and package (`Jaosnake-CENI_RELOAD`).
- Standalone HarmonyX architecture with a single dependency (`bbepis-BepInExPack-5.4.2121`).
- Two-column **Checkpoint Vault** card UI accessible from both the **Main Menu** and the **Character Select lobby**, featuring **survivor portraits**, survivor/all filter, live slot inspector, auto-scrolling card list, and inline delete confirmation.
- **Missing Survivor Protection**: automatically detects checkpoints whose modded survivor is no longer installed, displays a clear warning badge, and disables loading to prevent broken runs.
- Full controller support with a quick lobby **Vault shortcut (`Y` / `Triangle`)**, dynamic controller glyphs (Xbox, PS4, PS5, Switch), dedicated modal `UILayerKey` focus isolation, and instant close via `ESC` / `B` / `Circle`.
- Independent `.cenireload.json` checkpoint format with name-based permanent & **DLC3 temporary item** serialization, equipment slots/sets, **loop counters & run event flags**, allies/drones, and 5 rotating slots per survivor per profile.
- Built-in English (`en`) and Brazilian Portuguese (`pt-BR`) localization (`CENIRELOAD_*` keys) with optional external JSON overrides (`ceni_reload.json`).
