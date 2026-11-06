using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace ClassicSayHi;

public class ClassicSayHiConfig
{
    public const string DefaultGreeting = "{0} joined. Say hi :)";

    public bool Enabled { get; set; } = true;
    public string DefaultFormat { get; set; } = DefaultGreeting;
    public Dictionary<string, string> FormatsByLanguage { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ClassicSayHiModSystem : ModSystem
{
    private ICoreAPI api;
    private ICoreServerAPI sapi;
    private ClassicSayHiConfig config;
    private const string ConfigFileName = "classicsayhi.json";
    private readonly Dictionary<string, string> originalTranslations = new(StringComparer.OrdinalIgnoreCase);

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    public override void Start(ICoreAPI api)
    {
        this.api = api;
        LoadConfig(api);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        var cmd = api.ChatCommands.Create("sayhi")
            .RequiresPrivilege(Privilege.controlserver)
            .WithDescription("Classic Say Hi configuration and commands")
            .HandleWith(OnCmdStatus);

        cmd.BeginSubCommand("status")
            .WithDescription("Show current join message format and status")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("language"))
            .HandleWith(OnCmdStatus)
            .EndSubCommand();

        cmd.BeginSubCommand("set")
            .WithDescription("Set join message format with {0}. Optional language prefix: /sayhi set [lang] <format>")
            .WithArgs(api.ChatCommands.Parsers.All("args"))
            .HandleWith(OnCmdSet)
            .EndSubCommand();

        cmd.BeginSubCommand("setlang")
            .WithDescription("Set join message format for a specific language: /sayhi setlang <lang> <format>")
            .WithArgs(api.ChatCommands.Parsers.Word("language"), api.ChatCommands.Parsers.All("format"))
            .HandleWith(OnCmdSetLang)
            .EndSubCommand();

        cmd.BeginSubCommand("reset")
            .WithDescription("Reset format to default. Optional language: /sayhi reset [lang]")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("language"))
            .HandleWith(OnCmdReset)
            .EndSubCommand();

        cmd.BeginSubCommand("reload")
            .WithDescription("Reload configuration from disk")
            .HandleWith(OnCmdReload)
            .EndSubCommand();

        cmd.BeginSubCommand("enable")
            .WithDescription("Enable Classic Say Hi join messages")
            .HandleWith(OnCmdEnable)
            .EndSubCommand();

        cmd.BeginSubCommand("disable")
            .WithDescription("Disable Classic Say Hi (restores vanilla 1.22 join message)")
            .HandleWith(OnCmdDisable)
            .EndSubCommand();

        cmd.BeginSubCommand("toggle")
            .WithDescription("Toggle Classic Say Hi on/off")
            .HandleWith(OnCmdToggle)
            .EndSubCommand();
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        foreach (var (code, service) in Lang.AvailableLanguages)
        {
            if (service.GetAllEntries().TryGetValue("game:player-joined", out var orig))
            {
                originalTranslations[code] = orig;
            }
        }
        ApplyConfigToLang();
    }

    private void LoadConfig(ICoreAPI coreApi)
    {
        try
        {
            var loaded = coreApi.LoadModConfig<ClassicSayHiConfig>(ConfigFileName);
            if (loaded != null)
            {
                config = loaded;
                config.FormatsByLanguage ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (string.IsNullOrWhiteSpace(config.DefaultFormat))
                {
                    config.DefaultFormat = ClassicSayHiConfig.DefaultGreeting;
                }
                return;
            }
        }
        catch (Exception ex)
        {
            coreApi.Logger.Warning($"[ClassicSayHi] Failed loading {ConfigFileName}: {ex.Message}. Preserving existing configuration.");
            if (config != null) return;
        }

        config = new ClassicSayHiConfig();
        SaveConfig();
    }

    private void SaveConfig()
    {
        var targetApi = sapi ?? (api as ICoreServerAPI);
        if (targetApi == null) return;

        try
        {
            targetApi.StoreModConfig(config, ConfigFileName);
        }
        catch (Exception ex)
        {
            targetApi.Logger.Warning($"[ClassicSayHi] Failed saving {ConfigFileName}: {ex.Message}");
        }
    }

    private void ApplyConfigToLang()
    {
        if (config == null) return;

        bool hasCustomDefault = !string.IsNullOrWhiteSpace(config.DefaultFormat)
            && !string.Equals(config.DefaultFormat, ClassicSayHiConfig.DefaultGreeting, StringComparison.Ordinal);

        foreach (var (code, service) in Lang.AvailableLanguages)
        {
            string formatToUse;

            if (!config.Enabled)
            {
                formatToUse = "{0} joined";
            }
            else if (config.FormatsByLanguage.TryGetValue(code, out var specific) && !string.IsNullOrWhiteSpace(specific))
            {
                formatToUse = specific;
            }
            else if (hasCustomDefault)
            {
                formatToUse = config.DefaultFormat;
            }
            else if (originalTranslations.TryGetValue(code, out var orig) && !string.IsNullOrWhiteSpace(orig))
            {
                formatToUse = orig;
            }
            else
            {
                formatToUse = config.DefaultFormat ?? ClassicSayHiConfig.DefaultGreeting;
            }

            var entries = service.GetAllEntries();
            lock (entries)
            {
                entries["game:player-joined"] = formatToUse;
                entries["player-joined"] = formatToUse;
            }
        }
    }

    private static bool TryValidateFormat(string candidate, out string error)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            error = "Format cannot be empty.";
            return false;
        }

        if (!candidate.Contains("{0}"))
        {
            error = "Format must contain {0} placeholder for player name. Example: {0} joined. Say hi :)";
            return false;
        }

        if (candidate.Contains("{p"))
        {
            error = "Plural format syntax ({p...}) is not permitted in join broadcast messages.";
            return false;
        }

        try
        {
            _ = string.Format(candidate, "TestPlayer");
        }
        catch (FormatException ex)
        {
            error = $"Invalid format string: {ex.Message}";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsLanguageCode(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Contains("{")) return false;
        token = token.ToLowerInvariant();
        return Lang.AvailableLanguages.ContainsKey(token);
    }

    private TextCommandResult OnCmdStatus(TextCommandCallingArgs args)
    {
        string requestedLang = (args.Parsers.Count > 0 && !args.Parsers[0].IsMissing) ? ((string)args[0])?.ToLowerInvariant() : null;
        string serverLang = Lang.CurrentLocale ?? "en";
        string langToCheck = requestedLang ?? serverLang;

        if (requestedLang != null && !Lang.AvailableLanguages.ContainsKey(requestedLang))
        {
            return TextCommandResult.Error($"Language '{requestedLang}' is not registered in Vintage Story.");
        }

        string current = Lang.GetL(langToCheck, "player-joined", "Player");
        string defFmt = config.DefaultFormat ?? ClassicSayHiConfig.DefaultGreeting;
        string specFmt = config.FormatsByLanguage.TryGetValue(langToCheck, out var s) ? s : "(Fallback to default)";

        string overrides = config.FormatsByLanguage.Count > 0
            ? string.Join(", ", config.FormatsByLanguage.Keys)
            : "none";

        return TextCommandResult.Success(
            $"[ClassicSayHi] Status: {(config.Enabled ? "Enabled" : "Disabled")}\n" +
            $"Server language: {serverLang}\n" +
            $"Language checked: {langToCheck}\n" +
            $"Default/Fallback format: {defFmt}\n" +
            $"Specific format for {langToCheck}: {specFmt}\n" +
            $"Configured overrides: {overrides}\n" +
            $"Preview: {current}");
    }

    private TextCommandResult OnCmdSet(TextCommandCallingArgs args)
    {
        string raw = ((string)args[0])?.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            return TextCommandResult.Error("Usage: /sayhi set [language] <format with {0}>\nExamples:\n  /sayhi set {0} joined. Welcome :)\n  /sayhi set pt-br {0} entrou. Diga ola :)");
        }

        string targetLang = null;
        string format;

        int spaceIdx = raw.IndexOf(' ');
        if (spaceIdx > 0)
        {
            string firstWord = raw.Substring(0, spaceIdx).Trim();
            if (IsLanguageCode(firstWord))
            {
                targetLang = firstWord.ToLowerInvariant();
                format = raw.Substring(spaceIdx + 1).Trim();
            }
            else if (firstWord.Contains('-') || firstWord.Contains('_'))
            {
                return TextCommandResult.Error($"Unknown language code '{firstWord}'. Check available language codes in /sayhi status or omit the prefix to set the default format.");
            }
            else
            {
                format = raw;
            }
        }
        else
        {
            if (IsLanguageCode(raw))
            {
                return TextCommandResult.Error($"Please provide a format after the language code. Example: /sayhi set {raw} {{0}} joined. Welcome :)");
            }
            format = raw;
        }

        if (!TryValidateFormat(format, out var formatError))
        {
            return TextCommandResult.Error(formatError);
        }

        config.Enabled = true;

        if (string.IsNullOrEmpty(targetLang) || targetLang == "en")
        {
            config.DefaultFormat = format;
            config.FormatsByLanguage["en"] = format;
            ApplyConfigToLang();
            SaveConfig();
            return TextCommandResult.Success($"[ClassicSayHi] Default (English & fallback) join message updated:\n  \"{format}\"\nPreview ({targetLang ?? "en"}): {Lang.GetL(targetLang ?? "en", "player-joined", "Player")}");
        }
        else
        {
            config.FormatsByLanguage[targetLang] = format;
            ApplyConfigToLang();
            SaveConfig();
            return TextCommandResult.Success($"[ClassicSayHi] Join message updated for [{targetLang}]:\n  \"{format}\"\nPreview: {Lang.GetL(targetLang, "player-joined", "Player")}");
        }
    }

    private TextCommandResult OnCmdSetLang(TextCommandCallingArgs args)
    {
        string lang = ((string)args[0])?.Trim().ToLowerInvariant();
        string format = ((string)args[1])?.Trim();

        if (string.IsNullOrEmpty(lang) || !Lang.AvailableLanguages.ContainsKey(lang))
        {
            return TextCommandResult.Error($"Language '{lang}' is not registered in Vintage Story.");
        }

        if (!TryValidateFormat(format, out var formatError))
        {
            return TextCommandResult.Error(formatError);
        }

        config.Enabled = true;

        if (lang == "en")
        {
            config.DefaultFormat = format;
            config.FormatsByLanguage["en"] = format;
        }
        else
        {
            config.FormatsByLanguage[lang] = format;
        }

        ApplyConfigToLang();
        SaveConfig();
        return TextCommandResult.Success($"[ClassicSayHi] Join message updated for [{lang}]:\n  \"{format}\"\nPreview: {Lang.GetL(lang, "player-joined", "Player")}");
    }

    private TextCommandResult OnCmdReset(TextCommandCallingArgs args)
    {
        string lang = (args.Parsers.Count > 0 && !args.Parsers[0].IsMissing) ? ((string)args[0])?.ToLowerInvariant() : null;

        if (string.IsNullOrEmpty(lang))
        {
            config.DefaultFormat = ClassicSayHiConfig.DefaultGreeting;
            config.FormatsByLanguage.Clear();
            config.Enabled = true;
            ApplyConfigToLang();
            SaveConfig();
            return TextCommandResult.Success("[ClassicSayHi] All formats reset to default localized messages.");
        }

        if (!Lang.AvailableLanguages.ContainsKey(lang))
        {
            return TextCommandResult.Error($"Language '{lang}' is not registered in Vintage Story.");
        }

        if (lang == "en")
        {
            config.DefaultFormat = ClassicSayHiConfig.DefaultGreeting;
            config.FormatsByLanguage.Remove("en");
        }
        else
        {
            config.FormatsByLanguage.Remove(lang);
        }

        ApplyConfigToLang();
        SaveConfig();
        return TextCommandResult.Success($"[ClassicSayHi] Format for [{lang}] reset. Preview: {Lang.GetL(lang, "player-joined", "Player")}");
    }

    private TextCommandResult OnCmdReload(TextCommandCallingArgs args)
    {
        LoadConfig(sapi);
        ApplyConfigToLang();
        return TextCommandResult.Success("[ClassicSayHi] Config reloaded and applied.");
    }

    private TextCommandResult OnCmdEnable(TextCommandCallingArgs args)
    {
        config.Enabled = true;
        ApplyConfigToLang();
        SaveConfig();
        return TextCommandResult.Success("[ClassicSayHi] Mod enabled. Classic join messages active.");
    }

    private TextCommandResult OnCmdDisable(TextCommandCallingArgs args)
    {
        config.Enabled = false;
        ApplyConfigToLang();
        SaveConfig();
        return TextCommandResult.Success("[ClassicSayHi] Mod disabled. Reverted to vanilla 1.22 join messages.");
    }

    private TextCommandResult OnCmdToggle(TextCommandCallingArgs args)
    {
        config.Enabled = !config.Enabled;
        ApplyConfigToLang();
        SaveConfig();
        return TextCommandResult.Success($"[ClassicSayHi] Mod is now {(config.Enabled ? "enabled" : "disabled")}.");
    }
}
