using System;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [DisallowMultipleComponent]
    public class CompanionStateModule : NetworkBehaviour
    {
        public event Action<CompanionState, CompanionState> OnStateChanged;

        [SerializeField] private CompanionState initialState = CompanionState.Autonomous;

        [Title("Runtime Debug")]
        [HideInEditorMode]
        [ShowInInspector, ReadOnly]
        public CompanionState CurrentState => (CompanionState)currentState.Value;

        private readonly NetworkVariable<byte> currentState = new(
            writePerm: NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                currentState.Value = (byte)initialState;

            currentState.OnValueChanged += HandleStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            currentState.OnValueChanged -= HandleStateChanged;
        }

        public bool IsInState(CompanionState state)
        {
            return CurrentState == state;
        }

        public void SetState(CompanionState nextState)
        {
            if (!IsServer)
                return;

            if (CurrentState == nextState)
                return;

            currentState.Value = (byte)nextState;
        }

        private void HandleStateChanged(byte previousValue, byte nextValue)
        {
            OnStateChanged?.Invoke((CompanionState)previousValue, (CompanionState)nextValue);
        }

        public void SetAutonomous() => SetState(CompanionState.Autonomous);
        public void SetCommanded() => SetState(CompanionState.Commanded);
        public void SetMounted() => SetState(CompanionState.Mounted);
        public void SetDisabled() => SetState(CompanionState.Disabled);
    }
}
