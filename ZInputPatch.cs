namespace Tractor;

using HarmonyLib;

using static PluginConfig;

[HarmonyPatch(typeof(ZInput))]
static class ZInputPatch {
  [HarmonyPrefix]
  [HarmonyPatch(nameof(ZInput.GetMouseScrollWheel))]
  static bool GetMouseScrollWheelPrefix(ref float __result) {
    if (!IsModEnabled.Value || !TractorPanel.IsOpen) {
      return true;
    }

    __result = 0f;

    return false;
  }
}
