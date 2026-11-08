using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace SeneaLHudLayout
{
    /// <summary>
    /// BepInEx remembers every config line it does not recognize (a section that was renamed,
    /// a setting that was removed) and writes those lines back on every save, so legacy keys
    /// pile up in player config files forever. Once this plugin's settings are bound, this
    /// prunes the lines the current version no longer knows about:
    ///   - clears BepInEx's in-memory list of unrecognized lines (the public API cannot remove
    ///     them, and that list is what rewrites them on every save), so they never come back;
    ///   - rewrites the file without those lines, and without the doc comments and section
    ///     headers that belonged only to them, keeping every other line byte-for-byte.
    /// The file is not re-saved, so the player's own comments and layout are preserved.
    /// Values of renamed settings are migrated by the ConfigDefault(...) bindings before this
    /// runs. A config with nothing to prune is never touched.
    /// </summary>
    internal static class ConfigHousekeeping
    {
        // Safety valve: more unbound lines than this means the file is from a version too
        // different to prune safely. Nothing is removed; the count is logged once.
        const int MaxPrunePerLoad = 200;

        public static void PruneLegacyEntries(ConfigFile config, ManualLogSource log)
        {
            try
            {
                string path = config.ConfigFilePath;

                ClearUnrecognizedLines(config);

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return;
                }

                var bound = new HashSet<string>();
                foreach (ConfigDefinition definition in config.Keys)
                {
                    bound.Add(definition.Section + "|" + definition.Key);
                }
                if (bound.Count == 0)
                {
                    return;
                }

                string[] lines = File.ReadAllLines(path);
                List<string> unbound = FindUnbound(lines, bound);

                if (unbound.Count == 0)
                {
                    return;
                }

                if (unbound.Count > MaxPrunePerLoad)
                {
                    log.LogWarning("Config housekeeping: found " + unbound.Count
                        + " entries this version does not know about (over the "
                        + MaxPrunePerLoad + " safety limit). Nothing was removed.");
                    return;
                }

                List<string> kept = DropUnbound(lines, bound);
                File.WriteAllLines(path, kept, new UTF8Encoding(false));

                log.LogInfo("Config housekeeping: removed " + unbound.Count
                    + " leftover entries from " + Path.GetFileName(path)
                    + " (renamed or removed settings; the values of renamed settings were kept): "
                    + string.Join(", ", unbound.ToArray()));
            }
            catch (Exception ex)
            {
                log.LogWarning("Config housekeeping skipped: " + ex.Message);
            }
        }

        static List<string> FindUnbound(string[] lines, HashSet<string> bound)
        {
            var found = new List<string>();
            string section = "";
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]") && line.Length > 2)
                {
                    section = line.Substring(1, line.Length - 2);
                    continue;
                }
                if (IsKeyLine(line))
                {
                    string key = line.Substring(0, line.IndexOf('=')).Trim();
                    if (!bound.Contains(section + "|" + key))
                    {
                        found.Add("[" + section + "] " + key);
                    }
                }
            }
            return found;
        }

        static List<string> DropUnbound(string[] lines, HashSet<string> bound)
        {
            var drop = new bool[lines.Length];
            string section = "";
            var sectionHeader = new Dictionary<string, int>();
            var sectionKeyCount = new Dictionary<string, int>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("[") && line.EndsWith("]") && line.Length > 2)
                {
                    section = line.Substring(1, line.Length - 2);
                    sectionHeader[section] = i;
                    if (!sectionKeyCount.ContainsKey(section))
                    {
                        sectionKeyCount[section] = 0;
                    }
                    continue;
                }

                if (!IsKeyLine(line))
                {
                    continue;
                }

                string key = line.Substring(0, line.IndexOf('=')).Trim();
                if (bound.Contains(section + "|" + key))
                {
                    sectionKeyCount[section] = sectionKeyCount[section] + 1;
                    continue;
                }

                drop[i] = true;
                DropDocComment(lines, i, drop);
            }

            // Section headers whose keys were all pruned, and the blank lines that framed them.
            foreach (var pair in sectionKeyCount)
            {
                if (pair.Value == 0 && sectionHeader.ContainsKey(pair.Key))
                {
                    int header = sectionHeader[pair.Key];
                    drop[header] = true;
                    int after = header + 1;
                    while (after < lines.Length && lines[after].Trim().Length == 0)
                    {
                        drop[after] = true;
                        after++;
                    }
                }
            }

            var kept = new List<string>(lines.Length);
            int blanks = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (drop[i])
                {
                    continue;
                }
                bool blank = lines[i].Trim().Length == 0;
                if (blank)
                {
                    blanks++;
                    if (blanks > 1 && kept.Count > 0)
                    {
                        continue;
                    }
                }
                else
                {
                    blanks = 0;
                }
                kept.Add(lines[i]);
            }

            while (kept.Count > 0 && kept[kept.Count - 1].Trim().Length == 0)
            {
                kept.RemoveAt(kept.Count - 1);
            }
            return kept;
        }

        /// <summary>
        /// The doc comment BepInEx writes above a key: the contiguous run of # lines directly
        /// above it. Also drop the single blank line that separates that block from the
        /// previous setting.
        /// </summary>
        static void DropDocComment(string[] lines, int keyIndex, bool[] drop)
        {
            int i = keyIndex - 1;
            while (i >= 0 && lines[i].Trim().StartsWith("#"))
            {
                drop[i] = true;
                i--;
            }
            if (i >= 0 && lines[i].Trim().Length == 0)
            {
                drop[i] = true;
            }
        }

        static bool IsKeyLine(string line)
        {
            if (line.Length == 0)
            {
                return false;
            }
            if (line.StartsWith("#") || line.StartsWith("["))
            {
                return false;
            }
            int equals = line.IndexOf('=');
            return equals > 0;
        }

        /// <summary>
        /// BepInEx keeps config lines it did not bind in a private dictionary and writes them
        /// back on every save, so old keys never disappear on their own. Clearing it after the
        /// bindings exist makes the file prune permanent. If the field is ever renamed this is
        /// a no-op and the file is still pruned; the keys would reappear at the next save.
        /// </summary>
        static void ClearUnrecognizedLines(ConfigFile config)
        {
            try
            {
                var field = typeof(ConfigFile).GetField("OrphanedEntries",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null)
                {
                    return;
                }
                object dict = field.GetValue(config);
                if (dict == null)
                {
                    return;
                }
                var clear = dict.GetType().GetMethod("Clear");
                if (clear != null)
                {
                    clear.Invoke(dict, null);
                }
            }
            catch
            {
                // Not fatal: the file rewrite in PruneLegacyEntries covers the on-disk side.
            }
        }
    }
}
