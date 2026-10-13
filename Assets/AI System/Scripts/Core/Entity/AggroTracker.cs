using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public class AggroTracker : NetworkBehaviour
    {
        private readonly List<GameEntity> aggroEntities = new List<GameEntity>(8);
        private readonly NetworkVariable<int> aggroCountSync = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private bool publishedCombatState;

        public IReadOnlyList<GameEntity> AggroEntities => aggroEntities;

        public bool IsInCombat => IsSpawned ? aggroCountSync.Value > 0 : aggroEntities.Count > 0;

        public event Action<GameEntity> OnAggroAdded;
        public event Action<GameEntity> OnAggroRemoved;
        public event Action OnCombatStarted;

        public event Action OnCombatEnded;

        public void RegisterAggro(GameEntity entity)
        {
            if (!entity) return;
            if (IsSpawned && !IsServer) return;
            if (aggroEntities.Contains(entity)) return;

            aggroEntities.Add(entity);
            OnAggroAdded?.Invoke(entity);
            PublishAndSyncCombatState();
        }

        public void UnregisterAggro(GameEntity entity)
        {
            if (!entity) return;
            if (IsSpawned && !IsServer) return;
            if (!aggroEntities.Contains(entity)) return;

            aggroEntities.Remove(entity);
            OnAggroRemoved?.Invoke(entity);
            PublishAndSyncCombatState();
        }

        public bool HasAggro(GameEntity entity) => aggroEntities.Contains(entity);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            aggroCountSync.OnValueChanged += HandleAggroCountChanged;

            if (IsServer)
                aggroCountSync.Value = aggroEntities.Count;

            PublishCombatState(IsInCombat);
        }

        public override void OnNetworkDespawn()
        {
            aggroCountSync.OnValueChanged -= HandleAggroCountChanged;
            PublishCombatState(false);
            aggroEntities.Clear();
            base.OnNetworkDespawn();
        }

        private void PublishAndSyncCombatState()
        {
            if (IsSpawned && IsServer)
                aggroCountSync.Value = aggroEntities.Count;

            PublishCombatState(aggroEntities.Count > 0);
        }

        private void HandleAggroCountChanged(int previous, int current)
        {
            PublishCombatState(current > 0);
        }

        private void PublishCombatState(bool isInCombat)
        {
            if (publishedCombatState == isInCombat)
                return;

            publishedCombatState = isInCombat;

            if (isInCombat)
                OnCombatStarted?.Invoke();
            else
                OnCombatEnded?.Invoke();
        }
    }
}
