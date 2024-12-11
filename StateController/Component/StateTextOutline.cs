
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.StateController
{
    [RequireComponent(typeof(TextEx))]
    //[RequireComponent(typeof(UGUIOutlineEx))]
    [DisallowMultipleComponent]
    public class StateTextOutline:SelectableStateBase<Material>
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

        protected override void OnStateChanged(Material material)
        {
            text.material = material;
            text.SetMaterialDirty();
#if UNITY_EDITOR
            text.SetVerticesDirty();
#endif
        }
    }
}