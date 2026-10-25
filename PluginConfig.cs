namespace Tractor;

using BepInEx.Configuration;

using ComfyLib;

using UnityEngine;

public static class PluginConfig {
  public static ConfigEntry<bool> IsModEnabled { get; private set; }
  public static ConfigEntry<KeyCode> OpenKey { get; private set; }
  public static ConfigEntry<Vector2> PanelPosition { get; private set; }
  public static ConfigEntry<float> BulkRadius { get; private set; }
  public static ConfigEntry<float> LookRadius { get; private set; }
  public static ConfigEntry<bool> AutoApply { get; private set; }
  public static ConfigEntry<UiTheme> Theme { get; private set; }
  public static ConfigEntry<UiFont> FontChoice { get; private set; }

  public static readonly Vector2 DefaultPanelPosition = new(-620f, 0f);

  public static void BindConfig(ConfigFile config) {
    IsModEnabled =
        config.BindInOrder(
            "_Global",
            "isModEnabled",
            true,
            "Globally enable or disable this mod.");

    IsModEnabled.OnSettingChanged(TractorPanel.HandleModEnabledChanged);

    OpenKey =
        config.BindInOrder(
            "Panel",
            "openKey",
            KeyCode.F8,
            "Open the ZDO editor on the hovered object. Shift+RightClick also opens and closes it.");

    PanelPosition =
        config.BindInOrder(
            "Panel",
            "position",
            DefaultPanelPosition,
            "Panel position relative to the center of the screen.");

    PanelPosition.OnSettingChanged(TractorPanel.ApplyPanelPosition);

    BulkRadius =
        config.BindInOrder(
            "Panel",
            "bulkRadius",
            32f,
            "Radius in meters for the radius bulk action.",
            new AcceptableValueRange<float>(1f, 256f));

    BulkRadius.OnSettingChanged(TractorPanel.HandleBulkRadiusChanged);

    LookRadius =
        config.BindInOrder(
            "Selection",
            "lookRadius",
            0f,
            "Radius in meters of the sphere used when Shift+RightClick has no hover target. The sphere touches"
            + " the player and reaches along the look direction; the search grows to twice and four times this"
            + " radius before giving up. The default of 0 disables the search.",
            new AcceptableValueRange<float>(0f, 20f));

    AutoApply =
        config.BindInOrder(
            "Panel",
            "autoApply",
            true,
            "Apply the selected value when the editor loses focus.");

    Theme =
        config.BindInOrder(
            "Panel",
            "uiTheme",
            UiTheme.Vanilla,
            "Panel skin. Vanilla uses the game's wood, button, and text-field sprites."
            + " Takes effect the next time the panel opens.");

    Theme.OnSettingChanged(TractorPanel.HandleThemeChanged);

    FontChoice =
        config.BindInOrder(
            "Panel",
            "uiFont",
            UiFont.AveriaSans,
            "Panel font. AveriaSans uses the game's sans face, the one ComfySpawner builds its panels from;"
            + " Game uses the HUD hover-name face. Takes effect the next time the panel opens.");

    FontChoice.OnSettingChanged(TractorPanel.HandleFontChanged);
  }
}
