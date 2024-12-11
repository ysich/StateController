
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Core.UI.StateController
{
    public abstract class SelectableStateBase<T>:StateBase
    {
        [SerializeField]
        protected List<T> m_StateDataList = new List<T>();
        public List<T> stateDataList => m_StateDataList;

        public override void OnRefresh(int stateIndex)
        {
            if (stateIndex > m_StateDataList.Count - 1)
            {
                return;
            }

            T t = m_StateDataList[stateIndex];
            OnStateChanged(t); 
        }

        protected abstract void OnStateChanged(T t);
        
        
    }
}