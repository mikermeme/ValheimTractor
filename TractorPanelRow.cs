namespace Tractor;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public sealed class TractorPanelRow {
  static readonly Color NormalColor = new(1f, 1f, 1f, 0f);
  static readonly Color HoveredColor = new(1f, 1f, 1f, 0.08f);
  static readonly Color SelectedColor = new(1f, 1f, 1f, 0.2f);
  static readonly Color KeyNameColor = new(1f, 1f, 1f, 1f);
  static readonly Color FieldNameColor = new(0.85f, 0.85f, 0.85f, 1f);
  static readonly Color OverrideNameColor = new(1f, 0.9f, 0.5f, 1f);
  static readonly Color ValueColor = new(0.9f, 0.9f, 0.7f, 1f);
  static readonly Color ReadOnlyColor = new(0.65f, 0.65f, 0.65f, 1f);
  static readonly Color TypeColor = new(0.68f, 0.7f, 0.74f, 1f);
  static readonly Color KeyIconColor = new(1f, 0.9f, 0.5f, 1f);

  public GameObject Row { get; }
  public Image Background { get; }
  public Button Button { get; }
  public Button FavoriteButton { get; }
  public TextMeshProUGUI FavoriteLabel { get; }
  public TextMeshProUGUI IconLabel { get; }
  public TextMeshProUGUI NameLabel { get; }
  public TextMeshProUGUI ValueLabel { get; }
  public TextMeshProUGUI TypeLabel { get; }

  public ZdoKeyRow KeyRow { get; private set; }
  public ZdoFieldRow FieldRow { get; private set; }

  bool _isSelected;
  bool _isHovered;

  public TractorPanelRow(Transform parentTransform) {
    Row = UI.CreateRow(parentTransform, "Row", TractorPanel.RowHeight);

    // Transparent, but still a raycast target so the whole row is clickable.
    Background = UI.AddBackground(Row, NormalColor);

    Button = Row.AddComponent<Button>();
    Button.transition = Selectable.Transition.None;
    Button.onClick.AddListener(OnClick);

    PanelRowPointer pointer = Row.AddComponent<PanelRowPointer>();
    pointer.Row = this;

    GameObject favorite = UI.CreateContainer(Row.transform, "Favorite");

    LayoutElement favoriteLayout = favorite.AddComponent<LayoutElement>();
    favoriteLayout.minWidth = 20f;
    favoriteLayout.preferredWidth = 20f;
    favoriteLayout.minHeight = 20f;
    favoriteLayout.preferredHeight = 20f;

    Image favoriteBackground = UI.AddBackground(favorite, Color.clear);
    FavoriteButton = favorite.AddComponent<Button>();
    FavoriteButton.targetGraphic = favoriteBackground;
    FavoriteButton.transition = Selectable.Transition.None;
    FavoriteButton.onClick.AddListener(OnFavoriteClick);

    FavoriteLabel = UI.CreateLabel(favorite.transform, "Star");
    FavoriteLabel.fontSize = Style.FontSize(18f);
    FavoriteLabel.alignment = TextAlignmentOptions.Center;
    FavoriteLabel.overflowMode = TextOverflowModes.Overflow;

    // The key icon sits inline at the start of the name, the way a component name prefixes a field.
    GameObject nameGroup = UI.CreateInlineGroup(Row.transform, "NameGroup");
    UI.SetFlexibleWidth(nameGroup, 1f);

    IconLabel = UI.CreateLabel(nameGroup.transform, "Icon");
    IconLabel.fontSize = Style.FontSize(14f);
    IconLabel.alignment = TextAlignmentOptions.Left;
    IconLabel.overflowMode = TextOverflowModes.Overflow;
    IconLabel.color = KeyIconColor;

    NameLabel = UI.CreateLabel(nameGroup.transform, "Name");
    NameLabel.fontSize = Style.FontSize(14f);
    NameLabel.color = KeyNameColor;
    UI.SetFlexibleWidth(NameLabel.gameObject, 1f);

    ValueLabel = UI.CreateLabel(Row.transform, "Value");
    ValueLabel.fontSize = Style.FontSize(14f);
    ValueLabel.alignment = TextAlignmentOptions.Right;
    ValueLabel.color = ValueColor;

    LayoutElement valueLayout = ValueLabel.gameObject.AddComponent<LayoutElement>();
    valueLayout.minWidth = 150f;
    valueLayout.preferredWidth = 150f;

    TypeLabel = UI.CreateLabel(Row.transform, "Type");
    TypeLabel.fontSize = Style.FontSize(13f);
    TypeLabel.color = TypeColor;

    LayoutElement typeLayout = TypeLabel.gameObject.AddComponent<LayoutElement>();
    typeLayout.minWidth = 68f;
    typeLayout.preferredWidth = 68f;

    UpdateBackground();
  }

  public void BindKey(ZdoKeyRow row) {
    KeyRow = row;
    FieldRow = null;

    Row.SetActive(true);
    Style.ApplyFavorite(FavoriteLabel, Favorites.IsKey(row.Name));
    RefreshValue();
  }

  public void BindField(ZdoFieldRow row) {
    KeyRow = null;
    FieldRow = row;

    Row.SetActive(true);
    Style.ApplyFavorite(FavoriteLabel, Favorites.IsField(row.Name));
    RefreshValue();
  }

  public void Hide() {
    KeyRow = null;
    FieldRow = null;
    _isHovered = false;
    Row.SetActive(false);
  }

  public void RefreshValue() {
    if (KeyRow != null) {
      IconLabel.text = Style.GetKeyGlyph(IconLabel);
      NameLabel.text = KeyRow.Name;
      NameLabel.fontStyle = FontStyles.Normal;
      NameLabel.color = KeyRow.CanEdit ? KeyNameColor : ReadOnlyColor;
      ValueLabel.text = KeyRow.IsMissing ? "(not set)" : KeyRow.ValueText;
      ValueLabel.color = KeyRow.CanEdit ? ValueColor : ReadOnlyColor;
      TypeLabel.text = KeyRow.TypeText;
      return;
    }

    if (FieldRow != null) {
      IconLabel.text = string.Empty;
      NameLabel.text = FieldRow.Name;
      NameLabel.fontStyle = FieldRow.HasOverride ? FontStyles.Bold : FontStyles.Normal;
      NameLabel.color = FieldRow.HasOverride ? OverrideNameColor : FieldNameColor;
      ValueLabel.text = FieldRow.ValueText;
      ValueLabel.color = ValueColor;
      TypeLabel.text = FieldRow.TypeText;
    }
  }

  public void SetSelected(bool isSelected) {
    _isSelected = isSelected;
    UpdateBackground();
  }

  public void SetHovered(bool isHovered) {
    _isHovered = isHovered;
    UpdateBackground();
  }

  void UpdateBackground() {
    Background.color = _isSelected ? SelectedColor : _isHovered ? HoveredColor : NormalColor;
  }

  void OnClick() {
    if (KeyRow != null) {
      TractorPanel.SelectKey(KeyRow);
      return;
    }

    if (FieldRow != null) {
      TractorPanel.SelectField(FieldRow);
    }
  }

  void OnFavoriteClick() {
    TractorPanel.ToggleFavorite(this);
  }
}
