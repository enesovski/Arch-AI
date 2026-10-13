using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public abstract class UpdatableEntityModule : BaseEntityModule, IUpdatableEntityModule
    {
        private EntityCoordinator coordinator;
        private bool hasLoggedMissingCoordinator;

        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterWithUpdater();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterFromUpdater();
        }

        public abstract void PerformUpdate(float deltaTime);

        public abstract void DestroyModule();

        private void RegisterWithUpdater()
        {
            if (coordinator == null)
            {
                coordinator = GetComponent<EntityCoordinator>();
            }

            if (coordinator == null)
            {
                if (!hasLoggedMissingCoordinator)
                {
                    UnityEngine.Debug.LogError($"{GetType().Name} requires EntityCoordinator on {gameObject.name} to register updates.", this);
                    hasLoggedMissingCoordinator = true;
                }

                return;
            }

            coordinator.RegisterModule(this);
        }

        private void UnregisterFromUpdater()
        {
            if (coordinator != null)
            {
                coordinator.UnregisterModule(this);
            }
        }

        protected virtual void OnDestroy()
        {
            DestroyModule();
            UnregisterFromUpdater();
        }
    }
}
