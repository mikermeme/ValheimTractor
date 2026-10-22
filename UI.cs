namespace Tractor;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public static class UI {
  static Sprite _whiteSprite;

  public static Sprite WhiteSprite {
    get {
      if (!_whiteSprite) {
        _whiteSprite = CreateWhiteSprite();
      }

      return _whiteSprite;
    }
  }

  public static GameObject CreatePanel(Transform parentTransform, string name, Vector2 size) {
    GameObject panel = CreateContainer(parentTransform, name);
    RectTransform rectTransform = panel.GetComponent<RectTransform>();
    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    rectTransform.pivot = new Vector2(0.5f, 0.5f);
    rectTransform.anchoredPosition = Vector2.zero;
    rectTransform.sizeDelta = size;

    Image image = panel.AddComponent<Image>();
    image.sprite = WhiteSprite;
    image.type = Image.Type.Simple;
    image.color = new Color(0.05f, 0.05f, 0.05f, 0.94f);
    image.raycastTarget = true;

    VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
    layout.childControlWidth = true;
    layout.childControlHeight = true;
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;
    layout.padding = new RectOffset(14, 14, 14, 14);
    layout.spacing = 6f;

    return panel;
  }

  public static GameObject CreateContainer(Transform parentTransform, string name) {
    GameObject container = new(name, typeof(RectTransform));
    container.transform.SetParent(parentTransform, worldPositionStays: false);
    return container;
  }

  public static GameObject CreateRow(Transform parentTransform, string name, float height) {
    GameObject row = CreateContainer(parentTransform, name);

    HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
    layout.childControlWidth = true;
    layout.childControlHeight = true;
    layout.childForceExpandWidth = false;
    layout.childForceExpandHeight = false;
    layout.childAlignment = TextAnchor.MiddleLeft;
    layout.padding = new RectOffset(2, 2, 2, 2);
    layout.spacing = 6f;

    LayoutElement layoutElement = row.AddComponent<LayoutElement>();
    layoutElement.minHeight = height;
    layoutElement.preferredHeight = height;

    return row;
  }

  // A horizontal group with no padding or spacing, so an inline icon sits right against its label.
  public static GameObject CreateInlineGroup(Transform parentTransform, string name) {
    GameObject group = CreateContainer(parentTransform, name);

    HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
    layout.childControlWidth = true;
    layout.childControlHeight = true;
    layout.childForceExpandWidth = false;
    layout.childForceExpandHeight = false;
    layout.childAlignment = TextAnchor.MiddleLeft;
    layout.padding = new RectOffset(0, 0, 0, 0);
    layout.spacing = 2f;

    return group;
  }

  public static GameObject CreateSpacer(Transform parentTransform, string name) {
    GameObject spacer = CreateContainer(parentTransform, name);

    LayoutElement layoutElement = spacer.AddComponent<LayoutElement>();
    layoutElement.minWidth = 0f;
    layoutElement.preferredWidth = 0f;
    layoutElement.flexibleWidth = 1f;

    return spacer;
  }

  public static GameObject CreateColumn(Transform parentTransform, string name) {
    GameObject column = CreateContainer(parentTransform, name);

    VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
    layout.childControlWidth = true;
    layout.childControlHeight = true;
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;
    layout.childAlignment = TextAnchor.UpperLeft;
    layout.padding = new RectOffset(0, 0, 0, 0);
    layout.spacing = 4f;

    return column;
  }

  public static Image AddBackground(GameObject gameObject, Color color) {
    Image image = gameObject.AddComponent<Image>();
    image.sprite = WhiteSprite;
    image.type = Image.Type.Simple;
    image.color = color;
    image.raycastTarget = true;
    return image;
  }

  public static TextMeshProUGUI CreateLabel(Transform parentTransform, string name) {
    TextMeshProUGUI label = Object.Instantiate(Hud.instance.m_hoverName, parentTransform, worldPositionStays: false);
    label.name = name;

    TMP_FontAsset font = Style.UiFontAsset;

    if (font && font.material && label.font != font) {
      label.font = font;
      label.fontSharedMaterial = font.material;
    }

    label.text = string.Empty;
    label.fontSize = Style.FontSize(16f);
    label.color = Color.white;
    label.alignment = TextAlignmentOptions.Left;
    label.enableAutoSizing = false;
    label.textWrappingMode = TextWrappingModes.NoWrap;
    label.overflowMode = TextOverflowModes.Ellipsis;
    label.richText = false;
    label.raycastTarget = false;
    label.gameObject.SetActive(true);

    RectTransform rectTransform = label.rectTransform;
    rectTransform.anchorMin = Vector2.zero;
    rectTransform.anchorMax = Vector2.one;
    rectTransform.pivot = new Vector2(0.5f, 0.5f);
    rectTransform.anchoredPosition = Vector2.zero;
    rectTransform.sizeDelta = Vector2.zero;

    return label;
  }

  public static TMP_InputField CreateInputField(
      Transform parentTransform, string placeholderText, float fontSize = 16f) {
    fontSize = Style.FontSize(fontSize);

    GameObject inputObject = CreateContainer(parentTransform, "InputField");

    Image background = AddBackground(inputObject, new Color(0f, 0f, 0f, 0.55f));
    background.raycastTarget = true;
    Style.ApplyInput(background);

    inputObject.AddComponent<RectMask2D>();

    TextMeshProUGUI text = CreateLabel(inputObject.transform, "Text");
    text.alignment = TextAlignmentOptions.Left;
    text.fontSize = fontSize;
    text.overflowMode = TextOverflowModes.Ellipsis;
    text.margin = new Vector4(8f, 0f, 8f, 0f);

    TextMeshProUGUI placeholder = CreateLabel(inputObject.transform, "Placeholder");
    placeholder.alignment = TextAlignmentOptions.Left;
    placeholder.fontSize = fontSize;
    placeholder.color = new Color(1f, 1f, 1f, 0.4f);
    placeholder.text = placeholderText;
    placeholder.margin = new Vector4(8f, 0f, 8f, 0f);

    TMP_InputField inputField = inputObject.AddComponent<TMP_InputField>();
    inputField.textViewport = inputObject.GetComponent<RectTransform>();
    inputField.textComponent = text;
    inputField.placeholder = placeholder;
    inputField.fontAsset = text.font;
    inputField.pointSize = fontSize;
    inputField.caretWidth = 2;
    inputField.caretColor = Color.white;
    inputField.selectionColor = new Color(0.3f, 0.6f, 1f, 0.5f);
    inputField.lineType = TMP_InputField.LineType.SingleLine;
    inputField.onFocusSelectAll = false;
    inputField.richText = false;
    inputField.targetGraphic = background;

    // TMP_InputField caches its parts on OnEnable, so re-enable it after assigning them.
    inputField.enabled = false;
    inputField.enabled = true;

    return inputField;
  }

  public static Button CreateButton(Transform parentTransform, string buttonText, float width) {
    GameObject buttonObject = CreateContainer(parentTransform, "Button");

    AddBackground(buttonObject, Color.white);

    LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
    layoutElement.minWidth = width;
    layoutElement.preferredWidth = width;
    layoutElement.minHeight = 28f;
    layoutElement.preferredHeight = 28f;

    TextMeshProUGUI label = CreateLabel(buttonObject.transform, "Label");
    label.text = buttonText;
    label.alignment = TextAlignmentOptions.Center;
    label.fontSize = Style.FontSize(15f);
    label.color = Color.white;

    Button button = buttonObject.AddComponent<Button>();
    Style.ApplyButton(button);

    return button;
  }

  public static LayoutElement SetHeight(GameObject gameObject, float height) {
    LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
    layoutElement.minHeight = height;
    layoutElement.preferredHeight = height;
    return layoutElement;
  }

  public static LayoutElement SetFlexibleWidth(GameObject gameObject, float width) {
    LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
    layoutElement.flexibleWidth = width;
    return layoutElement;
  }

  // Splits a row evenly with the other half-width children, whatever the text is.
  public static LayoutElement SetHalfWidth(GameObject gameObject) {
    LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
    layoutElement.preferredWidth = 0f;
    layoutElement.flexibleWidth = 1f;
    return layoutElement;
  }

  public static void Stretch(RectTransform rectTransform) {
    Stretch(rectTransform, Vector4.zero);
  }

  // Padding is (left, bottom, right, top), applied inward from each edge.
  public static void Stretch(RectTransform rectTransform, Vector4 padding) {
    rectTransform.anchorMin = Vector2.zero;
    rectTransform.anchorMax = Vector2.one;
    rectTransform.pivot = new Vector2(0.5f, 0.5f);
    rectTransform.offsetMin = new Vector2(padding.x, padding.y);
    rectTransform.offsetMax = new Vector2(-padding.z, -padding.w);
  }

  static Sprite CreateWhiteSprite() {
    Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
    texture.name = "Tractor.White";
    texture.SetPixel(0, 0, Color.white);
    texture.Apply();

    Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    sprite.name = "Tractor.White";
    return sprite;
  }
}
