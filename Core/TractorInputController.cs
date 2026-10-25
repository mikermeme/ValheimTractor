namespace Tractor;

using UnityEngine;

using static PluginConfig;

public static class TractorInputController {
  static int _shortcutFrame = -1;

  public static bool TakeInput() {
    if (_shortcutFrame == Time.frameCount) {
      return false;
    }

    if (IsShortcutPressed()) {
      _shortcutFrame = Time.frameCount;
      TractorPanel.ToggleHovered();
      return false;
    }

    return !TractorPanel.IsOpen;
  }

  static bool IsShortcutPressed() {
    return ZInput.GetKeyDown(OpenKey.Value) || (IsShiftHeld() && ZInput.GetMouseButtonDown(1));
  }

  static bool IsShiftHeld() {
    return ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift);
  }
}
