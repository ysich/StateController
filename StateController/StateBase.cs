using UnityEngine;

namespace Core.UI.StateController
{
    public abstract class StateBase:MonoBehaviour
    {
        [SerializeField]
        public StateController stateController;
        
        public void OnInit(StateController stateController)
        {
            this.stateController = stateController;
            OnStateInit();
        } 

        protected abstract void OnStateInit();
        public abstract void OnRefresh(int stateIndex);
    }
}