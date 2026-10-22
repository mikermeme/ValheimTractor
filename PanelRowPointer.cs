namespace Tractor;

using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PanelRowPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
  public TractorPanelRow Row { get; set; }

  public void OnPointerEnter(PointerEventData eventData) {
    if (Row != null) {
      Row.SetHovered(true);
    }
  }

  public void OnPointerExit(PointerEventData eventData) {
    if (Row != null) {
      Row.SetHovered(false);
    }
  }
}
