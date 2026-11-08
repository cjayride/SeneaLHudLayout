# Progress - SeneaLHudLayout

## Goal
1.5.25: When chat input is closed, a new message jumps the log to the latest lines so you are not stuck scrolled up.

## Status
Built (Release). Not packaged / not committed.

## Files changed
- `ChatWindowLayout.cs` — prefix on `Terminal.AddString(string)` zeros `m_scrollHeight` when the chat box is not open
- `SeneaLHudLayoutPlugin.cs` — `[Chat] ChatAutoScrollOnNewMessage` (default on), version 1.5.25
- README / changelog / manifest / csproj

## Verification
- `dotnet build -c Release` — succeeded
- **DLL:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`

## Next
- In-game: scroll chat up, close the box, have someone send a line — log should jump to latest
- With the box still open, scrolled-up view should stay
