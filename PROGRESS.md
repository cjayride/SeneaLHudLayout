# PROGRESS — SeneaLHudLayout 1.3.7

## Goal
Fix shard window stealing the player inventory when selling/recycling socketed legendaries.

## Status
Released 1.3.7.

## Cause
E is the shard hotkey and Haldor/recycle confirm. AfterEpicOpen hosted any ShardUI.Open and Dock reparented the Epic Loot panel (sometimes the player grid). After disconnect, `_hosting` stayed true.

## Fix
- Host only when we opened the window (`_wantHost`)
- Ignore E while StoreGui or SeneaL recycle is open
- Close if the hosted item is no longer in the bag
- Never reparent the player inventory grid
- CharacterAttributes stays out of the compile and is not ticked

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`
- Zip (flat, no subfolders): `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\Cjayride.SeneaLHudLayout-1.3.7.zip`
