namespace Tractor;

using HarmonyLib;

using static PluginConfig;

[HarmonyPatch(typeof(global::Console))]
static class ConsolePatch {
  [HarmonyPrefix]
  [HarmonyPatch(nameof(global::Console.Update))]
  static bool UpdatePrefix() {
    if (!IsModEnabled.Value) {
      return true;
    }

    return !TractorPanel.IsOpen;
  }
}
