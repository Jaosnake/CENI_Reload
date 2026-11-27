using System;
using System.Collections.Generic;
using System.IO;
using RoR2;
using SimpleJSON;
using IOPath = System.IO.Path;

namespace Jaosnake.CeniReload.Localization
{
    public static partial class CeniReloadStrings
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CENIRELOAD_BTN_OPEN_VAULT"] = "CENI RELOAD VAULT",
            ["CENIRELOAD_MAINMENU_LOAD"] = "Load Game",
            ["CENIRELOAD_MAINMENU_LOAD_DESC"] = "Open the CENI RELOAD Checkpoint Vault to load a saved run.",
            ["CENIRELOAD_UI_TITLE"] = "CENI RELOAD — CHECKPOINT VAULT",
            ["CENIRELOAD_UI_SUBTITLE"] = "Select a slot to inspect, load, or clear",
            ["CENIRELOAD_UI_FILTER_SURVIVOR"] = "Survivor: {0}",
            ["CENIRELOAD_UI_FILTER_MULTIPLAYER"] = "Multiplayer (5 Slots)",
            ["CENIRELOAD_UI_FILTER_ALL"] = "View: All Survivors",
            ["CENIRELOAD_UI_LABEL_PLAYERS"] = "Players",
            ["CENIRELOAD_UI_PLAYER_PRESENT"] = "In Lobby",
            ["CENIRELOAD_UI_PLAYER_ABSENT"] = "Absent",
            ["CENIRELOAD_UI_STORAGE_CLOUD"] = "Cloud Storage",
            ["CENIRELOAD_UI_CLOSE"] = "Close",
            ["CENIRELOAD_UI_SLOT_LABEL"] = "SLOT {0}",
            ["CENIRELOAD_UI_SLOT_EMPTY"] = "Empty Slot",
            ["CENIRELOAD_UI_SLOT_EMPTY_HINT"] = "Start a run to record a checkpoint here.",
            ["CENIRELOAD_UI_INSPECT_EMPTY"] = "Select a saved checkpoint card on the left to view full details.",
            ["CENIRELOAD_UI_BTN_LOAD"] = "LOAD CHECKPOINT",
            ["CENIRELOAD_UI_BTN_DELETE"] = "DELETE SLOT",
            ["CENIRELOAD_UI_BTN_LOCKED"] = "SLOT LOCKED",
            ["CENIRELOAD_UI_CONFIRM_DELETE"] = "Confirm deletion?",
            ["CENIRELOAD_UI_BTN_CONFIRM_YES"] = "YES, DELETE",
            ["CENIRELOAD_UI_BTN_CONFIRM_NO"] = "CANCEL",
            ["CENIRELOAD_UI_LABEL_SURVIVOR"] = "Survivor",
            ["CENIRELOAD_UI_LABEL_STAGE"] = "Stage",
            ["CENIRELOAD_UI_LABEL_STAGE_NUM"] = "Stage #",
            ["CENIRELOAD_UI_LABEL_WAVE"] = "Simulacrum Wave",
            ["CENIRELOAD_UI_LABEL_TIME"] = "Run Time",
            ["CENIRELOAD_UI_LABEL_DIFFICULTY"] = "Difficulty",
            ["CENIRELOAD_UI_LABEL_GOLD"] = "Gold / Void Coins",
            ["CENIRELOAD_UI_LABEL_ITEMS"] = "Items / Allies",
            ["CENIRELOAD_UI_LABEL_SAVED_AT"] = "Saved At",
            ["CENIRELOAD_UI_MOD_MISMATCH"] = "Warning: Mod list changed since this checkpoint was saved.",
            ["CENIRELOAD_UI_MOD_OK"] = "Mod profile matches.",
            ["CENIRELOAD_UI_LOCK_PROTECTED"] = "Protected: Locked against auto-save overwrite and deletion.",
            ["CENIRELOAD_UI_ALL_LOCKED_WARN"] = "NO FREE SLOTS ({0}) — All 5 slots are locked! Unlock or delete a slot before continuing.",
            ["CENIRELOAD_CHAT_SAVED"] = "<color=#4de1ff><b>[CENI RELOAD]</b></color> Checkpoint saved to Slot {0} ({1} — Stage {2}).",
            ["CENIRELOAD_CHAT_ALL_LOCKED"] = "<color=#facc15><b>[CENI RELOAD]</b></color> All 5 slots for {0} are locked! Unlock or delete a slot manually in the Vault before new checkpoints can be saved.",
            ["CENIRELOAD_CHAT_SAVE_ERROR"] = "<color=#ff5c5c><b>[CENI RELOAD]</b></color> Could not save checkpoint for Stage {0}.",
            ["CENIRELOAD_CHAT_LOADED"] = "<color=#4de1ff><b>[CENI RELOAD]</b></color> Checkpoint restored from Slot {0} ({1} — Stage {2}).",
            ["CENIRELOAD_UI_NO_SAVES"] = "No checkpoints found for this view.",
            ["CENIRELOAD_UI_MISSING_SURVIVOR_BADGE"] = "Missing Mod",
            ["CENIRELOAD_UI_MISSING_SURVIVOR_WARN"] = "Survivor '{0}' is not installed in this profile. Reinstall the mod to load this checkpoint.",
            ["CENIRELOAD_UI_LABEL_LOOP"] = "Loop",
            ["CENIRELOAD_UI_ITEMS_SHORT"] = "items",
            ["CENIRELOAD_UI_HINT_CLOSE"] = "ESC / B: Close",
            ["CENIRELOAD_UI_HINT_KBM"] = "Click: Inspect  •  Right-Click Card: Lock / Unlock  •  ESC: Close",
            ["CENIRELOAD_UI_HINT_GAMEPAD"] = "D-Pad: Navigate  •  A: Select / Load  •  {0}Lock / Unlock  •  B: Close"
        };

        private static readonly Dictionary<string, string> PortugueseBr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CENIRELOAD_BTN_OPEN_VAULT"] = "COFRE CENI RELOAD",
            ["CENIRELOAD_MAINMENU_LOAD"] = "Carregar Jogo",
            ["CENIRELOAD_MAINMENU_LOAD_DESC"] = "Abra o Cofre CENI RELOAD para carregar uma partida salva.",
            ["CENIRELOAD_UI_TITLE"] = "CENI RELOAD — COFRE DE CHECKPOINTS",
            ["CENIRELOAD_UI_SUBTITLE"] = "Escolha um slot para inspecionar, carregar ou limpar",
            ["CENIRELOAD_UI_FILTER_SURVIVOR"] = "Sobrevivente: {0}",
            ["CENIRELOAD_UI_FILTER_MULTIPLAYER"] = "Multiplayer (5 Slots)",
            ["CENIRELOAD_UI_FILTER_ALL"] = "Filtro: Todos",
            ["CENIRELOAD_UI_LABEL_PLAYERS"] = "Jogadores",
            ["CENIRELOAD_UI_PLAYER_PRESENT"] = "No Lobby",
            ["CENIRELOAD_UI_PLAYER_ABSENT"] = "Ausente",
            ["CENIRELOAD_UI_STORAGE_CLOUD"] = "Nuvem",
            ["CENIRELOAD_UI_CLOSE"] = "Fechar",
            ["CENIRELOAD_UI_SLOT_LABEL"] = "SLOT {0}",
            ["CENIRELOAD_UI_SLOT_EMPTY"] = "Slot Vazio",
            ["CENIRELOAD_UI_SLOT_EMPTY_HINT"] = "Inicie uma partida para gravar um checkpoint aqui.",
            ["CENIRELOAD_UI_INSPECT_EMPTY"] = "Selecione um card salvo à esquerda para ver os detalhes.",
            ["CENIRELOAD_UI_BTN_LOAD"] = "CARREGAR CHECKPOINT",
            ["CENIRELOAD_UI_BTN_DELETE"] = "EXCLUIR SLOT",
            ["CENIRELOAD_UI_BTN_LOCKED"] = "SLOT BLOQUEADO",
            ["CENIRELOAD_UI_CONFIRM_DELETE"] = "Confirmar exclusão?",
            ["CENIRELOAD_UI_BTN_CONFIRM_YES"] = "SIM, EXCLUIR",
            ["CENIRELOAD_UI_BTN_CONFIRM_NO"] = "CANCELAR",
            ["CENIRELOAD_UI_LABEL_SURVIVOR"] = "Sobrevivente",
            ["CENIRELOAD_UI_LABEL_STAGE"] = "Fase",
            ["CENIRELOAD_UI_LABEL_STAGE_NUM"] = "Nº da Fase",
            ["CENIRELOAD_UI_LABEL_WAVE"] = "Onda do Simulacrum",
            ["CENIRELOAD_UI_LABEL_TIME"] = "Tempo",
            ["CENIRELOAD_UI_LABEL_DIFFICULTY"] = "Dificuldade",
            ["CENIRELOAD_UI_LABEL_GOLD"] = "Ouro / Moedas do Vazio",
            ["CENIRELOAD_UI_LABEL_ITEMS"] = "Itens / Aliados",
            ["CENIRELOAD_UI_LABEL_SAVED_AT"] = "Salvo em",
            ["CENIRELOAD_UI_MOD_MISMATCH"] = "Aviso: A lista de mods mudou desde que este checkpoint foi salvo.",
            ["CENIRELOAD_UI_MOD_OK"] = "Perfil de mods compatível.",
            ["CENIRELOAD_UI_LOCK_PROTECTED"] = "Protegido: Cadeado ativo contra sobrescrita e exclusão.",
            ["CENIRELOAD_UI_ALL_LOCKED_WARN"] = "SEM ESPAÇO ({0}) — Todos os 5 slots estão bloqueados! Desbloqueie ou apague um slot antes de continuar.",
            ["CENIRELOAD_CHAT_SAVED"] = "<color=#4de1ff><b>[CENI RELOAD]</b></color> Checkpoint salvo no Slot {0} ({1} — Fase {2}).",
            ["CENIRELOAD_CHAT_ALL_LOCKED"] = "<color=#facc15><b>[CENI RELOAD]</b></color> Todos os 5 slots de {0} estão bloqueados! Desbloqueie ou apague um slot manualmente no Cofre antes de continuar gravando.",
            ["CENIRELOAD_CHAT_SAVE_ERROR"] = "<color=#ff5c5c><b>[CENI RELOAD]</b></color> Não foi possível salvar o checkpoint da Fase {0}.",
            ["CENIRELOAD_CHAT_LOADED"] = "<color=#4de1ff><b>[CENI RELOAD]</b></color> Checkpoint restaurado do Slot {0} ({1} — Fase {2}).",
            ["CENIRELOAD_UI_NO_SAVES"] = "Nenhum checkpoint encontrado neste filtro.",
            ["CENIRELOAD_UI_MISSING_SURVIVOR_BADGE"] = "Mod Ausente",
            ["CENIRELOAD_UI_MISSING_SURVIVOR_WARN"] = "O sobrevivente '{0}' não está instalado neste perfil. Reinstale o mod para carregar este checkpoint.",
            ["CENIRELOAD_UI_LABEL_LOOP"] = "Loop",
            ["CENIRELOAD_UI_ITEMS_SHORT"] = "itens",
            ["CENIRELOAD_UI_HINT_CLOSE"] = "ESC / B: Fechar",
            ["CENIRELOAD_UI_HINT_KBM"] = "Clique: Inspecionar  •  Botão Direito no Card: Trancar / Destrancar  •  ESC: Fechar",
            ["CENIRELOAD_UI_HINT_GAMEPAD"] = "Direcional: Navegar  •  A: Selecionar / Carregar  •  {0}Trancar / Destrancar  •  B: Fechar"
        };

        private static readonly Dictionary<string, Dictionary<string, string>> ExternalOverrides =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public static void LoadExternalOverrides(string pluginDirectory)
        {
            ExternalOverrides.Clear();
            if (string.IsNullOrEmpty(pluginDirectory)) return;

            string langRoot = IOPath.Combine(pluginDirectory, "Language");
            if (!Directory.Exists(langRoot)) return;

            foreach (string langDir in Directory.GetDirectories(langRoot))
            {
                string langCode = IOPath.GetFileName(langDir);
                string jsonFile = IOPath.Combine(langDir, "ceni_reload.json");
                if (!File.Exists(jsonFile)) continue;

                try
                {
                    string raw = File.ReadAllText(jsonFile);
                    JSONNode root = JSON.Parse(raw);
                    if (root == null) continue;

                    JSONNode stringsNode = root["strings"] ?? root;
                    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string key in stringsNode.Keys)
                    {
                        var val = stringsNode[key];
                        if (!string.IsNullOrEmpty(key) && val != null)
                        {
                            map[key] = val.Value;
                        }
                    }

                    if (map.Count > 0)
                    {
                        ExternalOverrides[langCode] = map;
                    }
                }
                catch (Exception ex)
                {
                    CeniReloadPlugin.Log?.LogWarning($"Failed to load external language file '{jsonFile}': {ex.Message}");
                }
            }
        }

        public static string Get(string key)
        {
            string currentLang = RoR2.Language.currentLanguageName ?? "en";

            if (ExternalOverrides.TryGetValue(currentLang, out var extMap) && extMap.TryGetValue(key, out string extVal))
            {
                return extVal;
            }

            if (currentLang.StartsWith("pt", StringComparison.OrdinalIgnoreCase) && ExtraPortuguese.TryGetValue(key, out string extraPt)) return extraPt;
            if (ExtraEnglish.TryGetValue(key, out string extraEn)) return extraEn;

            if (currentLang.Equals("pt-BR", StringComparison.OrdinalIgnoreCase) ||
                currentLang.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                if (PortugueseBr.TryGetValue(key, out string ptVal))
                    return ptVal;
            }

            if (English.TryGetValue(key, out string enVal))
                return enVal;

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        public static string LocalizeGameToken(string token, string fallback)
        {
            if (string.IsNullOrEmpty(token)) return fallback;
            string translated = RoR2.Language.GetString(token);
            return string.IsNullOrEmpty(translated) || translated == token ? fallback : translated;
        }
    }
}
