
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.StateController
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class StateActive:SelectableStateBase<bool>
    {
        protected override void OnStateInit()
        {
        }

        protected override void OnStateChanged(bool isShow)
        {
            gameObject.SetActive(isShow);
        }
    }
}