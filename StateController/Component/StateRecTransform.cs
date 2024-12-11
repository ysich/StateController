
using UnityEngine;

namespace Core.UI.StateController
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class StateRecTransform:SelectableStateBase<Rect>
    {

        protected override void OnStateInit()
        {
        }

        protected override void OnStateChanged(Rect rect)
        {
            RectTransform rectTransform = this.rectTransform();
            rectTransform.anchoredPosition = rect.position;
            rectTransform.sizeDelta = rect.size;
        }
    }
}