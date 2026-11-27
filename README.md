# 🧤 C.E.N.I RELOAD (`CENI_RELOAD`) — Checkpoint Vault

> ✨ **100% Written From Scratch — Original Standalone Project**  
> **C.E.N.I RELOAD** (**C**hrono-**E**nhanced **N**exus of **I**teration) is a completely original stage checkpoint vault for **Risk of Rain 2**, designed and built **100% from scratch** by **Jaosnake** under the **MIT License**.  
> Zero third-party mod code • Standalone HarmonyX (`Jaosnake.CeniReload`) • Single dependency (`BepInExPack`) • Custom `.cenireload.json` format • Redesigned two-column Card Vault UI.

> 🆕 **Version 1.0.2 is here! / A versão 1.0.2 chegou!**
> 🛟 Backup recovery · 🔎 Pre-load validation · 📋 Restoration report · 👥 Flexible co-op · 🎲 More continuity
> 📖 **[Read the 1.0.2 guide in the Wiki / Veja as novidades e o guia da 1.0.2 na WIKI](https://thunderstore.io/c/riskofrain2/p/Jaosnake/CENI_RELOAD/wiki/6999-ceni-reload-guia-guide/)**

🇺🇸 **English** • 🇧🇷 **Português (Brasil)**

**⚽ What does C.E.N.I mean? / O que significa C.E.N.I?**

**C**hrono-**E**nhanced **N**exus of **I**teration. 🧤 A tribute to **Rogério Ceni**, the legendary **São Paulo FC** goalkeeper: just as he saved his team, C.E.N.I RELOAD saves your runs on Petrichor V!

Uma homenagem a **Rogério Ceni**, lendário goleiro do **São Paulo FC**: assim como ele salvava o time em campo, o C.E.N.I RELOAD salva suas runs em Petrichor V! 🏆🇧🇷

---

## 🇺🇸 English

### 🎯 Overview

**C.E.N.I RELOAD** (`Jaosnake-CENI_RELOAD`) automatically records a stage-entry checkpoint at the start of every playable stage **from Stage 2 onward** (skipping Stage 1 so you can freely test survivors, mods, or artifacts) and adds a sleek **Checkpoint Vault** directly in both the **Main Menu** (`Load Game`) and the **Character Select lobby** so you can browse, inspect, load, or clear your saved runs anytime.

### 📸 Preview

![C.E.N.I RELOAD — Main Menu & Checkpoint Vault](https://raw.githubusercontent.com/Jaosnake/CENI_Reload/main/docs/screenshots/main-menu-vault-en.jpg)

### ⚽ What does C.E.N.I mean?

**C.E.N.I** stands for:

- **C** — **C**hrono
- **E** — **E**nhanced
- **N** — **N**exus of
- **I** — **I**teration

And it's also a clever mnemonic tribute to legendary Brazilian goalkeeper **Rogério Ceni**, iconic captain of **São Paulo FC** — famous for always **"saving"** his team with miraculous stops when it mattered most! 🧤🇧🇷  
Just like Rogério Ceni saved São Paulo on the pitch, **C.E.N.I RELOAD** saves your runs on Petrichor V! *(That's why the icon features the Brazilian flag cape and the golden trophy!)* 🏆

### 🚀 Key Features

| Feature | Description |
| :--- | :--- |
| 🛠️ **100% Original Architecture** | Built from the ground up in C# (`netstandard2.1`) under `Jaosnake.CeniReload` using HarmonyX only — single dependency (`bbepis-BepInExPack-5.4.2121`). |
| 💾 **Automatic Stage Checkpoints** | Automatically captures a stage-entry checkpoint at the start of every gameplay stage **from Stage 2 onward** (Stage 1 is intentionally skipped so testing builds/mods won't clutter your slots). |
| 🗂️ **5 Rotating Slots per Survivor + 5 Co-op Slots + Slot Lock** | Stores up to **5 independent `.cenireload.json` slots** per survivor **plus 5 dedicated Multiplayer Co-op slots**, rotating the oldest unlocked slot when full. Lock any save (`Right-Click` on card with KB+M, `X` on Xbox, `Square` on PlayStation) with a glowing golden padlock badge to prevent overwrite or deletion; if all 5 slots are locked, the Vault warns you so you can unlock a slot before the next checkpoint. |
| 👥 **Multiplayer Co-op (Host-Only)** | Only the **Host** needs `CENI_RELOAD` installed. Matches each player by **SteamID / EOS identity**: present friends restore their saved survivor, loadout, items, currency, and drones. In 1.0.2 you can start with part of the group; absent saved players keep their build reserved for their first later join. New players start fresh without inheriting another player’s build. The Inspector shows live lobby presence. See the Wiki for the full co-op guide and limitations. |
| ☁️ **Local or Steam / Epic Cloud Storage** | Saves locally in `BepInEx/config` by default, or opt in to **Steam / Epic Cloud Storage** (`UseCloudStorage = true`, configurable `CloudStorageSubDirectory` to isolate mod profiles). |
| 🎨 **Color-Coded Vault List** | The slot list & cards automatically adapt their background color by run type: **Blue** for standard Singleplayer runs, **Red** for Multiplayer runs, and **Green** for **Simulacrum**, **Artifact of Evolution**, or **Artifact of Devotion** runs. |
| 🃏 **Redesigned Two-Column Vault UI** | Open **CENI RELOAD VAULT** directly from the **Main Menu** or the **Character Select lobby** to browse visual slot cards with **survivor portraits** (*Current Survivor* / *All Survivors* filter), inspect detailed stats on the right, load checkpoints, or delete slots with inline confirmation. |
| 🛡️ **Run State & Extra Modes** | Saves run seed, stage, **loop counters & event flags**, stopwatch, difficulty, artifacts/rules (`RuleBook`), team experience, survivor loadout, gold, void coins, permanent items and **DLC3 temporary items** (stored by **item name** for mod-order safety), equipment slots/sets, owned drones/allies, plus **Simulacrum** waves/enemy inventory, **Artifact of Evolution** monster items, and **Artifact of Devotion** minion inventory. |
| 🔍 **Mod-List & Missing Survivor Protection** | Checks your active BepInEx plugin list against checkpoint metadata, flags uninstalled modded survivors with a **Missing Mod** badge/warning, and blocks launch when a present player needs a survivor that is missing. |
| 🎮 **Mouse, Keyboard & Controller** | Full standard UI navigation with **auto-scrolling slot cards**, quick lobby shortcut (`Y` on Xbox / `Triangle` on PlayStation) with controller-aware glyphs, and instant close via `ESC` or `B` / `Circle`. |
| 🌐 **Built-in EN & pt-BR Localization** | Follows the game language automatically (`CENIRELOAD_*` keys), with optional external JSON overrides in `BepInEx/plugins/CENI_RELOAD/Language/<lang>/ceni_reload.json`. |

### 📦 Installation

**Via Thunderstore Mod Manager / r2modman (Recommended):**
1. Install `bbepis-BepInExPack`.
2. Install `Jaosnake-CENI_RELOAD`.
3. Launch Risk of Rain 2 and open **CENI RELOAD VAULT** in the Character Select lobby!

**Manual Installation:**
1. Copy the ZIP’s `plugins/CENI_RELOAD/` folder into your game’s `BepInEx/plugins/` folder, so the DLL is at `BepInEx/plugins/CENI_RELOAD/CENI_RELOAD.dll`.
2. Launch Risk of Rain 2.

### ⚙️ Configuration & Storage

**Checkpoint compatibility:** New checkpoints record player/ally positions and the generator state used to create the stage. Older checkpoints remain readable, but their original positions were never recorded and cannot be recovered; the game chooses the player spawn and allies use safe placement. Local file replacement retains the previous checkpoint as `.bak`. Cloud storage uses its separate existing backend.

- **Config file**: `BepInEx/config/com.Jaosnake.CENI_RELOAD.cfg`
  - `AutoSaveOnStageStart` *(default: `true`)* — Automatically saves a checkpoint at the start of every playable stage from Stage 2 onward.
  - `ShowChatMessages` *(default: `true`)* — Controls routine checkpoint chat notifications. Restoration reports remain visible.
  - `UseCloudStorage` *(default: `false`)* — Requests Steam / Epic Games Cloud Storage (`RoR2Application.cloudStorage`); if unavailable, CENI can fall back to local storage with a warning.
  - `CloudStorageSubDirectory` *(default: `CENI_RELOAD`)* — Subdirectory inside Cloud Storage to keep separate vaults per mod profile.
- **Checkpoint Vault directory**:
  ```text
  BepInEx/config/Jaosnake.CENI_RELOAD/Vault/<profile>/<survivor>/slot_<1..5>.cenireload.json
  ```

### 📜 Source Code & License

- 💻 **Official Repository**: [https://github.com/Jaosnake/CENI_Reload](https://github.com/Jaosnake/CENI_Reload)
- ⚖️ **License**: **MIT License** — Copyright (c) 2026 **Jaosnake**. See [LICENSE](https://github.com/Jaosnake/CENI_Reload/blob/main/LICENSE).

---

## 🇧🇷 Português (Brasil)

> ✨ **Projeto 100% Feito do Zero — Código-Fonte Próprio e Independente**  
> O **C.E.N.I RELOAD** (**C**hrono-**E**nhanced **N**exus of **I**teration) é um cofre de checkpoints de fase para **Risk of Rain 2** totalmente original, projetado e escrito **100% do zero** por **Jaosnake** sob a **Licença MIT**.  
> Zero código de terceiros • Arquitetura própria em HarmonyX (`Jaosnake.CeniReload`) • Dependência única (`BepInExPack`) • Formato próprio `.cenireload.json` • Interface própria em cards de duas colunas.

### 🎯 Visão Geral

O **C.E.N.I RELOAD** (`Jaosnake-CENI_RELOAD`) grava automaticamente um checkpoint de entrada na fase no início de cada fase jogável **a partir da Fase 2** (pulando a Fase 1 para você testar personagens, mods ou artefatos sem poluir os slots) e adiciona o **Cofre CENI RELOAD** tanto no **Menu Principal** (`Carregar Jogo`) quanto na tela de **Seleção de Personagem** para você visualizar, inspecionar, carregar ou limpar seus checkpoints quando quiser.

### 📸 Prévia

![C.E.N.I RELOAD — Menu Principal e Cofre de Checkpoints](https://raw.githubusercontent.com/Jaosnake/CENI_Reload/main/docs/screenshots/main-menu-vault-en.jpg)

### ⚽ O que significa C.E.N.I?

**C.E.N.I** é a sigla de:

- **C** — **C**hrono
- **E** — **E**nhanced
- **N** — **N**exus of
- **I** — **I**teration

E também é um mnemônico em homenagem ao lendário goleiro brasileiro **Rogério Ceni**, eterno ídolo do **São Paulo FC** — famoso por sempre **"salvar"** o time nos momentos mais difíceis! 🧤🇧🇷  
Assim como o Rogério Ceni salvava o São Paulo em campo, o **C.E.N.I RELOAD** salva as suas runs em Petrichor V! *(É por isso que o ícone traz a capa da bandeira do Brasil e a taça dourada!)* 🏆

### 🚀 Recursos Principais

| Recurso | Descrição |
| :--- | :--- |
| 🛠️ **Arquitetura 100% Autoral** | Escrito do zero em C# (`netstandard2.1`) em `Jaosnake.CeniReload` usando apenas HarmonyX — dependência única (`bbepis-BepInExPack-5.4.2121`). |
| 💾 **Checkpoints Automáticos por Fase** | Registra automaticamente um checkpoint de entrada na fase no início de cada fase jogável **a partir da Fase 2** (a Fase 1 nunca é salva, evitando poluir seus slots ao testar mods, personagens ou artefatos). |
| 🗂️ **5 Slots por Sobrevivente + 5 Slots Co-op + Cadeado (Lock)** | Armazena até **5 slots independentes em `.cenireload.json`** por sobrevivente **e mais 5 slots dedicados para Multiplayer Co-op**, substituindo o slot desbloqueado mais antigo quando cheio. Tranque qualquer save (**Botão Direito** no card com mouse, **`X`** no Xbox ou **`Quadrado`** no PlayStation) com ícone dourado de cadeado brilhante para impedir sobrescrita ou exclusão; se os 5 slots estiverem trancados, o Cofre e o chat avisam que não há espaço até você destrancar um slot. |
| 👥 **Multiplayer Co-op (Apenas o Host)** | Somente o **Host** precisa ter o `CENI_RELOAD` instalado. Cada jogador é casado pelo **SteamID / EOS**: amigos presentes recebem de volta seus personagens, loadouts, itens, moedas e drones. Na 1.0.2 você pode começar com parte do grupo; ausentes do save mantêm a build reservada para a primeira entrada posterior. Jogadores novos entram sem herdar builds de terceiros. O inspetor mostra quem está presente. Consulte o guia cooperativo e os limites na Wiki. |
| ☁️ **Armazenamento Local ou Nuvem (Steam / Epic Cloud)** | Salva localmente em `BepInEx/config` por padrão, ou ative a **Nuvem da Steam / Epic** (`UseCloudStorage = true`, com `CloudStorageSubDirectory` configurável para isolar perfis de mods). |
| 🎨 **Cores por Modo na Lista de Slots** | O fundo da lista e dos cards muda automaticamente conforme o tipo da partida: **Azul** para Singleplayer padrão, **Vermelho** para **Multiplayer** e **Verde** para **Simulacrum**, **Artifact of Evolution** ou **Artifact of Devotion**. |
| 🃏 **Interface de Cofre em Duas Colunas** | Abra o **COFRE CENI RELOAD** direto no **Menu Principal** ou no **lobby de Seleção de Personagem** para navegar pelos cards visuais com **retratos dos sobreviventes** (filtro *Sobrevivente Atual* / *Todos*), ver os detalhes completos no painel direito, carregar checkpoints ou excluir slots com confirmação integrada. |
| 🛡️ **Estado da Partida e Modos Extras** | Salva seed, fase, **contadores de loop e flags de evento**, cronômetro, dificuldade, artefatos/regras (`RuleBook`), experiência do time, loadout, ouro, moedas do vazio, itens permanentes e **itens temporários da DLC3** (salvos pelo **nome do item** para segurança entre mods), equipamentos, drones/aliados, além de ondas e inventário inimigo do **Simulacrum**, itens de monstros do **Artifact of Evolution** e inventário do **Artifact of Devotion**. |
| 🔍 **Verificação de Mods e Sobrevivente Ausente** | Compara os plugins ativos do BepInEx com o checkpoint, identifica sobreviventes de mods removidos com aviso **Mod Ausente** e impede iniciar quando um jogador presente precisa de um sobrevivente ausente. |
| 🎮 **Mouse, Teclado e Controle** | Navegação padrão completa com **auto-scroll na lista de cards**, atalho rápido no lobby (`Y` no Xbox / `Triângulo` no PlayStation) com ícone dinâmico do controle e fechamento rápido com `ESC` ou `B` / `Círculo`. |
| 🌐 **Localização Nativa EN e pt-BR** | Segue automaticamente o idioma do jogo (chaves `CENIRELOAD_*`), com suporte opcional a JSON externo em `BepInEx/plugins/CENI_RELOAD/Language/<idioma>/ceni_reload.json`. |

### 📦 Instalação

**Via Thunderstore Mod Manager / r2modman (Recomendado):**
1. Instale `bbepis-BepInExPack`.
2. Instale `Jaosnake-CENI_RELOAD`.
3. Inicie o Risk of Rain 2 e clique em **COFRE CENI RELOAD** na tela de Seleção de Personagem!

**Instalação Manual:**
1. Copie a pasta `plugins/CENI_RELOAD/` do ZIP para `BepInEx/plugins/` do jogo, deixando a DLL em `BepInEx/plugins/CENI_RELOAD/CENI_RELOAD.dll`.
2. Inicie o Risk of Rain 2.

### ⚙️ Configuração e Armazenamento

**Compatibilidade dos checkpoints:** Novos checkpoints registram as posições do jogador/aliados e o estado do gerador usado para criar a fase. Checkpoints antigos continuam legíveis, mas suas posições originais não foram registradas e não podem ser recuperadas; o jogo escolhe o ponto do jogador e os aliados usam posicionamento seguro. A substituição local conserva o checkpoint anterior em `.bak`. A nuvem usa seu backend separado já existente.

- **Arquivo de configuração**: `BepInEx/config/com.Jaosnake.CENI_RELOAD.cfg`
  - `AutoSaveOnStageStart` *(padrão: `true`)* — Salva automaticamente um checkpoint no início de cada fase jogável a partir da Fase 2.
  - `ShowChatMessages` *(padrão: `true`)* — Controla as notificações rotineiras de checkpoint no chat. Relatórios de restauração continuam visíveis.
  - `UseCloudStorage` *(padrão: `false`)* — Solicita a Nuvem da Steam / Epic (`RoR2Application.cloudStorage`); se indisponível, o CENI pode usar armazenamento local com um aviso.
  - `CloudStorageSubDirectory` *(padrão: `CENI_RELOAD`)* — Subpasta dentro da nuvem para separar cofres entre diferentes perfis de mods.
- **Pasta do Cofre de Checkpoints**:
  ```text
  BepInEx/config/Jaosnake.CENI_RELOAD/Vault/<perfil>/<sobrevivente>/slot_<1..5>.cenireload.json
  ```

### 📜 Código-Fonte e Licença

- 💻 **Repositório Oficial**: [https://github.com/Jaosnake/CENI_Reload](https://github.com/Jaosnake/CENI_Reload)
- ⚖️ **Licença**: **Licença MIT** — Copyright (c) 2026 **Jaosnake**. Consulte [LICENSE](https://github.com/Jaosnake/CENI_Reload/blob/main/LICENSE).

---

<p align="center"><strong>💡 IDEAS HAVE NO COPYRIGHT • IDEIAS NÃO TÊM COPYRIGHT 💡</strong></p>
