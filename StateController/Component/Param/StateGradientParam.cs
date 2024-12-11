
using System;
using UnityEngine;

namespace Core.UI.StateController
{
    [Serializable]
    public class StateGradientParam
    {
        [SerializeField] public bool isEnable = true;
        [SerializeField] public Color topColor;
        [SerializeField] public Color bottomColor;

        public StateGradientParam(bool isEnable, Color topColor, Color bottomColor)
        {
            this.isEnable = isEnable;
            this.topColor = topColor;
            this.bottomColor = bottomColor;
        }
    }
}