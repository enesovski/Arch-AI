using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Artifika.AI.Death.Reactions
{
    [DisallowMultipleComponent]
    public class DeathReactionsBus : MonoBehaviour, IReactionBus
    {
        private HealthComponent healthComponent;
        private Blackboard blackboard;

        [Title("Managed (SerializeReference) Reactions")]
        [SerializeReference, ListDrawerSettings(Expanded = true)]
        private List<IDeathReaction> deathStartManaged = new();

        [SerializeReference, ListDrawerSettings(Expanded = true)]
        private List<IDeathReaction> deathEndManaged = new();

        [Title("Component (MonoBehaviour/NetworkBehaviour) Reactions")]
        [SerializeField, ListDrawerSettings(Expanded = true)]
        [ValidateInput(nameof(ValidateAllImplementReaction), "One or more entries do not implement IDeathReaction.")]
        private List<MonoBehaviour> deathStartComponents = new();

        [SerializeField, ListDrawerSettings(Expanded = true)]
        [ValidateInput(nameof(ValidateAllImplementReaction), "One or more entries do not implement IDeathReaction.")]
        private List<MonoBehaviour> deathEndComponents = new();

        private readonly List<IDeathReaction> startRuntime = new();
        private readonly List<IDeathReaction> endRuntime = new();

        public void Initialize(Blackboard blackboard)
        {
            this.blackboard = blackboard;
            healthComponent = blackboard.healthComponent;

            BuildRuntimeLists();
            
            if (healthComponent != null)
                healthComponent.OnDeath += ExecuteStartReactions;

            foreach (var r in startRuntime) SafeInit(r);
            foreach (var r in endRuntime) SafeInit(r);
        }

        private void OnDisable()
        {
            if (healthComponent != null)
                healthComponent.OnDeath -= ExecuteStartReactions;
        }

        [Button(ButtonSizes.Medium), GUIColor(0.7f, 0.9f, 1f)]
        private void RebuildNow()
        {
            BuildRuntimeLists();
        }

        private void BuildRuntimeLists()
        {
            startRuntime.Clear();
            endRuntime.Clear();

            startRuntime.AddRange(deathStartManaged);
            endRuntime.AddRange(deathEndManaged);

            AddComponents(deathStartComponents, startRuntime);
            AddComponents(deathEndComponents, endRuntime);
        }

        private void AddComponents(List<MonoBehaviour> src, List<IDeathReaction> dst)
        {
            for (int i = 0; i < src.Count; i++)
            {
                var mb = src[i];
                if (mb == null) continue;

                if (mb is IDeathReaction r)
                    dst.Add(r);
            }
        }

        private void SafeInit(IDeathReaction reaction)
        {
            try { reaction?.Initialize(blackboard); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        private void ExecuteStartReactions()
        {
            foreach (var r in startRuntime)
            {
                try { r.Execute(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        public void ExecuteEndReactions()
        {
            foreach (var r in endRuntime)
            {
                try { r.Execute(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        private bool ValidateAllImplementReaction(List<MonoBehaviour> list)
        {
            if (list == null) return true;
            foreach (var mb in list)
            {
                if (mb == null) continue;
                if (mb is not IDeathReaction) return false;
            }
            return true;
        }
    }
}
