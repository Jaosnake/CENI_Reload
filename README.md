# 🧤 CENI_RELOAD — Checkpoint Vault

> ✨ **100% Written From Scratch — Original Standalone Project**  
> **CENI_RELOAD** is a completely original stage checkpoint vault for **Risk of Rain 2**, designed and built **100% from scratch** by **Jaosnake** under the **MIT License**.  
> Zero third-party mod code • Standalone HarmonyX (`Jaosnake.CeniReload`) • Single dependency (`BepInExPack`) • Custom `.cenireload.json` format • Redesigned two-column Card Vault UI.

[🇺🇸 English](#-english) • [🇧🇷 Português (Brasil)](#-português-brasil) • [⚽ Why the name CENI?](#-why-the-name-ceni)

---

## 🇺🇸 English

### 🎯 Overview

**CENI_RELOAD** automatically records a full run checkpoint at the start of every playable stage and adds a sleek **Checkpoint Vault** directly inside the Character Select lobby so you can browse, inspect, load, or clear your saved runs anytime.

### ⚽ Why the name CENI?

The name **C.E.N.I** is a playful tribute to legendary Brazilian goalkeeper **Rogério Ceni**, iconic captain of **São Paulo FC** — famous for always **"saving"** his team with miraculous stops when it mattered most! 🧤🇧🇷  
Just like Ceni saved São Paulo on the pitch, **CENI_RELOAD** saves your runs on Petrichor V! *(And yes, that's why the icon features the Brazilian flag cape and the golden trophy!)* 🏆

### 🚀 Key Features

| Feature | Description |
| :--- | :--- |
| 🛠️ **100% Original Architecture** | Built from the ground up in C# (`netstandard2.1`) under `Jaosnake.CeniReload` using HarmonyX only — single dependency (`bbepis-BepInExPack-5.4.2121`). |
| 💾 **Automatic Stage Checkpoints** | Automatically captures a full snapshot at the start of every gameplay stage. |
| 🗂️ **5 Rotating Slots per Survivor** | Stores up to **5 independent `.cenireload.json` slots** per survivor and profile, automatically rotating the oldest slot when full. |
| 🃏 **Redesigned Two-Column Vault UI** | Open **CENI RELOAD VAULT** from the lobby to browse visual slot cards (with *Current Survivor* / *All Survivors* filter), inspect detailed stats on the right, load checkpoints, or delete slots with inline confirmation. |
| 🛡️ **Complete State Preservation** | Saves run seed, stage, stopwatch, difficulty, artifacts/rules (`RuleBook`), team experience, survivor loadout, gold, void coins, permanent items (stored by **item name** for mod-order safety), equipment slots/sets, and owned drones/allies. |
| 🔍 **Mod-List Verification** | Checks your active BepInEx plugin list against the checkpoint metadata and highlights any mismatch right inside the inspector. |
| 🎮 **Mouse, Keyboard & Controller** | Full standard UI navigation plus instant close via `ESC` or controller `B` / `Circle`. |
| 🌐 **Built-in EN & pt-BR Localization** | Follows the game language automatically (`CENIRELOAD_*` keys), with optional external JSON overrides in `BepInEx/plugins/CENI_RELOAD/Language/<lang>/ceni_reload.json`. |

### 📦 Installation

**Via Thunderstore Mod Manager / r2modman (Recommended):**
1. Install `bbepis-BepInExPack`.
2. Install `Jaosnake-CENI_RELOAD`.
3. Launch Risk of Rain 2 and open **CENI RELOAD VAULT** in the Character Select lobby!

**Manual Installation:**
1. Extract the ZIP into `BepInEx/plugins/CENI_RELOAD/` (keeping `CENI_RELOAD.dll` inside `BepInEx/plugins/`).
2. Launch Risk of Rain 2.

### ⚙️ Configuration & Storage

- **Config file**: `BepInEx/config/com.Jaosnake.CENI_RELOAD.cfg`
  - `AutoSaveOnStageStart` *(default: `true`)* — Automatically saves a checkpoint at the start of every playable stage.
  - `ShowChatMessages` *(default: `true`)* — Prints a chat notification when a checkpoint is saved or restored.
- **Checkpoint Vault directory**:
  ```text
  BepInEx/config/Jaosnake.CENI_RELOAD/Vault/<profile>/<survivor>/slot_<1..5>.cenireload.json
  ```

### 📜 Source Code & License

- 💻 **Official Repository**: [https://github.com/Jaosnake/CENI_Reload](https://github.com/Jaosnake/CENI_Reload)
- ⚖️ **License**: **MIT License** — Copyright (c) 2026 **Jaosnake**. See [LICENSE](LICENSE).

---

## 🇧🇷 Português (Brasil)

> ✨ **Projeto 100% Feito do Zero — Código-Fonte Próprio e Independente**  
> O **CENI_RELOAD** é um cofre de checkpoints de fase para **Risk of Rain 2** totalmente original, projetado e escrito **100% do zero** por **Jaosnake** sob a **Licença MIT**.  
> Zero código de terceiros • Arquitetura própria em HarmonyX (`Jaosnake.CeniReload`) • Dependência única (`BepInExPack`) • Formato próprio `.cenireload.json` • Interface própria em cards de duas colunas.

### 🎯 Visão Geral

O **CENI_RELOAD** grava automaticamente o estado completo da sua partida no início de cada fase jogável e adiciona o **Cofre CENI RELOAD** direto na tela de Seleção de Personagem para você visualizar, inspecionar, carregar ou limpar seus checkpoints quando quiser.

### ⚽ Por que o nome CENI?

O nome **C.E.N.I** é uma homenagem bem-humorada ao lendário goleiro brasileiro **Rogério Ceni**, eterno ídolo do **São Paulo FC** — famoso por sempre **"salvar"** o time nos momentos mais difíceis! 🧤🇧🇷  
Assim como o Ceni salvava o São Paulo em campo, o **CENI_RELOAD** salva as suas runs em Petrichor V! *(E é por isso que o ícone traz a capa da bandeira do Brasil e a taça dourada!)* 🏆

### 🚀 Recursos Principais

| Recurso | Descrição |
| :--- | :--- |
| 🛠️ **Arquitetura 100% Autoral** | Escrito do zero em C# (`netstandard2.1`) em `Jaosnake.CeniReload` usando apenas HarmonyX — dependência única (`bbepis-BepInExPack-5.4.2121`). |
| 💾 **Checkpoints Automáticos por Fase** | Registra automaticamente um snapshot completo no início de cada fase jogável. |
| 🗂️ **5 Slots Rotativos por Sobrevivente** | Armazena até **5 slots independentes em `.cenireload.json`** por sobrevivente e perfil, substituindo automaticamente o slot mais antigo quando os cinco estiverem cheios. |
| 🃏 **Interface de Cofre em Duas Colunas** | Abra o **COFRE CENI RELOAD** no lobby para navegar pelos cards visuais dos slots (com filtro *Sobrevivente Atual* / *Todos*), ver os detalhes completos no painel direito, carregar checkpoints ou excluir slots com confirmação integrada. |
| 🛡️ **Preservação Completa da Partida** | Salva seed, fase, cronômetro, dificuldade, artefatos/regras (`RuleBook`), experiência do time, loadout, ouro, moedas do vazio, itens permanentes (salvos pelo **nome do item** para segurança entre mods), equipamentos e drones/aliados. |
| 🔍 **Verificação da Lista de Mods** | Compara os plugins ativos do BepInEx com os do checkpoint e avisa diretamente no inspetor se houve mudança. |
| 🎮 **Mouse, Teclado e Controle** | Navegação padrão completa e fechamento rápido com `ESC` ou botão `B` / `Círculo`. |
| 🌐 **Localização Nativa EN e pt-BR** | Segue automaticamente o idioma do jogo (chaves `CENIRELOAD_*`), com suporte opcional a JSON externo em `BepInEx/plugins/CENI_RELOAD/Language/<idioma>/ceni_reload.json`. |

### 📦 Instalação

**Via Thunderstore Mod Manager / r2modman (Recomendado):**
1. Instale `bbepis-BepInExPack`.
2. Instale `Jaosnake-CENI_RELOAD`.
3. Inicie o Risk of Rain 2 e clique em **COFRE CENI RELOAD** na tela de Seleção de Personagem!

**Instalação Manual:**
1. Extraia o ZIP em `BepInEx/plugins/CENI_RELOAD/` (mantendo `CENI_RELOAD.dll` dentro de `BepInEx/plugins/`).
2. Inicie o Risk of Rain 2.

### ⚙️ Configuração e Armazenamento

- **Arquivo de configuração**: `BepInEx/config/com.Jaosnake.CENI_RELOAD.cfg`
  - `AutoSaveOnStageStart` *(padrão: `true`)* — Salva automaticamente um checkpoint no início de cada fase jogável.
  - `ShowChatMessages` *(padrão: `true`)* — Mostra um aviso no chat sempre que um checkpoint é salvo ou restaurado.
- **Pasta do Cofre de Checkpoints**:
  ```text
  BepInEx/config/Jaosnake.CENI_RELOAD/Vault/<perfil>/<sobrevivente>/slot_<1..5>.cenireload.json
  ```

### 📜 Código-Fonte e Licença

- 💻 **Repositório Oficial**: [https://github.com/Jaosnake/CENI_Reload](https://github.com/Jaosnake/CENI_Reload)
- ⚖️ **Licença**: **Licença MIT** — Copyright (c) 2026 **Jaosnake**. Consulte [LICENSE](LICENSE).
