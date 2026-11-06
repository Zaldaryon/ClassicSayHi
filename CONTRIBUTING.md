# Contributing to Classic Say Hi :)

Thank you for your interest in contributing to Classic Say Hi :)!

---

## Support the Project

If you enjoy this mod and want to support ongoing maintenance and new features, consider supporting on Ko-fi:

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support%20Zaldaryon-FF5E5B?style=for-the-badge&logo=kofi&logoColor=white)](https://ko-fi.com/Zaldaryon)

**Link:** [https://ko-fi.com/Zaldaryon](https://ko-fi.com/Zaldaryon)

---

## Reporting Issues

If you encounter a bug, translation error, or compatibility issue:

1. Check existing issues to see if someone already reported the problem.
2. Open a new issue with:
   - Your game version (e.g., Vintage Story 1.22.7).
   - Your server environment (Dedicated or Singleplayer).
   - Server language and relevant lines from `server-main.log`.
   - Steps to reproduce the problem.

---

## Translations

We welcome corrections and improvements to translations for any of Vintage Story's supported languages.

1. Locate the language file in `assets/game/lang/<language_code>.json`.
2. Update the translation string ensuring `{0}` remains intact for player name interpolation.
3. Submit a pull request explaining the correction.

---

## Code Contributions

1. Fork the repository and create a feature branch.
2. Build with .NET 10.0 SDK:
   ```bash
   dotnet build ClassicSayHi.csproj -c Release
   ```
3. Test locally using `./pack.sh`.
4. Submit a pull request describing your changes.

---

## Code of Conduct

Be welcoming, respectful, and constructive in all interactions.
