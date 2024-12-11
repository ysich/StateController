
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.StateController
{
    [RequireComponent(typeof(TextEx))]
    [DisallowMultipleComponent]
    public class StateTextColor:SelectableStateBase<Color>
    {
        [SerializeField]
        private TextEx m_Text;
        
        public TextEx text
        {
            get
            {
                if (m_Text == null)
                    m_Text = GetComponent<TextEx>();
                return m_Text;
            }
        }
        protected override void OnStateInit()
        {
            if(m_Text == null)
                m_Text = GetComponent<TextEx>();
        }

        protected override void OnStateChanged(Color color)
        {
            text.color = color;
        }
    }
}