public abstract class UpdatableEntityModule : BaseEntityModule, IUpdatableEntityModule
{
    private EntityCoordinator _coordinator;
    private bool _hasLoggedMissingCoordinator;

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
        if (_coordinator == null)
        {
            _coordinator = GetComponent<EntityCoordinator>();
        }

        if (_coordinator == null)
        {
            if (!_hasLoggedMissingCoordinator)
            {
                UnityEngine.Debug.LogError($"{GetType().Name} requires EntityCoordinator on {gameObject.name} to register updates.", this);
                _hasLoggedMissingCoordinator = true;
            }

            return;
        }

        _coordinator.RegisterModule(this);
    }

    private void UnregisterFromUpdater()
    {
        if (_coordinator != null)
        {
            _coordinator.UnregisterModule(this);
        }
    }

    protected virtual void OnDestroy()
    {
        DestroyModule();
        UnregisterFromUpdater();
    }
}