namespace Tractor;

using System;
using System.Collections.Generic;
using System.Globalization;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using static PluginConfig;

public static class TractorPanel {
  // Row height follows the font, so a taller face never gets its line clipped by the row box.
  public static float RowHeight {
    get { return Style.FontSize(24f); }
  }

  public const float RowSpacing = 2f;

  const float PanelWidth = 600f;
  const float PanelHeight = 720f;
  const float ScrollbarWidth = 16f;
  const float MinBulkRadius = 1f;
  const float MaxBulkRadius = 256f;

  static GameObject _panel;
  static GameObject _backdrop;
  static RectTransform _panelRectTransform;
  static ScrollRect _scrollRect;
  static RectTransform _rowContent;
  static TMP_InputField _filterInput;
  static TMP_InputField _valueInput;
  static TextMeshProUGUI _titleLabel;
  static TextMeshProUGUI _infoLabel;
  static TextMeshProUGUI _selectedLabel;
  static TextMeshProUGUI _statusLabel;
  static TMP_InputField _radiusInput;
  static Button _applyButton;
  static Button _resetButton;
  static Button _radiusPrefabButton;
  static Button _sectorPrefabButton;
  static Button _radiusComponentButton;
  static Button _sectorComponentButton;

  static readonly List<TractorPanelRow> _rows = [];
  static List<ZdoKeyRow> _keyRows = [];
  static List<ZdoFieldRow> _fieldRows = [];

  static ZdoTarget _target;
  static ZdoKeyRow _selectedKey;
  static ZdoFieldRow _selectedField;

  static string _filter = string.Empty;
  static int _closedFrame = -1;
  static bool _refocusValueInput;
  static float _lastShortcutTime = float.NegativeInfinity;

  const float ShortcutCooldownSeconds = 0.25f;

  static readonly Color ScopeTextColor = new(0.85f, 0.85f, 0.85f, 1f);

  public static bool IsOpen => _panel && _panel.activeSelf;
  public static bool WasClosedThisFrame => _closedFrame == Time.frameCount;

  public static void ToggleHovered() {
    // A single physical click can be reported on two frames, which used to open and then
    // immediately close the panel. Ignore shortcut repeats inside the cooldown window.
    if (Time.unscaledTime - _lastShortcutTime < ShortcutCooldownSeconds) {
      return;
    }

    _lastShortcutTime = Time.unscaledTime;

    if (IsOpen) {
      Close();
      return;
    }

    TryOpenHovered();
  }

  public static bool TryOpenHovered() {
    if (!IsModEnabled.Value || !Player.m_localPlayer || IsGameUiBlocking()) {
      return false;
    }

    Player player = Player.m_localPlayer;
    GameObject hovering = player.GetHoverObject();
    ZNetView netView = hovering ? hovering.GetComponentInParent<ZNetView>() : null;

    bool foundInFront = false;

    if (!netView || !netView.IsValid()) {
      float lookRadius = LookRadius.Value;

      if (lookRadius > 0f) {
        netView = LookTarget.FindInFront(player, lookRadius);
        foundInFront = netView;
      }
    }

    if (!netView || !netView.IsValid()) {
      return false;
    }

    if (!foundInFront) {
      return Open(netView);
    }

    ZDO zdo = netView.GetZDO();
    float distance = Vector3.Distance(player.transform.position, netView.transform.position);

    return Open(
        netView,
        "No hover target. Selected '"
            + ZdoCatalog.GetName(zdo.GetPrefab())
            + "' "
            + distance.ToString("0.0", CultureInfo.InvariantCulture)
            + "m ahead.");
  }

  public static bool Open(ZNetView netView, string status = null) {
    ZDO zdo = netView ? netView.GetZDO() : null;

    if (zdo == null || !EnsurePanel()) {
      return false;
    }

    _target = new ZdoTarget(zdo.m_uid);
    _filter = string.Empty;

    if (_filterInput) {
      _filterInput.SetTextWithoutNotify(string.Empty);
    }

    _panel.SetActive(true);

    if (_backdrop) {
      _backdrop.SetActive(true);
    }

    ApplyPanelPosition(SavedPanelPosition);
    Refresh();
    KeepCursorVisible();

    SetStatus(status ?? "Escape or a click outside closes.");
    return true;
  }

  public static void Close() {
    if (!_panel) {
      return;
    }

    if (_valueInput && _valueInput.isFocused && AutoApply.Value) {
      ApplySelected();
    }

    _refocusValueInput = false;
    _panel.SetActive(false);

    if (_backdrop) {
      _backdrop.SetActive(false);
    }

    _closedFrame = Time.frameCount;
  }

  public static void Update() {
    if (!IsOpen) {
      return;
    }

    if (ZInput.GetKeyDown(KeyCode.Escape) || !Player.m_localPlayer || IsGameUiBlocking()) {
      Close();
      return;
    }

    if (_refocusValueInput) {
      _refocusValueInput = false;

      if (_valueInput.interactable) {
        _valueInput.ActivateInputField();
      }
    }

    KeepCursorVisible();
  }

  public static void HandleModEnabledChanged() {
    if (!IsModEnabled.Value) {
      Close();
    }
  }

  public static void HandleBulkRadiusChanged() {
    SyncRadiusInput();
  }

  // The theme changes sprite sets and the font changes the label assets, so drop the panel and rebuild
  // it on the next open.
  public static void HandleThemeChanged() {
    RebuildPanel();
  }

  public static void HandleFontChanged() {
    Style.ResetUiFont();
    RebuildPanel();
  }

  static void RebuildPanel() {
    if (!_panel) {
      return;
    }

    Close();
    DestroyPanel();
  }

  public static void ApplyPanelPosition(Vector2 position) {
    if (_panelRectTransform) {
      _panelRectTransform.anchoredPosition = position;
    }
  }

  // 0,0 was the old default, so treat it as unset and use the current default instead.
  static Vector2 SavedPanelPosition {
    get { return PanelPosition.Value == Vector2.zero ? DefaultPanelPosition : PanelPosition.Value; }
  }

  public static void SelectKey(ZdoKeyRow row) {
    _selectedKey = row;
    _selectedField = null;

    UpdateSelectedLabel();
    _valueInput.SetTextWithoutNotify(row.ValueText);

    UpdateEditorState();
    RefreshSelection();

    if (row.CanEdit) {
      _valueInput.ActivateInputField();
      SelectValueText();
    }
  }

  public static void SelectField(ZdoFieldRow row) {
    _selectedKey = null;
    _selectedField = row;

    UpdateSelectedLabel();
    _valueInput.SetTextWithoutNotify(row.ValueText);

    UpdateEditorState();
    RefreshSelection();

    _valueInput.ActivateInputField();
    SelectValueText();
  }

  static void SelectValueText() {
    if (!_valueInput) {
      return;
    }

    _valueInput.MoveTextStart(false);
    _valueInput.MoveTextEnd(true);
  }

  static void UpdateSelectedLabel() {
    if (!_selectedLabel) {
      return;
    }

    if (_selectedKey != null) {
      _selectedLabel.text =
          "Key: " + _selectedKey.Name + (_selectedKey.IsMissing ? "  (not set)" : string.Empty);
      return;
    }

    if (_selectedField != null) {
      _selectedLabel.text =
          "Field: "
          + _selectedField.Name
          + (_selectedField.HasOverride ? "  (override)" : "  (prefab value)");
      return;
    }

    _selectedLabel.text = "Select a key or field.";
  }

  static bool EnsurePanel() {
    if (_panel && _filterInput && _backdrop) {
      return true;
    }

    // A build that failed part way used to leave _panel set with the rest null, so every later open
    // crashed on the missing widgets. Drop whatever survived and start over.
    DestroyPanel();

    if (!Hud.instance || !Hud.instance.m_rootObject) {
      return false;
    }

    try {
      BuildPanel(Hud.instance.m_rootObject.transform);
    } catch (Exception exception) {
      Tractor.LogError($"Could not build the panel: {exception}");
      DestroyPanel();
      return false;
    }

    return true;
  }

  static void BuildPanel(Transform parentTransform) {
    _rows.Clear();
    CreateBackdrop(parentTransform);

    _panel = UI.CreatePanel(parentTransform, "Tractor.Panel", new Vector2(PanelWidth, PanelHeight));
    _panelRectTransform = _panel.GetComponent<RectTransform>();
    Style.ApplyPanel(_panel.GetComponent<Image>());

    CreateTitleRow();
    CreateToolbarRow();
    CreateInfoLabel();
    CreateList();
    CreateEditor();

    _panel.SetActive(false);
    _backdrop.SetActive(false);
    ApplyPanelPosition(SavedPanelPosition);
  }

  static void CreateBackdrop(Transform parentTransform) {
    _backdrop = UI.CreateContainer(parentTransform, "Tractor.Backdrop");
    UI.Stretch(_backdrop.GetComponent<RectTransform>());

    Image image = UI.AddBackground(_backdrop, new Color(0f, 0f, 0f, 0.01f));
    image.raycastTarget = true;

    Button button = _backdrop.AddComponent<Button>();
    button.transition = Selectable.Transition.None;
    button.onClick.AddListener(Close);
  }

  static void DestroyPanel() {
    if (_backdrop) {
      UnityEngine.Object.Destroy(_backdrop);
    }

    if (_panel) {
      UnityEngine.Object.Destroy(_panel);
    }

    _panel = null;
    _backdrop = null;
    _panelRectTransform = null;
    _scrollRect = null;
    _rowContent = null;
    _filterInput = null;
    _valueInput = null;
    _titleLabel = null;
    _infoLabel = null;
    _selectedLabel = null;
    _statusLabel = null;
    _radiusInput = null;
    _applyButton = null;
    _resetButton = null;
    _radiusPrefabButton = null;
    _sectorPrefabButton = null;
    _radiusComponentButton = null;
    _sectorComponentButton = null;

    _rows.Clear();
    _keyRows = [];
    _fieldRows = [];
    _selectedKey = null;
    _selectedField = null;
    _refocusValueInput = false;
  }

  static void CreateTitleRow() {
    GameObject row = UI.CreateRow(_panel.transform, "Title.Row", Style.FontSize(32f));
    UI.AddBackground(row, new Color(1f, 1f, 1f, 0.06f));

    _titleLabel = UI.CreateLabel(row.transform, "Title");
    _titleLabel.text = "No target";
    _titleLabel.fontSize = Style.FontSize(20f);
    _titleLabel.fontStyle = FontStyles.Bold;
    UI.SetFlexibleWidth(_titleLabel.gameObject, 1f);

    Button closeButton = UI.CreateButton(row.transform, "Close", 60f);
    closeButton.onClick.AddListener(Close);

    PanelDragger dragger = row.AddComponent<PanelDragger>();
    dragger.TargetRectTransform = _panelRectTransform;
  }

  static void CreateToolbarRow() {
    GameObject row = UI.CreateRow(_panel.transform, "Toolbar.Row", Style.FontSize(30f));

    _filterInput = UI.CreateInputField(row.transform, "Filter keys and fields...");

    LayoutElement filterLayout = _filterInput.gameObject.AddComponent<LayoutElement>();
    filterLayout.flexibleWidth = 1f;
    filterLayout.minHeight = 28f;
    filterLayout.preferredHeight = 28f;

    _filterInput.onValueChanged.AddListener(OnFilterChanged);

    Button reloadButton = UI.CreateButton(row.transform, "Reload", 70f);
    reloadButton.onClick.AddListener(Reload);
  }

  static void CreateInfoLabel() {
    _infoLabel = UI.CreateLabel(_panel.transform, "Info");
    _infoLabel.fontSize = Style.FontSize(13f);
    _infoLabel.color = new Color(0.7f, 0.7f, 0.7f, 1f);
    UI.SetHeight(_infoLabel.gameObject, Style.FontSize(20f));
  }

  static void CreateList() {
    GameObject list = UI.CreateContainer(_panel.transform, "List");
    list.AddComponent<LayoutElement>().flexibleHeight = 1f;

    Image listImage = UI.AddBackground(list, Color.clear);
    listImage.raycastTarget = false;
    Style.ApplyList(listImage);

    _scrollRect = list.AddComponent<ScrollRect>();
    _scrollRect.horizontal = false;
    _scrollRect.vertical = true;
    _scrollRect.movementType = ScrollRect.MovementType.Clamped;
    _scrollRect.inertia = false;

    GameObject viewport = UI.CreateContainer(list.transform, "Viewport");
    RectTransform viewportRect = viewport.GetComponent<RectTransform>();
    viewportRect.anchorMin = Vector2.zero;
    viewportRect.anchorMax = Vector2.one;
    viewportRect.pivot = new Vector2(0.5f, 0.5f);
    viewportRect.offsetMin = Vector2.zero;
    viewportRect.offsetMax = new Vector2(-ScrollbarWidth, 0f);

    viewport.AddComponent<RectMask2D>();

    Image viewportImage = UI.AddBackground(viewport, new Color(0f, 0f, 0f, 0.01f));
    viewportImage.raycastTarget = true;

    PanelScroll panelScroll = viewport.AddComponent<PanelScroll>();
    panelScroll.ScrollRect = _scrollRect;

    GameObject content = UI.CreateContainer(viewport.transform, "Content");
    _rowContent = content.GetComponent<RectTransform>();
    _rowContent.anchorMin = new Vector2(0f, 1f);
    _rowContent.anchorMax = new Vector2(1f, 1f);
    _rowContent.pivot = new Vector2(0.5f, 1f);
    _rowContent.anchoredPosition = Vector2.zero;
    _rowContent.sizeDelta = Vector2.zero;

    VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
    contentLayout.childControlWidth = true;
    contentLayout.childControlHeight = true;
    contentLayout.childForceExpandWidth = true;
    contentLayout.childForceExpandHeight = false;
    contentLayout.padding = new RectOffset(0, 0, 0, 0);
    contentLayout.spacing = RowSpacing;

    ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    CreateScrollbar(list.transform);

    _scrollRect.viewport = viewportRect;
    _scrollRect.content = _rowContent;
  }

  static void CreateScrollbar(Transform parentTransform) {
    GameObject track = UI.CreateContainer(parentTransform, "Scrollbar");
    RectTransform trackRect = track.GetComponent<RectTransform>();
    trackRect.anchorMin = new Vector2(1f, 0f);
    trackRect.anchorMax = new Vector2(1f, 1f);
    trackRect.pivot = new Vector2(1f, 0.5f);
    trackRect.sizeDelta = new Vector2(ScrollbarWidth, 0f);
    trackRect.anchoredPosition = Vector2.zero;

    Image trackImage = UI.AddBackground(track, Color.white);
    Style.ApplyScrollbarTrack(trackImage);

    Scrollbar scrollbar = track.AddComponent<Scrollbar>();
    scrollbar.direction = Scrollbar.Direction.BottomToTop;
    scrollbar.transition = Selectable.Transition.ColorTint;
    scrollbar.colors = Style.ScrollbarColors;

    GameObject slidingArea = UI.CreateContainer(track.transform, "Sliding Area");
    UI.Stretch(slidingArea.GetComponent<RectTransform>(), new Vector4(2f, 2f, 2f, 2f));

    GameObject handle = UI.CreateContainer(slidingArea.transform, "Handle");
    RectTransform handleRect = handle.GetComponent<RectTransform>();
    handleRect.anchorMin = Vector2.zero;
    handleRect.anchorMax = Vector2.one;
    handleRect.pivot = new Vector2(0.5f, 0.5f);
    handleRect.sizeDelta = Vector2.zero;

    Image handleImage = UI.AddBackground(handle, Color.white);
    Style.ApplyScrollbarHandle(handleImage);

    scrollbar.handleRect = handleRect;
    scrollbar.targetGraphic = handleImage;

    _scrollRect.verticalScrollbar = scrollbar;
    _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
  }

  static void CreateEditor() {
    CreateHeaderRow(_panel.transform);
    CreateValueRow(_panel.transform);
    CreateScopeRows(_panel.transform);
  }

  // Selected key or field on the left, the last action on the right.
  static void CreateHeaderRow(Transform parentTransform) {
    GameObject row = UI.CreateRow(parentTransform, "Editor.Header", Style.FontSize(24f));

    _selectedLabel = UI.CreateLabel(row.transform, "Selected");
    _selectedLabel.fontSize = Style.FontSize(13f);
    _selectedLabel.color = new Color(0.9f, 0.9f, 0.9f, 1f);
    UI.SetHalfWidth(_selectedLabel.gameObject);

    _statusLabel = UI.CreateLabel(row.transform, "Status");
    _statusLabel.fontSize = Style.FontSize(13f);
    _statusLabel.alignment = TextAlignmentOptions.Right;
    UI.SetHalfWidth(_statusLabel.gameObject);
  }

  static void CreateValueRow(Transform parentTransform) {
    GameObject row = UI.CreateRow(parentTransform, "Editor.Value", Style.FontSize(30f));

    _valueInput = UI.CreateInputField(row.transform, "Select a key or field...");
    _valueInput.onFocusSelectAll = true;
    _valueInput.onEndEdit.AddListener(OnValueEndEdit);
    _valueInput.onSubmit.AddListener(OnValueSubmit);

    LayoutElement valueLayout = _valueInput.gameObject.AddComponent<LayoutElement>();
    valueLayout.flexibleWidth = 1f;
    valueLayout.minHeight = Style.FontSize(28f);
    valueLayout.preferredHeight = Style.FontSize(28f);

    _applyButton = UI.CreateButton(row.transform, "Apply", 64f);
    _applyButton.onClick.AddListener(ApplySelected);

    _resetButton = UI.CreateButton(row.transform, "Reset", 64f);
    _resetButton.onClick.AddListener(ResetSelected);
  }

  static void CreateScopeRows(Transform parentTransform) {
    GameObject row = UI.CreateRow(parentTransform, "Editor.Scope", Style.FontSize(28f));

    // Tighter spacing inside each group, so the spare width stays in the two group gaps.
    row.GetComponent<HorizontalLayoutGroup>().spacing = 4f;

    TextMeshProUGUI title = UI.CreateLabel(row.transform, "RadiusTitle");
    title.text = "Radius:";
    title.fontSize = Style.FontSize(12f);
    title.color = ScopeTextColor;

    // Measure the label with the panel font, so the text never ellipsizes at "Radi...".
    title.gameObject.AddComponent<LayoutElement>().preferredWidth =
        Mathf.Max(title.GetPreferredValues(title.text).x + 6f, Style.FontSize(46f));

    _radiusInput = UI.CreateInputField(row.transform, "32", 12f);
    _radiusInput.onEndEdit.AddListener(OnRadiusInputEndEdit);
    _radiusInput.onSubmit.AddListener(OnRadiusInputEndEdit);

    LayoutElement radiusLayout = _radiusInput.gameObject.AddComponent<LayoutElement>();
    radiusLayout.minWidth = 36f;
    radiusLayout.preferredWidth = 36f;
    radiusLayout.minHeight = Style.FontSize(20f);
    radiusLayout.preferredHeight = Style.FontSize(20f);

    UI.CreateSpacer(row.transform, "PrefabSpacer");

    CreateScopeTitle(row.transform, "All prefabs in:");

    _radiusPrefabButton = CreateScopeButton(row.transform, "Radius");
    _radiusPrefabButton.onClick.AddListener(BulkToRadius);

    _sectorPrefabButton = CreateScopeButton(row.transform, "Sector");
    _sectorPrefabButton.onClick.AddListener(BulkToSector);

    UI.CreateSpacer(row.transform, "ComponentSpacer");

    CreateScopeTitle(row.transform, "All components in:");

    _radiusComponentButton = CreateScopeButton(row.transform, "Radius");
    _radiusComponentButton.onClick.AddListener(BulkToRadiusComponent);

    _sectorComponentButton = CreateScopeButton(row.transform, "Sector");
    _sectorComponentButton.onClick.AddListener(BulkToSectorComponent);

    SyncRadiusInput();
  }

  // The scope labels size to their text, so the spare width becomes visible space between the groups.
  static void CreateScopeTitle(Transform parentTransform, string text) {
    TextMeshProUGUI title = UI.CreateLabel(parentTransform, text);
    title.text = text;
    title.fontSize = Style.FontSize(12f);
    title.color = ScopeTextColor;
  }

  static Button CreateScopeButton(Transform parentTransform, string text) {
    Button button = UI.CreateButton(parentTransform, text, 54f);
    button.GetComponentInChildren<TextMeshProUGUI>().fontSize = Style.FontSize(12f);
    return button;
  }

  static void OnRadiusInputEndEdit(string value) {
    if (float.TryParse(
            value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float radius)) {
      BulkRadius.Value = Mathf.Clamp(radius, MinBulkRadius, MaxBulkRadius);
    }

    SyncRadiusInput();
  }

  static void SyncRadiusInput() {
    if (_radiusInput) {
      _radiusInput.SetTextWithoutNotify(
          BulkRadius.Value.ToString("0", CultureInfo.InvariantCulture));
    }
  }

  static void OnFilterChanged(string value) {
    _filter = value.Trim();
    RebuildRows();
  }

  static void OnValueEndEdit(string value) {
    if (!IsOpen || !AutoApply.Value) {
      return;
    }

    ApplySelected();
  }

  static void OnValueSubmit(string value) {
    ApplySelected();
    _refocusValueInput = true;
  }

  static void Reload() {
    Refresh();

    if (_target != null && _target.Zdo != null) {
      SetStatus("Reloaded.");
    }
  }

  static void Refresh(bool resetScroll = true) {
    ClearSelection();

    ZDO zdo = _target == null ? null : _target.Zdo;

    if (zdo == null) {
      _keyRows = [];
      _fieldRows = [];
      UpdateInfo(null);
      RebuildRows();
      SetStatus("Target ZDO is gone.", isError: true);
      return;
    }

    ZNetView netView = _target.NetView;
    _fieldRows = netView && netView.IsValid() ? ZdoEditor.ReadFields(zdo, netView) : [];

    // A stored field override is also a raw ZDO entry. Hide the key row so the field row owns it.
    HashSet<int> fieldHashes = new();

    for (int i = 0; i < _fieldRows.Count; i++) {
      fieldHashes.Add(_fieldRows[i].Hash);
    }

    _keyRows = ZdoEditor.ReadKeys(zdo, netView, fieldHashes);

    UpdateInfo(zdo);
    RebuildRows();

    if (resetScroll) {
      ResetScrollPosition();
    }
  }

  static void RebuildRows() {
    SortRows();

    int keyIndex = 0;
    int fieldIndex = 0;
    int rowIndex = 0;

    while (true) {
      while (keyIndex < _keyRows.Count && !MatchesFilter(_keyRows[keyIndex].Name)) {
        keyIndex++;
      }

      while (fieldIndex < _fieldRows.Count && !MatchesFilter(_fieldRows[fieldIndex].Name)) {
        fieldIndex++;
      }

      bool hasKey = keyIndex < _keyRows.Count;
      bool hasField = fieldIndex < _fieldRows.Count;

      if (!hasKey && !hasField) {
        break;
      }

      if (hasKey && (!hasField || CompareDisplay(_keyRows[keyIndex], _fieldRows[fieldIndex]) <= 0)) {
        ZdoKeyRow row = _keyRows[keyIndex++];
        TractorPanelRow widget = GetRow(rowIndex++);
        widget.BindKey(row);
        widget.SetSelected(row == _selectedKey);
      } else {
        ZdoFieldRow row = _fieldRows[fieldIndex++];
        TractorPanelRow widget = GetRow(rowIndex++);
        widget.BindField(row);
        widget.SetSelected(row == _selectedField);
      }
    }

    for (int i = rowIndex; i < _rows.Count; i++) {
      _rows[i].Hide();
    }
  }

  // Favorites first (keys before fields), then stored overrides, then keys, then the hashes the catalog
  // cannot name, then plain fields.
  static void SortRows() {
    _keyRows.Sort(CompareKeyRows);
    _fieldRows.Sort(CompareFieldRows);
  }

  static int GetRank(ZdoKeyRow row) {
    if (Favorites.IsKey(row.Name)) {
      return 0;
    }

    return row.IsUnresolved ? 4 : 3;
  }

  static int GetRank(ZdoFieldRow row) {
    if (Favorites.IsField(row.Name)) {
      return 1;
    }

    return row.HasOverride ? 2 : 5;
  }

  static int CompareDisplay(ZdoKeyRow keyRow, ZdoFieldRow fieldRow) {
    int keyRank = GetRank(keyRow);
    int fieldRank = GetRank(fieldRow);

    if (keyRank != fieldRank) {
      return keyRank.CompareTo(fieldRank);
    }

    return string.Compare(keyRow.Name, fieldRow.Name, StringComparison.OrdinalIgnoreCase);
  }

  static int CompareKeyRows(ZdoKeyRow a, ZdoKeyRow b) {
    int rankCompare = GetRank(a).CompareTo(GetRank(b));

    return rankCompare != 0
        ? rankCompare
        : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
  }

  static int CompareFieldRows(ZdoFieldRow a, ZdoFieldRow b) {
    int rankCompare = GetRank(a).CompareTo(GetRank(b));

    return rankCompare != 0
        ? rankCompare
        : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
  }

  public static void ToggleFavorite(TractorPanelRow row) {
    if (!IsOpen) {
      return;
    }

    string name = null;
    bool isFavorite = false;

    if (row.KeyRow != null) {
      name = row.KeyRow.Name;
      isFavorite = Favorites.ToggleKey(name);
    } else if (row.FieldRow != null) {
      name = row.FieldRow.Name;
      isFavorite = Favorites.ToggleField(name);
    } else {
      return;
    }

    RebuildRows();
    SetStatus((isFavorite ? "Pinned '" : "Unpinned '") + name + "'.");
  }

  static TractorPanelRow GetRow(int index) {
    while (_rows.Count <= index) {
      _rows.Add(new TractorPanelRow(_rowContent));
    }

    return _rows[index];
  }

  static bool MatchesFilter(string name) {
    return _filter.Length == 0 || name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
  }

  static void ClearSelection() {
    _selectedKey = null;
    _selectedField = null;
    _refocusValueInput = false;

    UpdateSelectedLabel();

    if (_valueInput) {
      _valueInput.SetTextWithoutNotify(string.Empty);
      _valueInput.interactable = false;
    }

    UpdateEditorState();

    if (_rows.Count > 0) {
      RefreshSelection();
    }
  }

  static void RefreshSelection() {
    for (int i = 0; i < _rows.Count; i++) {
      TractorPanelRow row = _rows[i];
      row.SetSelected(
          (_selectedKey != null && row.KeyRow == _selectedKey)
          || (_selectedField != null && row.FieldRow == _selectedField));
    }
  }

  static void RefreshRowValues() {
    for (int i = 0; i < _rows.Count; i++) {
      if (_rows[i].Row.activeSelf) {
        _rows[i].RefreshValue();
      }
    }
  }

  static void UpdateEditorState() {
    bool canEdit = _selectedKey != null ? _selectedKey.CanEdit : _selectedField != null;
    bool canRemove = _selectedKey != null ? _selectedKey.CanRemove : _selectedField != null;
    bool isField = _selectedField != null;

    if (_applyButton) {
      _applyButton.interactable = canEdit;
    }

    if (_resetButton) {
      _resetButton.interactable = canRemove;
    }

    if (_radiusPrefabButton) {
      _radiusPrefabButton.interactable = canEdit;
    }

    if (_sectorPrefabButton) {
      _sectorPrefabButton.interactable = canEdit;
    }

    // Keys have no component to match, so the component scopes only apply to a selected field.
    if (_radiusComponentButton) {
      _radiusComponentButton.interactable = canEdit && isField;
    }

    if (_sectorComponentButton) {
      _sectorComponentButton.interactable = canEdit && isField;
    }

    if (_valueInput) {
      _valueInput.interactable = canEdit;
    }
  }

  static void ApplySelected() {
    if (_selectedKey != null) {
      ApplyKey();
      return;
    }

    if (_selectedField != null) {
      ApplyField();
      return;
    }

    SetStatus("Select a key or field first.", isError: true);
  }

  static void ApplyKey() {
    ZdoKeyRow row = _selectedKey;
    string text = _valueInput.text;
    bool wasMissing = row.IsMissing;
    bool wasHashed = IsHashedKeyText(row, text);

    if (!wasMissing && text == row.ValueText) {
      return;
    }

    if (!ZdoEditor.TryApplyKey(_target, row, text, out string error)) {
      SetStatus(error, isError: true);
      return;
    }

    _valueInput.SetTextWithoutNotify(row.ValueText);
    RefreshRowValues();
    UpdateEditorState();
    UpdateSelectedLabel();
    SetStatus(
        wasMissing
            ? $"Added '{row.Name}'."
            : wasHashed
                ? $"Applied '{row.Name}' as stable hash {row.ValueText}."
                : $"Applied '{row.Name}'.");
  }

  // 'ShoulderItem' and friends store the stable hash of a prefab name as an int.
  static bool IsHashedKeyText(ZdoKeyRow row, string text) {
    return row.Type == ZdoKeyType.Int
        && !row.IsBool
        && !int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
  }

  static void ApplyField() {
    ZdoFieldRow row = _selectedField;
    string text = _valueInput.text;

    if (text == row.ValueText) {
      return;
    }

    if (!ZdoEditor.TryApplyField(_target, row, text, out string error)) {
      SetStatus(error, isError: true);
      return;
    }

    _valueInput.SetTextWithoutNotify(row.ValueText);
    RefreshRowValues();
    UpdateSelectedLabel();
    SetStatus($"Applied '{row.Name}'.");
  }

  static void ResetSelected() {
    if (_selectedKey != null) {
      ResetKey();
      return;
    }

    if (_selectedField != null) {
      ResetField();
      return;
    }

    SetStatus("Select a key or field first.", isError: true);
  }

  static void ResetKey() {
    ZdoKeyRow row = _selectedKey;

    if (!row.CanRemove) {
      SetStatus(
          row.IsMissing ? "'" + row.Name + "' is not set." : "Connections are read-only.",
          isError: true);
      return;
    }

    if (!ZdoEditor.TryRemoveKey(_target, row, out string error)) {
      SetStatus(error, isError: true);
      return;
    }

    Refresh(resetScroll: false);
    SetStatus($"Removed '{row.Name}'.");
  }

  static void ResetField() {
    ZdoFieldRow row = _selectedField;

    if (!ZdoEditor.TryResetField(_target, row, out string error)) {
      SetStatus(error, isError: true);
      return;
    }

    _valueInput.SetTextWithoutNotify(row.ValueText);
    RefreshRowValues();
    UpdateSelectedLabel();
    SetStatus($"Reset '{row.Name}' to the prefab value.");
  }

  static void BulkToRadius() {
    BulkApply(zoneOnly: false, matchComponent: false);
  }

  static void BulkToSector() {
    BulkApply(zoneOnly: true, matchComponent: false);
  }

  static void BulkToRadiusComponent() {
    BulkApply(zoneOnly: false, matchComponent: true);
  }

  static void BulkToSectorComponent() {
    BulkApply(zoneOnly: true, matchComponent: true);
  }

  static void BulkApply(bool zoneOnly, bool matchComponent) {
    string text = _valueInput.text;
    string scope =
        zoneOnly
            ? "this sector"
            : BulkRadius.Value.ToString("0", CultureInfo.InvariantCulture) + "m";

    if (_selectedKey != null) {
      int applied =
          ZdoEditor.BulkApplyKey(
              _target, _selectedKey, text, zoneOnly, out int candidates, out string error);

      if (error == null) {
        _valueInput.SetTextWithoutNotify(_selectedKey.ValueText);
        RefreshRowValues();
        UpdateEditorState();
      }

      ReportBulk(applied, candidates, error, scope, "same-prefab");
      return;
    }

    if (_selectedField != null) {
      int applied =
          ZdoEditor.BulkApplyField(
              _target,
              _selectedField,
              text,
              zoneOnly,
              matchComponent,
              out int candidates,
              out string error);

      if (error == null) {
        _valueInput.SetTextWithoutNotify(_selectedField.ValueText);
        RefreshRowValues();
      }

      ReportBulk(
          applied, candidates, error, scope, matchComponent ? "same-component" : "same-prefab");
      return;
    }

    SetStatus("Select a key or field first.", isError: true);
  }

  static void ReportBulk(int applied, int candidates, string error, string scope, string match) {
    if (error != null) {
      SetStatus(error, isError: true);
      return;
    }

    if (candidates == 0) {
      SetStatus($"No {match} objects in {scope}.", isError: true);
      return;
    }

    SetStatus($"Applied to {applied}/{candidates} {match} objects in {scope}.");
  }

  static void UpdateInfo(ZDO zdo) {
    if (!_titleLabel || !_infoLabel) {
      return;
    }

    if (zdo == null) {
      _titleLabel.text = "No target";
      _infoLabel.text = "0 keys  |  0 fields";
      return;
    }

    int missing = 0;

    for (int i = 0; i < _keyRows.Count; i++) {
      if (_keyRows[i].IsMissing) {
        missing++;
      }
    }

    int overrides = 0;

    for (int i = 0; i < _fieldRows.Count; i++) {
      if (_fieldRows[i].HasOverride) {
        overrides++;
      }
    }

    _titleLabel.text = ZdoCatalog.GetName(zdo.GetPrefab());

    _infoLabel.text =
        $"{_keyRows.Count} keys"
        + (missing > 0 ? $" ({missing} not set)" : string.Empty)
        + $"  |  {_fieldRows.Count} fields  |  {overrides} overrides  |  {zdo.m_uid}";
  }

  static void ResetScrollPosition() {
    if (!_scrollRect || !_rowContent) {
      return;
    }

    LayoutRebuilder.ForceRebuildLayoutImmediate(_rowContent);
    _scrollRect.verticalNormalizedPosition = 1f;
  }

  static void SetStatus(string message, bool isError = false) {
    if (!_statusLabel) {
      return;
    }

    _statusLabel.text = message;
    _statusLabel.color = isError ? new Color(1f, 0.45f, 0.45f, 1f) : new Color(0.75f, 0.9f, 0.75f, 1f);
  }

  static bool IsGameUiBlocking() {
    return Menu.IsActive()
        || InventoryGui.IsVisible()
        || global::Console.IsVisible()
        || TextInput.IsVisible()
        || Minimap.IsOpen()
        || (Chat.instance && Chat.instance.HasFocus());
  }

  public static void KeepCursorVisible() {
    ZCursor.LockState = CursorLockMode.None;
    ZCursor.Show();
  }
}
