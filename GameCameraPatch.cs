namespace Tractor;

using HarmonyLib;

using UnityEngine;

using static PluginConfig;

[HarmonyPatch(typeof(GameCamera))]
static class GameCameraPatch {
  [HarmonyPrefix]
  [HarmonyPatch(nameof(GameCamera.UpdateMouseCapture))]
  static bool UpdateMouseCapturePrefix() {
    if (!IsModEnabled.Value || !TractorPanel.IsOpen) {
      return true;
    }

    ZCursor.LockState = CursorLockMode.None;
    ZCursor.Show();

    return false;
  }

  [HarmonyPrefix]
  [HarmonyPatch(nameof(GameCamera.UpdateFreeFly))]
  static bool UpdateFreeFlyPrefix() {
    if (!IsModEnabled.Value) {
      return true;
    }

    return !TractorPanel.IsOpen;
  }
}
