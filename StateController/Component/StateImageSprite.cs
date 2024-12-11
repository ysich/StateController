
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.StateController
{
    [RequireComponent(typeof(ImageEx))]
    [DisallowMultipleComponent]
    public class StateImageSprite:SelectableStateBase<Sprite>
    {
        [SerializeField]
        private ImageEx m_Image;
        public ImageEx image
        {
            get
            {
                if (m_Image == null)
                    m_Image = GetComponent<ImageEx>();
                return m_Image;
            }
        }
        protected override void OnStateInit()
        {
            if(m_Image == null)
                m_Image = GetComponent<ImageEx>();
        }

        protected override void OnStateChanged(Sprite sprite)
        {
            image.sprite = sprite;
        }
        
    }
}