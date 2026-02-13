using Artifika.AI;
using Sirenix.OdinInspector;
using UnityEngine;

using Blackboard = Artifika.AI.Blackboard;
public abstract class BaseEntityModule : MonoBehaviour, IEntityModule
{
    protected Blackboard blackboard { get; private set; }
    public virtual void SetBlackboard(Blackboard _blackboard)
    {
        blackboard = _blackboard;
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
        GameLog.Info("initialize, " + isInitialized);
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
