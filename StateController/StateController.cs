
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.UI.StateController
{
    [DisallowMultipleComponent]
    public class StateController : MonoBehaviour
    {
        [SerializeField] private int m_GrayStateIndex = -1;
        
        [SerializeField] private List<string> states = new List<string>();

        [SerializeField] private int m_SelectedStateIndex;

        [SerializeField]
        private int m_GrayOriginalStateIndex;
        
        private bool m_IsGray;

        // [NoToLua]
        public int selectedStateIndex
        {
            get { return m_SelectedStateIndex; }
            set
            {
                if (value > states.Count - 1 || value < 0)
                {
                    return;
                }

                m_SelectedStateIndex = value;
                foreach (var stateBase in m_StateBases)
                {
                    stateBase.OnRefresh(m_SelectedStateIndex);
                }
            }
        }

        [SerializeField] private List<StateBase> m_StateBases = new List<StateBase>();

        private void Awake()
        {
            //改为绑定的形式
            // GetComponentsInChildren<StateBase>(true, m_StateBases);
            foreach (var state in m_StateBases)
            {
                state.OnInit(this);
            }
        }

        public bool SelectedIndex(int stateIndex)
        {
            if (stateIndex > states.Count - 1 || stateIndex < 0)
            {
                return false;
            }

            this.selectedStateIndex = stateIndex;
            return true;
        }

        [NoToLua]
        public void SetGray(bool isGray)
        {
            if (m_IsGray == isGray)
            {
                return;
            }

            m_IsGray = isGray;
            
            if (m_GrayStateIndex == -1)
            {
                return;
            }

            if (isGray)
            {
                m_GrayOriginalStateIndex = m_SelectedStateIndex;
                SelectedIndex(m_GrayStateIndex);
            }
            else
            {
                SelectedIndex(m_GrayOriginalStateIndex);
            }
        }
    }
}