using System;
using Unity.Netcode;
using UnityEngine;

namespace Artifika.AI.Attack
{
    public abstract class AttackSubModule : MonoBehaviour, IAttackSubModule
    {
        [SerializeField] protected AttackModule attackModule;
        protected GameEntity gameEntity;
        protected NetworkObject selfNetworkObject;

        public AttackType ModuleType;
        public event Action OnAttack;

        private void Awake()
        {
            gameEntity = GetComponent<GameEntity>();
            selfNetworkObject = GetComponent<NetworkObject>();
        }

        protected void InvokeOnAttack() => OnAttack?.Invoke();

        public abstract void Attack(BaseAttackDefinition def);
    }
}