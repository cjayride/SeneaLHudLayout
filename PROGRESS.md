# Progress - SeneaLHudLayout

## Current version: 1.3.29

## Goal
Add `[Chat] ChatSettingsEnabled` as a master on/off for all Chat settings except HideChat. Change RememberLastUsed and chatMessageFontEnabled defaults to false.

## Status: completed (build + package)

## Acceptance
- HideChat stays independent.
- ChatSettingsEnabled off: DefaultToShout, RememberLastUsed, ShoutKeepsCapitalization, HideChatPanelDelay, chat font keys do not apply.
- RememberLastUsed default false.
- chatMessageFontEnabled default false.

## Files changed
- SeneaLHudLayoutPlugin.cs, ChatPrefix.cs, ChatFont.cs, ChatHide.cs, ChatShoutText.cs
- Package/config, README, CHANGELOG, manifest
- Version 1.3.29

## Commands
- `dotnet build SeneaLHudLayout.csproj -c Release` — succeeded (6 existing warnings, 0 errors)
- Packaged `Package\*` to `dist\1.3.29.zip`

## Output
- `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll` 1.3.29
- `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\1.3.29.zip`
