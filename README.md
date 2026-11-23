# CENI_RELOAD — Checkpoint Vault

> **100% Written From Scratch (Original Standalone Project)**  
> **CENI_RELOAD** is a completely original stage checkpoint system for **Risk of Rain 2**, designed and implemented **100% from scratch** by **Jaosnake** under the **MIT License**. It contains zero third-party mod code, uses its own standalone HarmonyX architecture (`Jaosnake.CeniReload`), its own `.cenireload.json` storage format, its own two-column card-based Vault interface, and requires only `BepInExPack`.

---

## English

### Overview

**CENI_RELOAD** automatically saves run checkpoints at the start of every gameplay stage and gives you a dedicated **Checkpoint Vault** directly inside the Character Select lobby to inspect, load, or clear your saved runs.

### Key Features

- **100% Original Architecture**: Built from the ground up in C# (`netstandard2.1`) under `Jaosnake.CeniReload` using HarmonyX only — single dependency (`bbepis-BepInExPack-5.4.2121`).
- **Automatic Stage Checkpoints**: Captures a full run snapshot at the start of every playable stage.
- **5 Rotating Slots per Survivor**: Stores up to five independent `.cenireload.json` slots per survivor and user profile, automatically rotating the oldest slot when all five are filled.
- **Two-Column Card Vault UI**:
  - Open **CENI RELOAD VAULT** from the Character Select lobby.
  - Browse visual slot cards on the left with status accent strips and a filter toggle (*Current Survivor* / *All Survivors*).
  - Inspect full run stats on the right (Stage, Stage #, Run Time, Difficulty, Gold / Void Coins, Items / Allies, Saved Date, and Mod Compatibility status).
  - Load any checkpoint or clear slots using the built-in inline confirmation bar.
- **Resilient State Preservation**: Saves run seed, stage, stopwatch, difficulty, artifacts/rules (`RuleBook`), team experience, survivor loadout, gold, void coins, permanent items (stored by **item name** for load-order safety), equipment slots/sets, and owned drones/allies.
- **Mod-List Verification**: Alerts you in the inspector if your installed BepInEx plugins changed since the checkpoint was created.
- **Mouse, Keyboard & Controller Navigation**: Standard UI navigation plus quick close via `ESC` or controller `B` / `Circle`.
- **Built-in EN & pt-BR Localization**: Follows the in-game language automatically (`CENIRELOAD_*` keys), with optional external JSON overrides in `BepInEx/plugins/CENI_RELOAD/Language/<lang>/ceni_reload.json`.

### Installation

**Via Thunderstore Mod Manager / r2modman (Recommended):**
1. Install `bbepis-BepInExPack`.
2. Install `Jaosnake-CENI_RELOAD`.
3. Launch the game.

**Manual Installation:**
1. Extract the package into `BepInEx/plugins/CENI_RELOAD/` (ensuring `CENI_RELOAD.dll` is inside `BepInEx/plugins/`).
2. Launch Risk of Rain 2.

### Configuration & Storage Paths

- **Config file**: `BepInEx/config/com.Jaosnake.CENI_RELOAD.cfg`
  - `AutoSaveOnStageStart` (`true` by default): Automatically saves a checkpoint at the start of each gameplay stage.
  - `ShowChatMessages` (`true` by default): Displays a chat notification when a checkpoint is saved or restored.
- **Checkpoint files**:
  ```text
  BepInEx/config/Jaosnake.CENI_RELOAD/Vault/<profile>/<survivor>/slot_<1..5>.cenireload.json
  ```

### Source Code & License

- **Source Repository**: [https://github.com/Jaosnake/CENI_Reload](https://github.com/Jaosnake/CENI_Reload)
- **License**: **MIT License** — Copyright (c) 2026 Jaosnake. See [LICENSE](LICENSE).

---

## Português (Brasil)

> **Projeto 100% Feito do Zero (Código-Fonte Próprio e Independente)**  
> O **CENI_RELOAD** é um sistema de checkpoints de fase para **Risk of Rain 2** totalmente original, projetado e escrito **100% do zero** por **Jaosnake** sob a **Licença MIT**. Não reutiliza código de terceiros, possui arquitetura própria em HarmonyX (`Jaosnake.CeniReload`), formato próprio de salvamento (`.cenireload.json`), interface própria em cards de duas colunas e depende apenas do `BepInExPack`.

### Visão Geral

O **CENI_RELOAD** grava checkpoints automáticos no início de cada fase jogável e adiciona o **Cofre CENI RELOAD** diretamente na tela de Seleção de Personagem para inspecionar, carregar ou limpar seus slots salvos.

### Recursos Principais

- **Arquitetura 100% Autoral**: Escrito do zero em C# (`netstandard2.1`) em `Jaosnake.CeniReload` usando apenas HarmonyX — dependência única (`bbepis-BepInExPack-5.4.2121`).
- **Checkpoints Automáticos por Fase**: Registra automaticamente o estado completo da partida no início de cada fase jogável.
- **5 Slots Rotativos por Sobrevivente**: Armazena até cinco slots independentes em `.cenireload.json` por sobrevivente e perfil, sobrescrevendo automaticamente o slot mais antigo quando os cinco estiverem ocupados.
- **Interface de Cofre em Duas Colunas**:
  - Abra o **COFRE CENI RELOAD** direto na tela de Seleção de Personagem.
  - Navegue pelos cards visuais dos slots à esquerda, com barra lateral de status e botão de filtro (*Sobrevivente Atual* / *Todos*).
  - Veja todos os detalhes da partida à direita (Fase, Nº da Fase, Tempo, Dificuldade, Ouro / Moedas do Vazio, Itens / Aliados, Data e compatibilidade da lista de mods).
  - Carregue qualquer checkpoint ou exclua slots com confirmação integrada diretamente no painel.
- **Preservação Completa da Partida**: Salva seed, fase, cronômetro, dificuldade, artefatos/regras (`RuleBook`), experiência do time, loadout, ouro, moedas do vazio, itens permanentes (salvos pelo **nome do item** para maior segurança entre mods), equipamentos e drones/aliados.
- **Verificação de Lista de Mods**: Exibe um aviso no painel de inspeção caso os plugins instalados no BepInEx tenham mudado desde o salvamento.
- **Suporte a Mouse, Teclado e Controle**: Navegação padrão e fechamento rápido via `ESC` ou botão `B` / `Círculo`.
- **Localização Nativa em EN e pt-BR**: Segue automaticamente o idioma do jogo (chaves `CENIRELOAD_*`), com suporte opcional a arquivos JSON externos em `BepInEx/plugins/CENI_RELOAD/Language/<idioma>/ceni_reload.json`.

### Instalação

**Via Thunderstore Mod Manager / r2modman (Recomendado):**
1. Instale `bbepis-BepInExPack`.
2. Instale `Jaosnake-CENI_RELOAD`.
3. Inicie o jogo.

**Instalação Manual:**
1. Extraia o pacote em `BepInEx/plugins/CENI_RELOAD/` (mantendo `CENI_RELOAD.dll` dentro de `BepInEx/plugins/`).
2. Inicie o Risk of Rain 2.

### Configuração e Caminhos de Salvamento

- **Arquivo de configuração**: `BepInEx/config/com.Jaosnake.CENI_RELOAD.cfg`
  - `AutoSaveOnStageStart` (padrão `true`): Salva automaticamente um checkpoint no início de cada fase jogável.
  - `ShowChatMessages` (padrão `true`): Exibe aviso no chat sempre que um checkpoint é salvo ou restaurado.
- **Arquivos de checkpoint**:
  ```text
  BepInEx/config/Jaosnake.CENI_RELOAD/Vault/<perfil>/<sobrevivente>/slot_<1..5>.cenireload.json
  ```

### Código-Fonte e Licença

- **Repositório Oficial**: [https://github.com/Jaosnake/CENI_Reload](https://github.com/Jaosnake/CENI_Reload)
- **Licença**: **Licença MIT** — Copyright (c) 2026 Jaosnake. Consulte [LICENSE](LICENSE).
