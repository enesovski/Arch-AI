using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Blackboard = Artifika.AI.Blackboard;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public abstract class BaseEntityModule : MonoBehaviour, IEntityModule
    {
        protected Blackboard blackboard { get; private set; }
        public virtual void SetBlackboard(Blackboard newBlackboard)
        {
            blackboard = newBlackboard;
        }

        private bool isInitialized = false;
        public GameObject GameObject => gameObject;
        public Transform Transform => transform;
        public bool IsEnabled { get; private set; }

        protected virtual void OnEnable()
        {
            IsEnabled = true;
            if (isInitialized)
            {
                OnModuleEnabled();
            }
        }

        protected virtual void OnDisable()
        {
            IsEnabled = false;
            OnModuleDisabled();
        }

        public virtual void Initialize()
        {
            if (isInitialized) return;

            OnInitialize();
            isInitialized = true;

            if (IsEnabled)
            {
                OnModuleEnabled();
            }
        }

        protected abstract void OnInitialize();
        public virtual void OnModuleEnabled() { }
        public virtual void OnModuleDisabled() { }
    }
}
