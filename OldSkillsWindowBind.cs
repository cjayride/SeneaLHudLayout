using HarmonyLib;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SkillsReworked newer than 1.8.2 is drawn by SeneaL UI.
    /// 1.8.2 and older stay on the vanilla skills window, which still has the
    /// Skills and Active skills tabs. SeneaL UI's panel has no place for those tabs.
    /// </summary>
    static class OldSkillsWindowBind
    {
        public static void Apply(Harmony harmony)
        {
        }
    }
}
