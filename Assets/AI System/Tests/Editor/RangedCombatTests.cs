using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Artifika.AI;
using Artifika.AI.Attack;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class RangedCombatTests
{
    private readonly List<Object> created = new();
    private NavMeshDataInstance mesh;
    private AttackModule attack;
    private MovementModule movement;
    private RangedCombatProfile profile;
    private RangedAttackDefinition definition;
    private GameEntity target;
    private NavMeshAgent agent;
    private readonly Vector3 offset = new(5000f, 0f, 5000f);

    private GameObject NewObject(string name)
    { var obj = new GameObject(name); created.Add(obj); return obj; }
    private T NewAsset<T>() where T : ScriptableObject
    { var asset = ScriptableObject.CreateInstance<T>(); created.Add(asset); return asset; }
    private static void Set(object obj, string name, object value)
    {
        for (var type = obj.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) continue;
            field.SetValue(obj, value);
            return;
        }
        Assert.Fail("Missing field " + name);
    }
    private static object Call(object obj, string name, params object[] args) =>
        obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    private object Controller => typeof(MovementModule).GetField("rangedCombatMovement", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(movement);
    private static Vector3 Goal(object controller) => (Vector3)controller.GetType().GetField("destination", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);

    [TearDown]
    public void Cleanup()
    {
        if (mesh.valid) mesh.Remove();
        for (int i = created.Count - 1; i >= 0; i--) if (created[i]) Object.DestroyImmediate(created[i]);
        created.Clear();
    }
    [UnityTearDown]
    public IEnumerator ExitTestPlayMode()
    { if (Application.isPlaying) yield return new ExitPlayMode(); }

    private static EnterPlayMode Enter() => new(!EditorSettings.enterPlayModeOptionsEnabled ||
        (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0);

    private void Setup(bool native = false, float floorSize = 100f, bool backWall = false)
    {
        var robot = NewObject("test ranged drone");
        robot.SetActive(false);
        robot.layer = 18;
        robot.transform.SetPositionAndRotation(offset, Quaternion.Euler(0f, 90f, 0f));
        robot.AddComponent<EntityCoordinator>();
        attack = robot.AddComponent<AttackModule>();
        typeof(AttackModule).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(attack, null);
        var ranged = robot.AddComponent<RangedAttackModule>();
        Set(ranged, "attackModule", attack);
        Set(attack, "rangedModule", ranged);
        movement = robot.AddComponent<MovementModule>();
        agent = robot.GetComponent<NavMeshAgent>();
        agent.enabled = false;
        agent.autoBraking = false;
        agent.stoppingDistance = 4f;
        var blackboard = robot.AddComponent<Artifika.AI.Blackboard>();
        blackboard.attackModule = attack;
        blackboard.movementModule = movement;
        blackboard.agent = agent;
        movement.SetBlackboard(blackboard);
        Set(attack, "movementModule", movement);
        var speeds = NewAsset<MovementProfile>();
        speeds.runMovementSpeed = 9f;
        speeds.rangedRepositionSpeed = 6f;
        speeds.rangedRetreatSpeed = 9f;
        Set(movement, "movementProfile", speeds);
        profile = NewAsset<RangedCombatProfile>();
        Set(movement, "rangedCombatProfile", profile);
        target = NewObject("ranged target").AddComponent<GameEntity>();
        target.gameObject.layer = 6;
        target.transform.position = offset + Vector3.right * 16f;
        Set(target, "useAimOffset", true);
        Set(target, "aimOffset", Vector3.up * 1.6f);
        target.gameObject.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
        var muzzle = NewObject("elevated muzzle");
        muzzle.transform.SetParent(robot.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 5f, 3.55f);
        definition = NewAsset<RangedAttackDefinition>();
        Set(definition, "attackType", AttackType.Ranged);
        Set(definition, "minRange", 0f);
        Set(definition, "maxRange", 35f);
        Set(definition, "distanceUtilityCurve", AnimationCurve.Constant(0f, 1f, 1f));
        Set(attack, "target", target);
        Set(attack, "attackDefinitions", new List<AttackModule.AttackData> { new() { attackDefinition = definition, attackPoint = muzzle.transform } });
        Set(attack, "cooldowns", new Dictionary<BaseAttackDefinition, float> { { definition, 0f } });
        if (native)
        {
            var sources = new List<NavMeshBuildSource>
            {
                new() { shape = NavMeshBuildSourceShape.Box, size = new Vector3(floorSize, 0.1f, floorSize),
                    transform = Matrix4x4.TRS(offset + Vector3.down * 0.05f, Quaternion.identity, Vector3.one), area = 0 }
            };
            if (backWall) sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(0.2f, 4f, 30f),
                transform = Matrix4x4.TRS(offset + new Vector3(-1.4f, 2f, 0f), Quaternion.identity, Vector3.one), area = 1 });
            var settings = NavMesh.GetSettingsByID(agent.agentTypeID);
            // Retain the deliberately tiny island used by the unreachable-position test.
            settings.minRegionArea = 0f;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources,
                new Bounds(offset, new Vector3(120f, 20f, 120f)), Vector3.zero, Quaternion.identity);
            Assert.IsNotNull(data);
            created.Add(data);
            mesh = NavMesh.AddNavMeshData(data);
            robot.SetActive(true);
            agent.enabled = true;
            Assert.IsTrue(agent.Warp(offset), "Native navigation fixture failed to register.");
            typeof(Unity.Netcode.NetworkBehaviour).GetProperty("IsServer").GetSetMethod(true).Invoke(movement, new object[] { true });
        }
        Physics.SyncTransforms();
    }

    private BoxCollider Wall(float height, float width)
    {
        var wall = NewObject("line of fire obstacle");
        wall.transform.position = offset + new Vector3(8f, height * 0.5f, 0f);
        var collider = wall.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.4f, height, width);
        Physics.SyncTransforms();
        return collider;
    }

    [Test]
    public void ElevatedMuzzleCanSeeOverLowCoverButImpactRechecksNewCover()
    {
        Setup();
        profile.aimPause = 0f;
        var wall = Wall(2f, 8f);
        Assert.IsTrue(Physics.Linecast(movement.transform.position, target.transform.position, 1 << 0));
        Assert.IsTrue(attack.CanStartAttack(), "Root visibility must not override an elevated muzzle's clear shot.");
        wall.transform.position = offset + new Vector3(8f, 4f, 0f);
        wall.size = new Vector3(0.4f, 8f, 8f);
        wall.gameObject.layer = 20; // Structure: excluded by the prefab's visual sensor mask.
        Physics.SyncTransforms();
        Assert.IsFalse((bool)Call(attack, "IsRangedShotClear", definition, target, true),
            "The animation event must refresh visibility rather than use a cached clear shot.");
        Assert.IsFalse(attack.CanStartAttack());
        wall.transform.position = attack.FindAttackData(definition).attackPoint.position;
        wall.size = Vector3.one * 0.5f;
        Physics.SyncTransforms();
        Assert.IsFalse((bool)Call(attack, "IsRangedShotClear", definition, target, true),
            "A muzzle inside a wall must be blocked even when the ray does not hit its containing collider.");
        Set(movement, "rangedCombatProfile", null);
        Assert.IsTrue(attack.CanStartAttack(), "Non-opted-in ranged enemies must retain their existing selection behavior.");
    }

    [UnityTest]
    public IEnumerator RecoveryMovesDuringCooldownThenPausesWithoutReplanningEachTick()
    {
        yield return Enter();
        Setup(true);
        Set(attack, "cooldowns", new Dictionary<BaseAttackDefinition, float> { { definition, 10f } });
        Call(movement, "BeginRangedReposition", target);
        movement.UpdateRangedMovement(target);
        Assert.AreEqual(RangedMovementPhase.Reposition, movement.RangedPhase);
        Assert.IsTrue(agent.hasPath, "Recovery did not create a native path.");
        Assert.IsFalse(agent.isStopped);
        Assert.That(agent.speed, Is.EqualTo(6f));
        Vector3 goal = Goal(Controller);
        Assert.That(Vector3.Distance(goal, movement.transform.position), Is.InRange(2f, 3.2f));
        Set(Controller, "nextPlanAt", 0f);
        movement.UpdateRangedMovement(target);
        Assert.AreEqual(goal, Goal(Controller), "A short step must retain its destination.");
        float arrivalDeadline = Time.time + 3f;
        while (movement.RangedPhase == RangedMovementPhase.Reposition && Time.time < arrivalDeadline)
        {
            yield return null;
            movement.UpdateRangedMovement(target);
        }
        Assert.AreEqual(RangedMovementPhase.Aim, movement.RangedPhase);
        Assert.That(Vector3.Distance(movement.transform.position, goal), Is.LessThan(0.6f), "Native agent must actually reach the short step.");
        Set(attack, "cooldowns", new Dictionary<BaseAttackDefinition, float> { { definition, 0f } });
        Assert.IsFalse(attack.CanStartAttack(), "Arrival must begin an aim pause even when the weapon is ready.");
        float pauseEnds = Time.time + 0.4f;
        while (Time.time < pauseEnds) yield return null;
        Assert.IsTrue(attack.CanStartAttack(), $"After aim pause: phase={movement.RangedPhase}, time={Time.time}, target={target}, position={movement.transform.position}, targetPosition={target.transform.position}, server={movement.IsServer}");
        movement.EndRangedMovement();
        Assert.That(agent.stoppingDistance, Is.EqualTo(4f));
        Assert.IsFalse(agent.autoBraking);
    }

    [UnityTest]
    public IEnumerator RetreatKeepsFixedGoalWhenChasedAndBlockedRetreatAllowsCounterfire()
    {
        yield return Enter();
        Setup(true, backWall: true);
        profile.aimPause = 0f;
        target.transform.position = offset + Vector3.right * 4f;
        movement.UpdateRangedMovement(target);
        if (movement.RangedPhase == RangedMovementPhase.Retreat)
        {
            Vector3 goal = Goal(Controller);
            Assert.That(Vector3.Distance(goal, offset), Is.LessThanOrEqualTo(4.25f));
            target.transform.position = movement.transform.position + Vector3.right;
            Set(Controller, "nextPlanAt", 0f);
            movement.UpdateRangedMovement(target);
            Assert.AreEqual(goal, Goal(Controller), "A pursuing player must not extend the retreat target.");
            Set(Controller, "phaseDeadline", Time.time - 1f);
            movement.UpdateRangedMovement(target);
        }
        Assert.AreEqual(RangedMovementPhase.Aim, movement.RangedPhase);
        Assert.IsTrue(attack.CanStartAttack(), "A blocked/timed-out retreat must release a valid close-range shot.");
        movement.ResetRangedCombat();
        Assert.AreEqual(RangedMovementPhase.Aim, movement.RangedPhase);
    }

    [UnityTest]
    public IEnumerator UnreachablePreferredPositionStillAllowsClearShotFromNavigationIsland()
    {
        yield return Enter();
        Setup(true, floorSize: 2f);
        profile.aimPause = 0f;
        target.transform.position = offset + Vector3.right * 25f;
        Physics.SyncTransforms();
        movement.UpdateRangedMovement(target);
        Assert.AreEqual(RangedMovementPhase.Aim, movement.RangedPhase);
        Assert.IsFalse(agent.hasPath);
        Assert.IsTrue(attack.CanStartAttack(), "Preferred spacing must not deadlock a valid shot when the NavMesh cannot reach that spacing.");
        movement.UpdateRangedMovement(target);
        Assert.AreEqual(RangedMovementPhase.Aim, movement.RangedPhase, "Fallback must allow the aim pause before retrying approach.");
    }

    [UnityTest]
    public IEnumerator BlockedShotFindsReachableSideAngleAndDistantTargetGetsApproach()
    {
        yield return Enter();
        Setup(true);
        Wall(8f, 2f);
        profile.aimPause = 0f;
        Assert.IsFalse(attack.CanStartAttack());
        movement.UpdateRangedMovement(target); // initial aim poll is throttled
        Set(Controller, "nextPlanAt", 0f);
        movement.UpdateRangedMovement(target);
        Assert.AreEqual(RangedMovementPhase.Reposition, movement.RangedPhase);
        Assert.IsTrue(agent.hasPath, "Obstruction must produce movement, not indefinite facing.");
        Vector3 goal = Goal(Controller);
        Assert.IsTrue(agent.Warp(goal));
        movement.transform.rotation = Quaternion.LookRotation(target.transform.position - goal);
        Physics.SyncTransforms();
        Assert.IsTrue((bool)Call(attack, "HasRangedShotAt", goal, target));
        movement.Stop();
        movement.UpdateRangedMovement(target);
        Assert.IsTrue(attack.CanStartAttack());
        target.transform.position = offset + Vector3.right * 40f;
        movement.UpdateRangedMovement(target);
        Assert.AreEqual(RangedMovementPhase.Approach, movement.RangedPhase);
        Assert.IsTrue(agent.hasPath);
        Assert.That(agent.speed, Is.EqualTo(9f));
        Assert.IsFalse(attack.CanStartAttack());
    }
}
