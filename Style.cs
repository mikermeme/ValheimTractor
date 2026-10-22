namespace Tractor;

using System;
using System.Collections.Generic;
using System.Reflection;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public enum UiTheme {
  Vanilla,
  Dark,
}

public enum UiFont {
  Game,
  AveriaSans,
}

public static class Style {
  static readonly string[] SpriteNames = [
      "woodpanel_trophys",
      "item_background",
      "text_field",
      "button",
      "button_highlight",
      "button_pressed",
      "button_disabled",
      "Background",
      "UISprite",
  ];

  static readonly string[] MaterialNames = [
      "litpanel",
  ];

  static readonly Color DarkPanelColor = new(0.05f, 0.05f, 0.05f, 0.94f);
  static readonly Color DarkListColor = new(0f, 0f, 0f, 0.35f);
  static readonly Color DarkInputColor = new(0f, 0f, 0f, 0.55f);
  static readonly Color FavoriteOnColor = new(1f, 0.78f, 0.16f, 1f);
  static readonly Color FavoriteOffColor = new(0.6f, 0.6f, 0.6f, 1f);

  static Dictionary<string, Sprite> _sprites;
  static Dictionary<string, Material> _materials;
  static bool? _hasStarGlyph;
  static TMP_FontAsset _uiFontAsset;

  public static bool IsVanilla {
    get { return PluginConfig.Theme != null && PluginConfig.Theme.Value == UiTheme.Vanilla; }
  }

  // Labels clone the HUD hover name, which is the game's serif face. The Averia Sans option uses the
  // popup body font, which is the face ComfySpawner builds its own panels from.
  public static TMP_FontAsset UiFontAsset {
    get {
      if (!_uiFontAsset) {
        _uiFontAsset = ResolveUiFont();
      }

      return _uiFontAsset;
    }
  }

  public static void ResetUiFont() {
    _uiFontAsset = null;
    _hasStarGlyph = null;
    _keyGlyph = null;
  }

  // Averia Sans sets tighter than the HUD face at the same point size, so nudge it up to match.
  public static float FontScale {
    get {
      return PluginConfig.FontChoice != null && PluginConfig.FontChoice.Value == UiFont.AveriaSans
          ? 1.1f
          : 1f;
    }
  }

  public static float FontSize(float size) {
    return size * FontScale;
  }

  static TMP_FontAsset ResolveUiFont() {
    TMP_FontAsset gameFont =
        Hud.instance && Hud.instance.m_hoverName ? Hud.instance.m_hoverName.font : null;

    if (PluginConfig.FontChoice == null || PluginConfig.FontChoice.Value != UiFont.AveriaSans) {
      return gameFont;
    }

    TMP_FontAsset sansFont = FindSansFont();

    if (sansFont && sansFont.material) {
      Tractor.LogInfo($"Panel font: '{sansFont.name}'.");
      return sansFont;
    }

    Tractor.LogWarning("No sans font asset found, keeping the HUD hover name font.");
    return gameFont;
  }

  // ComfySpawner builds its labels from the popup body text, so prefer the same face. Both members are
  // private, and a direct field access fails Mono's JIT access check in the real assembly, so read them
  // through reflection.
  static TMP_FontAsset FindSansFont() {
    try {
      object popup =
          typeof(UnifiedPopup)
              .GetField("instance", BindingFlags.NonPublic | BindingFlags.Static)
              ?.GetValue(null);

      TextMeshProUGUI bodyText =
          popup == null
              ? null
              : typeof(UnifiedPopup)
                  .GetField("bodyText", BindingFlags.NonPublic | BindingFlags.Instance)
                  ?.GetValue(popup) as TextMeshProUGUI;

      if (bodyText && bodyText.font) {
        return bodyText.font;
      }
    } catch (Exception exception) {
      Tractor.LogWarning($"Cannot read the popup body font: {exception.Message}");
    }

    TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

    for (int i = 0; i < loadedFonts.Length; i++) {
      if (loadedFonts[i].name.IndexOf("sans", StringComparison.OrdinalIgnoreCase) >= 0) {
        return loadedFonts[i];
      }
    }

    return null;
  }

  public static ColorBlock ButtonColors {
    get {
      ColorBlock colors = ColorBlock.defaultColorBlock;

      if (IsVanilla) {
        colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        colors.fadeDuration = 0.08f;
        return colors;
      }

      colors.normalColor = new Color(1f, 1f, 1f, 0.12f);
      colors.highlightedColor = new Color(0.4f, 0.7f, 1f, 0.4f);
      colors.pressedColor = new Color(0.3f, 0.55f, 0.9f, 0.6f);
      colors.selectedColor = new Color(0.4f, 0.7f, 1f, 0.5f);
      colors.disabledColor = new Color(1f, 1f, 1f, 0.05f);
      colors.fadeDuration = 0.1f;
      return colors;
    }
  }

  public static ColorBlock ScrollbarColors {
    get {
      ColorBlock colors = ColorBlock.defaultColorBlock;
      colors.normalColor = Color.white;
      colors.highlightedColor = new Color(1f, 0.75f, 0f);
      colors.selectedColor = new Color(1f, 0.75f, 0f);
      colors.pressedColor = new Color(1f, 0.67f, 0.11f);
      colors.disabledColor = Color.gray;
      colors.fadeDuration = 0.15f;
      return colors;
    }
  }

  public static void ApplyPanel(Image image) {
    if (IsVanilla && AssignSprite(image, "woodpanel_trophys")) {
      image.color = Color.white;

      Material material = GetMaterial("litpanel");

      if (material) {
        image.material = material;
      }

      return;
    }

    ApplyFlat(image, DarkPanelColor);
  }

  public static void ApplyList(Image image) {
    if (IsVanilla && AssignSprite(image, "item_background")) {
      image.color = new Color(0f, 0f, 0f, 0.5f);
      return;
    }

    ApplyFlat(image, DarkListColor);
  }

  public static void ApplyInput(Image image) {
    if (IsVanilla && AssignSprite(image, "text_field")) {
      image.color = Color.white;
      return;
    }

    ApplyFlat(image, DarkInputColor);
  }

  public static void ApplyButton(Button button) {
    Image image = button.GetComponent<Image>();

    if (!image) {
      return;
    }

    if (IsVanilla
        && AssignSprite(image, "button")
        && GetSprite("button_highlight")
        && GetSprite("button_pressed")) {
      image.color = Color.white;
      button.targetGraphic = image;
      button.transition = Selectable.Transition.SpriteSwap;
      button.spriteState = CreateButtonSpriteState();
      return;
    }

    ApplyFlat(image, Color.white);
    button.targetGraphic = image;
    button.transition = Selectable.Transition.ColorTint;
    button.colors = ButtonColors;
  }

  public static void ApplyFavorite(TextMeshProUGUI label, bool isFavorite) {
    if (HasStarGlyph(label)) {
      label.text = isFavorite ? "★" : "☆";
    } else {
      label.text = isFavorite ? "*" : "·";
    }

    label.color = isFavorite ? FavoriteOnColor : FavoriteOffColor;
  }

  // Key rows are marked with a key icon. TextMeshPro fonts rarely carry emoji, so step down to the
  // first glyph the font (or its fallbacks) can render.
  static readonly string[] KeyGlyphCandidates = [
      "🗝",
      "🔑",
      "⚿",
      "•",
  ];

  static string _keyGlyph;

  public static string GetKeyGlyph(TextMeshProUGUI label) {
    if (_keyGlyph != null) {
      return _keyGlyph;
    }

    TMP_FontAsset font = label.font;

    for (int i = 0; i < KeyGlyphCandidates.Length; i++) {
      string glyph = KeyGlyphCandidates[i];

      if (font && font.HasCharacters(
              glyph, out _, searchFallbacks: true, tryAddCharacter: true)) {
        _keyGlyph = glyph;
        break;
      }
    }

    if (_keyGlyph == null) {
      _keyGlyph = "•";
    }

    return _keyGlyph;
  }

  // The default font carries the star, but fall back to plain text if a font swap drops it.
  static bool HasStarGlyph(TextMeshProUGUI label) {
    if (_hasStarGlyph.HasValue) {
      return _hasStarGlyph.Value;
    }

    TMP_FontAsset font = label.font;
    _hasStarGlyph = font && font.HasCharacter('★', searchFallbacks: true, tryAddCharacter: true);
    return _hasStarGlyph.Value;
  }

  public static void ApplyScrollbarTrack(Image image) {
    if (IsVanilla && AssignSprite(image, "Background")) {
      image.color = Color.black;
      return;
    }

    ApplyFlat(image, new Color(1f, 1f, 1f, 0.08f));
  }

  public static void ApplyScrollbarHandle(Image image) {
    if (IsVanilla && AssignSprite(image, "UISprite")) {
      image.color = Color.white;
      return;
    }

    ApplyFlat(image, new Color(0.85f, 0.85f, 0.85f, 0.95f));
  }

  public static Sprite GetSprite(string name) {
    EnsureSprites();
    return _sprites.TryGetValue(name, out Sprite sprite) ? sprite : null;
  }

  public static Material GetMaterial(string name) {
    EnsureMaterials();
    return _materials.TryGetValue(name, out Material material) ? material : null;
  }

  static SpriteState CreateButtonSpriteState() {
    SpriteState state = default;
    state.highlightedSprite = GetSprite("button_highlight");
    state.pressedSprite = GetSprite("button_pressed");
    state.selectedSprite = state.highlightedSprite;
    state.disabledSprite = GetSprite("button_disabled");
    return state;
  }

  static void ApplyFlat(Image image, Color color) {
    image.sprite = UI.WhiteSprite;
    image.type = Image.Type.Simple;
    image.color = color;
  }

  static bool AssignSprite(Image image, string spriteName) {
    Sprite sprite = GetSprite(spriteName);

    if (!sprite) {
      return false;
    }

    image.sprite = sprite;
    image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
    return true;
  }

  static void EnsureSprites() {
    if (_sprites != null) {
      return;
    }

    _sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);

    Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();

    for (int i = 0; i < sprites.Length; i++) {
      Sprite sprite = sprites[i];

      if (!sprite) {
        continue;
      }

      string name = sprite.name;

      if (Array.IndexOf(SpriteNames, name) >= 0 && !_sprites.ContainsKey(name)) {
        _sprites.Add(name, sprite);
      }
    }
  }

  static void EnsureMaterials() {
    if (_materials != null) {
      return;
    }

    _materials = new Dictionary<string, Material>(StringComparer.Ordinal);

    Material[] materials = Resources.FindObjectsOfTypeAll<Material>();

    for (int i = 0; i < materials.Length; i++) {
      Material material = materials[i];

      if (!material) {
        continue;
      }

      string name = material.name;

      if (Array.IndexOf(MaterialNames, name) >= 0 && !_materials.ContainsKey(name)) {
        _materials.Add(name, material);
      }
    }
  }
}
