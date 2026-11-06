# Classic Say Hi :)

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support%20Zaldaryon-FF5E5B?style=flat&logo=kofi&logoColor=white)](https://ko-fi.com/Zaldaryon)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A server-side Vintage Story mod that restores the classic `"Say hi :)"` player join broadcast message from before game version 1.22.

In Vintage Story 1.22, the join broadcast changed from:
> *Player joined. Say hi :)*

to:
> *Player joined*

This mod restores the original, friendly message across all 34 supported game languages, and provides server commands to customize the join broadcast in real time without restarting the server.

---

## Features

- **Server-side only:** Connecting players do not need to install the mod.
- **Multilingual:** Restores the authentic localized string for all 34 supported game languages.
- **Live customization:** Change the join message format on the fly with `/sayhi set`.
- **Smart fallback:** Setting a general message updates English and cascades as a fallback to other languages that do not have a custom override.
- **Persistent configuration:** Automatically creates and saves settings to `ModConfig/classicsayhi.json`.
- **Zero client overhead:** The server resolves localization and sends formatted chat lines directly to players.

---

## In-Game Commands

Requires the `controlserver` privilege.

| Command | Description |
|---|---|
| `/sayhi` | Displays current status and a preview of the join message |
| `/sayhi status [lang]` | Displays status and preview for the active or specified language |
| `/sayhi set <format>` | Sets the default English message and global fallback (must contain `{0}`) |
| `/sayhi set <lang> <format>` | Sets a message for a specific language (e.g. `pt-br`, `de`, `fr`, `ru`) |
| `/sayhi reset` | Resets all formats back to the default localized messages |
| `/sayhi reset <lang>` | Resets a specific language back to fallback |
| `/sayhi reload` | Reloads configuration from `ModConfig/classicsayhi.json` |

### Command Examples

```text
# Set a custom global join message (English & fallback)
/sayhi set {0} entered the world. Say hi :)

# Set a custom message for Portuguese players
/sayhi set pt-br {0} entrou no servidor. Diga olá :)

# Check current configuration and preview
/sayhi status

# Reset Portuguese to global fallback
/sayhi reset pt-br

# Reset all languages back to original localized strings
/sayhi reset
```

---

## Configuration (`ModConfig/classicsayhi.json`)

The configuration file is created automatically on first run:

```json
{
  "Enabled": true,
  "DefaultFormat": null,
  "FormatsByLanguage": {}
}
```

- When `DefaultFormat` is `null`, the mod uses the authentic localized message for the server's configured language.
- When `DefaultFormat` contains a string, that template overrides English and serves as fallback for any language without a specific override.
- `FormatsByLanguage` stores per-language overrides configured via `/sayhi set <lang> <format>`.

---

## Installation

### Dedicated Server
1. Download `classicsayhi_v1.0.0.zip` from [Releases](https://github.com/Zaldaryon/classicsayhi/releases).
2. Place the `.zip` file into your server's `Mods/` folder.
3. Restart the server.

### Singleplayer
Place the `.zip` file into your local data folder's `Mods/` directory:
- **Windows:** `%appdata%\VintagestoryData\Mods\`
- **Linux:** `~/.config/VintagestoryData/Mods/`
- **macOS:** `~/Library/Application Support/VintagestoryData/Mods/`

---

## Building from Source

Requirements:
- .NET 10.0 SDK
- Vintage Story 1.22.x

```bash
# Build the mod DLL
dotnet build ClassicSayHi.csproj -c Release

# Package the release zip
./pack.sh
```

---

## Contributing & Support

Contributions, bug reports, and translation improvements are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

If you would like to support the project:

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support%20Zaldaryon-FF5E5B?style=for-the-badge&logo=kofi&logoColor=white)](https://ko-fi.com/Zaldaryon)

---

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
