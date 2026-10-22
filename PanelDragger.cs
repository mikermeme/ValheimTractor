namespace Tractor;

using UnityEngine;
using UnityEngine.EventSystems;

using static PluginConfig;

public sealed class PanelDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
  Canvas _canvas;

  public RectTransform TargetRectTransform { get; set; }

  void Awake() {
    _canvas = GetComponentInParent<Canvas>();
  }

  public void OnBeginDrag(PointerEventData eventData) {
  }

  public void OnDrag(PointerEventData eventData) {
    if (!TargetRectTransform) {
      return;
    }

    float scaleFactor = _canvas ? _canvas.scaleFactor : 1f;

    if (scaleFactor <= 0f) {
      scaleFactor = 1f;
    }

    TargetRectTransform.anchoredPosition += eventData.delta / scaleFactor;
  }

  public void OnEndDrag(PointerEventData eventData) {
    if (TargetRectTransform) {
      PanelPosition.Value = TargetRectTransform.anchoredPosition;
    }
  }
}
