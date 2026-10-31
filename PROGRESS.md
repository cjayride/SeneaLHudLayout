# PROGRESS — SeneaLHudLayout 1.3.10

## Goal
Dragged shard icons must stay visible over the hosted shard window.

## Status
Released 1.3.10.

## Cause
1.3.9 put the shard panel at ~29000. SeneaL `Inv.Selection` drag ghosts use 28000, so the icon slid under the panel and only appeared at the edges.

## Fix
- Shard panel sort ~27000 (above inventory/craft, below drag).
- While hosting, keep SeneaL selection layer and `InventoryGui.m_dragGo` at 28000.

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`
- Zip (flat): `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\Cjayride.SeneaLHudLayout-1.3.10.zip`
