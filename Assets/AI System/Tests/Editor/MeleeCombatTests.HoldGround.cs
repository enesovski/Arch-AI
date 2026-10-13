using System.Collections;
using System.Collections.Generic;
using Artifika.AI;
using Artifika.AI.Attack;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public partial class MeleeCombatTests
{
    private object ConfigureHoldGround(AttackModule attack, MovementModule movement)
    {
        MovementProfile speeds = Object.Instantiate(AssetDatabase.LoadAssetAtPath<MovementProfile>(
            "Assets/Project/Systems/AI System/Data/Movement Config/Mecha Movement Profile.asset"));
        spawned.Add(speeds);
        Set(movement, "movementProfile", speeds);
        MeleeCombatProfile combat = Object.Instantiate(AssetDatabase.LoadAssetAtPath<MeleeCombatProfile>(
            "Assets/Project/Systems/AI System/Data/Combat Configuration/Mecha Melee Combat Profile.asset"));
        spawned.Add(combat);
        Assert.That(combat.recoveryMode, Is.EqualTo(MeleeRecoveryMode.HoldGround));
        return ConfigureCombatMovement(attack, combat);
    }

    [UnityTest]
    public IEnumerator HoldGroundUsesFixedRetreatAndLocalStepsThenCountersOrChases()
    {
        bool reload = !EditorSettings.enterPlayModeOptionsEnabled ||
                      (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
        yield return new EnterPlayMode(reload);
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out MeleeAttackDefinition def);
        MovementModule movement = EnableTestNavigation(attack, target);
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        object controller = ConfigureHoldGround(attack, movement);
        BeginWithdrawal(attack, target);
        Vector3 withdrawalStart = movement.transform.position;
        movement.UpdateMeleeMovement(target);
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Withdraw));
        Assert.IsTrue(agent.hasPath);
        Vector3 retreatGoal = Get<Vector3>(controller, "meleeDestination");
        Assert.That(Vector3.Distance(retreatGoal, withdrawalStart), Is.LessThanOrEqualTo(2.05f));
        Assert.That(agent.speed, Is.EqualTo(2.5f));
        yield return null;

        target.transform.position += Vector3.left * 0.75f;
        Physics.SyncTransforms();
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.That(Get<Vector3>(controller, "meleeDestination"), Is.EqualTo(retreatGoal),
            "Player pressure must not move the fixed retreat goal farther away.");

        Assert.IsTrue(agent.Warp(retreatGoal));
        Set(controller, "phaseStartedAt", Time.time - 1f);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target); // Finish the step.
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target); // Enter local recovery.
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Circle));
        Vector3 anchor = Get<Vector3>(controller, "holdGroundAnchor");
        Vector3 sideGoal = Get<Vector3>(controller, "meleeDestination");
        Assert.IsTrue(agent.hasPath);
        Assert.That(Vector3.Distance(sideGoal, anchor), Is.LessThanOrEqualTo(0.55f));

        var cooldowns = Get<Dictionary<BaseAttackDefinition, float>>(attack, "cooldowns");
        cooldowns[def] = 5f;
        Set(controller, "circleUntil", Time.time + 10f);
        target.transform.position = anchor + Vector3.right * 6f + Vector3.up;
        Physics.SyncTransforms();
        Set(attack, "attackRangeSamples", null);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.That(Get<Vector3>(controller, "holdGroundAnchor"), Is.EqualTo(anchor));
        Assert.That(Get<Vector3>(controller, "meleeDestination"), Is.EqualTo(sideGoal));
        Assert.IsFalse(attack.CanStartAttack(), "Pressure must not bypass the cooldown.");

        Assert.IsTrue(agent.Warp(sideGoal));
        movement.Stop();
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsTrue(movement.IsMeleeCirclingPaused);
        Assert.IsTrue(agent.isStopped);
        Assert.IsFalse(agent.hasPath);
        Assert.That(agent.speed, Is.Zero);

        cooldowns[def] = 0f;
        Set(attack, "attackRangeSamples", null);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Rush));
        Assert.IsTrue(attack.CanStartAttack(), "A ready attack must answer player pressure without another retreat.");
        Assert.IsFalse(agent.hasPath);

        // An out-of-range target must still be chased once recovery expires.
        BeginWithdrawal(attack, target);
        Set(controller, "phaseStartedAt", Time.time - 3f);
        movement.UpdateMeleeMovement(target);
        target.transform.position += Vector3.right * 8f;
        Physics.SyncTransforms();
        Set(attack, "attackRangeSamples", null);
        Set(controller, "circleUntil", Time.time - 1f);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Rush));
        Assert.IsTrue(agent.hasPath, "Holding ground must not permanently disable pursuit.");
    }

    [UnityTest]
    public IEnumerator HoldGroundStopsAtBackWallOrStalledRetreat()
    {
        bool reload = !EditorSettings.enterPlayModeOptionsEnabled ||
                      (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
        yield return new EnterPlayMode(reload);
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out _);
        MovementModule movement = EnableTestNavigation(attack, target, backWall: true);
        object controller = ConfigureHoldGround(attack, movement);
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        Assert.IsTrue(agent.Raycast(movement.transform.position + Vector3.left * 2f, out NavMeshHit _),
            "The fixture must block the direct retreat with a real NavMesh wall.");
        BeginWithdrawal(attack, target);
        movement.UpdateMeleeMovement(target);
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Circle),
            "With no room behind, skip the retreat instead of routing around the wall.");
        Assert.That(Get<Vector3>(controller, "holdGroundAnchor"), Is.EqualTo(movement.transform.position));
        TearDown();

        attack = CreateMeleeAttacker(out target, out _);
        movement = EnableTestNavigation(attack, target);
        controller = ConfigureHoldGround(attack, movement);
        agent = movement.GetComponent<NavMeshAgent>();
        BeginWithdrawal(attack, target);
        movement.UpdateMeleeMovement(target);
        yield return null;
        agent.isStopped = true; // Simulate local avoidance stalling an otherwise complete path.
        Set(controller, "bestCircleStepDistance", 0f);
        Set(controller, "circleStepProgressAt", Time.time - 3f);
        Set(controller, "phaseStartedAt", Time.time);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Circle));
        Assert.That(Get<Vector3>(controller, "holdGroundAnchor"), Is.EqualTo(movement.transform.position));
    }
}
