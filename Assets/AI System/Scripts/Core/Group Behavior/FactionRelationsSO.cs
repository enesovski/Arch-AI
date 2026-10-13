using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Artifika.AI
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    [CreateAssetMenu(fileName = "FactionRelations", menuName = "AI/Factions/Faction Relations")]
    public sealed class FactionRelationsSO : ScriptableObject
    {
        private const string ResourcePath = "FactionRelations";

        private static FactionRelationsSO instance;
        private static bool hasLoggedMissingAsset;

        [SerializeField, HideInInspector]
        private FactionMask[] damagePermissions = Array.Empty<FactionMask>();

        public static FactionRelationsSO Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = Resources.Load<FactionRelationsSO>(ResourcePath);

                if (instance == null && !hasLoggedMissingAsset)
                {
                    Debug.LogError(
                        $"Missing {nameof(FactionRelationsSO)} Resources asset at " +
                        $"'{ResourcePath}'. Entity-to-entity damage will be rejected.");
                    hasLoggedMissingAsset = true;
                }

                return instance;
            }
        }

        public static bool AllowsDamage(GameEntity attacker, GameEntity target)
        {
            FactionRelationsSO relations = Instance;
            return relations != null && relations.CanDamage(attacker, target);
        }

        public static bool AllowsDamage(FactionType attackerFaction, GameEntity target)
        {
            FactionRelationsSO relations = Instance;
            return relations != null && target != null &&
                relations.CanDamage(attackerFaction, target.Faction);
        }

        public bool CanDamage(GameEntity attacker, GameEntity target)
        {
            if (attacker == null || target == null)
                return false;

            if (attacker == target)
                return false;

            if (attacker.NetworkObject != null &&
                attacker.NetworkObject == target.NetworkObject)
            {
                return false;
            }

            return CanDamage(attacker.Faction, target.Faction);
        }

        public bool CanDamage(FactionType attackerFaction, FactionType targetFaction)
        {
            int attackerIndex = (int)attackerFaction;
            int targetIndex = (int)targetFaction;
            if (damagePermissions == null ||
                attackerIndex < 0 || attackerIndex >= damagePermissions.Length ||
                targetIndex < 0 || targetIndex >= sizeof(int) * 8)
            {
                return false;
            }

            FactionMask targetFlag = (FactionMask)(1 << targetIndex);
            return (damagePermissions[attackerIndex] & targetFlag) != 0;
        }

        private void OnValidate()
        {
            EnsureMatrixSize();
        }

        public void EnsureMatrixSize()
        {
            int requiredSize = GetRequiredMatrixSize();
            if (damagePermissions != null && damagePermissions.Length == requiredSize)
                return;

            var resized = new FactionMask[requiredSize];
            if (damagePermissions != null)
            {
                Array.Copy(damagePermissions, resized, Mathf.Min(damagePermissions.Length, requiredSize));
            }

            damagePermissions = resized;
        }

        private static int GetRequiredMatrixSize()
        {
            int highestValue = -1;
            foreach (FactionType faction in Enum.GetValues(typeof(FactionType)))
                highestValue = Mathf.Max(highestValue, (int)faction);

            return highestValue + 1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCachedInstance()
        {
            instance = null;
            hasLoggedMissingAsset = false;
        }
    }
}
