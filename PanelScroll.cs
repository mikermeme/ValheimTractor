namespace Tractor;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PanelScroll : MonoBehaviour, IScrollHandler {
  public ScrollRect ScrollRect { get; set; }
  public float RowsPerNotch { get; set; } = 3f;

  public void OnScroll(PointerEventData eventData) {
    if (!ScrollRect || !ScrollRect.content || !ScrollRect.viewport) {
      return;
    }

    float direction = Mathf.Sign(eventData.scrollDelta.y);

    if (direction == 0f) {
      return;
    }

    float hiddenLength = ScrollRect.content.rect.height - ScrollRect.viewport.rect.height;

    if (hiddenLength <= 0f) {
      // Nothing to scroll. Let the wheel pass to whatever is behind the panel.
      return;
    }

    float step = RowsPerNotch * (TractorPanel.RowHeight + TractorPanel.RowSpacing) / hiddenLength;

    ScrollRect.verticalNormalizedPosition =
        Mathf.Clamp01(ScrollRect.verticalNormalizedPosition + direction * step);
  }
}
