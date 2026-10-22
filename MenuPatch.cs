namespace Tractor;

using HarmonyLib;

using static PluginConfig;

[HarmonyPatch(typeof(Menu))]
static class MenuPatch {
  [HarmonyPrefix]
  [HarmonyPatch(nameof(Menu.Update))]
  static bool UpdatePrefix() {
    if (!IsModEnabled.Value) {
      return true;
    }

    return !TractorPanel.IsOpen && !TractorPanel.WasClosedThisFrame;
  }
}
