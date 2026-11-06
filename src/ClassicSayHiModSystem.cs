using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace ClassicSayHi;

public class ClassicSayHiConfig
{
    public bool Enabled { get; set; } = true;
    public string DefaultFormat { get; set; } = null;
    public Dictionary<string, string> FormatsByLanguage { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ClassicSayHiModSystem : ModSystem
{
    private ICoreServerAPI sapi;
    private ClassicSayHiConfig config;
    private const string ConfigFileName = "classicsayhi.json";
    private readonly Dictionary<string, string> originalTranslations = new(StringComparer.OrdinalIgnoreCase);

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        LoadConfig();

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

        cmd.BeginSubCommand("reset")
            .WithDescription("Reset format to default. Optional language: /sayhi reset [lang]")
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("language"))
            .HandleWith(OnCmdReset)
            .EndSubCommand();

        cmd.BeginSubCommand("reload")
            .WithDescription("Reload configuration from disk")
            .HandleWith(OnCmdReload)
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

    private void LoadConfig()
    {
        try
        {
            config = sapi.LoadModConfig<ClassicSayHiConfig>(ConfigFileName) ?? new ClassicSayHiConfig();
            config.FormatsByLanguage ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            sapi.Logger.Warning($"[ClassicSayHi] Failed loading {ConfigFileName}: {ex.Message}. Using defaults.");
            config = new ClassicSayHiConfig();
        }
        SaveConfig();
    }

    private void SaveConfig()
    {
        try
        {
            sapi.StoreModConfig(config, ConfigFileName);
        }
        catch (Exception ex)
        {
            sapi.Logger.Warning($"[ClassicSayHi] Failed saving {ConfigFileName}: {ex.Message}");
        }
    }

    private void ApplyConfigToLang()
    {
        if (!config.Enabled) return;

        foreach (var (code, service) in Lang.AvailableLanguages)
        {
            string formatToUse;
            if (config.FormatsByLanguage.TryGetValue(code, out var specific) && !string.IsNullOrEmpty(specific))
            {
                formatToUse = specific;
            }
            else if (!string.IsNullOrEmpty(config.DefaultFormat))
            {
                formatToUse = config.DefaultFormat;
            }
            else if (originalTranslations.TryGetValue(code, out var orig))
            {
                formatToUse = orig;
            }
            else
            {
                continue;
            }

            service.GetAllEntries()["game:player-joined"] = formatToUse;
            service.GetAllEntries()["player-joined"] = formatToUse;
        }
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
        string serverLang = sapi.World.Config.GetString("serverLanguage", "en");
        string langToCheck = requestedLang ?? serverLang;

        string current = Lang.GetL(langToCheck, "player-joined", "Player");
        string defFmt = config.DefaultFormat ?? "(Default localized pre-1.22)";
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
            else
            {
                format = raw;
            }
        }
        else
        {
            format = raw;
        }

        if (!format.Contains("{0}"))
        {
            return TextCommandResult.Error("Format must contain {0} placeholder for player name. Example: /sayhi set {0} joined. Welcome :)");
        }

        config.Enabled = true;

        if (string.IsNullOrEmpty(targetLang) || targetLang == "en")
        {
            config.DefaultFormat = format;
            config.FormatsByLanguage["en"] = format;
            SaveConfig();
            ApplyConfigToLang();
            return TextCommandResult.Success($"[ClassicSayHi] Default (English & fallback) join message updated:\n  \"{format}\"\nPreview ({targetLang ?? "en"}): {Lang.GetL(targetLang ?? "en", "player-joined", "Player")}");
        }
        else
        {
            config.FormatsByLanguage[targetLang] = format;
            SaveConfig();
            ApplyConfigToLang();
            return TextCommandResult.Success($"[ClassicSayHi] Join message updated for [{targetLang}]:\n  \"{format}\"\nPreview: {Lang.GetL(targetLang, "player-joined", "Player")}");
        }
    }

    private TextCommandResult OnCmdReset(TextCommandCallingArgs args)
    {
        string lang = (args.Parsers.Count > 0 && !args.Parsers[0].IsMissing) ? ((string)args[0])?.ToLowerInvariant() : null;

        if (string.IsNullOrEmpty(lang))
        {
            config.DefaultFormat = null;
            config.FormatsByLanguage.Clear();
            config.Enabled = true;
            SaveConfig();
            ApplyConfigToLang();
            return TextCommandResult.Success("[ClassicSayHi] All formats reset to default localized messages.");
        }

        if (lang == "en")
        {
            config.DefaultFormat = null;
            config.FormatsByLanguage.Remove("en");
        }
        else
        {
            config.FormatsByLanguage.Remove(lang);
        }

        SaveConfig();
        ApplyConfigToLang();
        return TextCommandResult.Success($"[ClassicSayHi] Format for [{lang}] reset. Preview: {Lang.GetL(lang, "player-joined", "Player")}");
    }

    private TextCommandResult OnCmdReload(TextCommandCallingArgs args)
    {
        LoadConfig();
        ApplyConfigToLang();
        return TextCommandResult.Success("[ClassicSayHi] Config reloaded and applied.");
    }
}
