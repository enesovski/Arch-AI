using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Artifika.AI;
using Artifika.AI.Attack;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public partial class MeleeCombatTests
{
    private readonly List<Object> spawned = new();
    private NavMeshDataInstance navigationInstance;

    [TearDown]
    public void TearDown()
    {
        if (navigationInstance.valid) navigationInstance.Remove();
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i]) Object.DestroyImmediate(spawned[i]);
        spawned.Clear();
    }

    private GameObject CreateObject(string name)
    {
        var obj = new GameObject(name);
        spawned.Add(obj);
        return obj;
    }

    private T CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        spawned.Add(asset);
        return asset;
    }

    private static void Set(object instance, string field, object value)
    {
        for (System.Type type = instance.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null) continue;
            info.SetValue(instance, value);
            return;
        }
        Assert.Fail("Missing field " + field);
    }

    [Test]
    public void ClosestPointUsesLargeTargetsSurfaceRatherThanCenter()
    {
        GameObject obj = CreateObject("large target");
        obj.transform.position = new Vector3(8f, 1f, 0f);
        GameEntity entity = obj.AddComponent<GameEntity>();
        BoxCollider body = obj.AddComponent<BoxCollider>();
        body.size = new Vector3(10f, 2f, 10f);

        Assert.IsTrue(entity.TryGetClosestCombatPoint(Vector3.up, ~0, out Vector3 point));
        Assert.That(point.x, Is.EqualTo(3f).Within(0.001f));
        Assert.That(Vector3.Distance(Vector3.up, point), Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void RangeUsesActualRotatedShapeRatherThanEnclosingBounds()
    {
        GameObject obj = CreateObject("rotated target");
        obj.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        GameEntity entity = obj.AddComponent<GameEntity>();
        BoxCollider body = obj.AddComponent<BoxCollider>();
        body.size = new Vector3(10f, 2f, 1f);
        Vector3 origin = new Vector3(3f, 0f, 3f);

        Assert.That((body.bounds.ClosestPoint(origin) - origin).sqrMagnitude, Is.LessThan(0.001f));
        Assert.IsTrue(entity.TryGetClosestCombatPoint(origin, ~0, out Vector3 point));
        Assert.That(Vector3.Distance(origin, point), Is.GreaterThan(2f));
    }

    [Test]
    public void ChildBodiesWorkButTriggersDisabledBodiesAndNestedEntitiesAreExcluded()
    {
        GameEntity entity = CreateObject("target").AddComponent<GameEntity>();
        GameObject child = CreateObject("body");
        child.transform.SetParent(entity.transform);
        child.transform.position = new Vector3(4f, 0f, 0f);
        BoxCollider body = child.AddComponent<BoxCollider>();
        GameObject nested = CreateObject("other entity");
        nested.transform.SetParent(entity.transform);
        nested.AddComponent<GameEntity>();
        nested.AddComponent<BoxCollider>();

        Assert.IsTrue(entity.TryGetClosestCombatPoint(Vector3.zero, ~0, out Vector3 point));
        Assert.That(point.x, Is.EqualTo(3.5f).Within(0.001f));
        body.enabled = false;
        Assert.IsFalse(entity.TryGetClosestCombatPoint(Vector3.zero, ~0, out _));
        body.enabled = true;
        body.isTrigger = true;
        Assert.IsFalse(entity.TryGetClosestCombatPoint(Vector3.zero, ~0, out _));
        body.isTrigger = false;
        child.layer = 8;
        Assert.IsFalse(entity.TryGetClosestCombatPoint(Vector3.zero, 1 << 0, out _));
    }

    private AttackModule CreateMeleeAttacker(out GameEntity target, out MeleeAttackDefinition def)
    {
        GameObject robot = CreateObject("attacker");
        robot.SetActive(false);
        robot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        robot.AddComponent<EntityCoordinator>();
        AttackModule attack = robot.AddComponent<AttackModule>();
        MeleeAttackModule melee = robot.AddComponent<MeleeAttackModule>();
        // EditMode does not run these MonoBehaviours' normal runtime activation.
        typeof(AttackModule).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(attack, null);
        Set(melee, "attackModule", attack);
        Set(melee, "attackableLayers", (LayerMask)(1 << 0));
        Set(attack, "meleeModule", melee);
        target = CreateObject("large target").AddComponent<GameEntity>();
        target.transform.position = new Vector3(8f, 1f, 0f);
        target.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        target.gameObject.AddComponent<BoxCollider>().size = new Vector3(10f, 2f, 10f);
        GameObject origin = CreateObject("attack point");
        origin.transform.SetParent(robot.transform);
        origin.transform.position = new Vector3(0.5f, 1f, 0f);
        def = CreateAsset<MeleeAttackDefinition>();
        Set(def, "maxRange", 3f);
        Set(def, "distanceUtilityCurve", AnimationCurve.Constant(0f, 1f, 1f));
        Set(attack, "target", target);
        Set(attack, "attackDefinitions", new List<AttackModule.AttackData>
        {
            new() { attackPoint = origin.transform, attackDefinition = def }
        });
        Set(attack, "cooldowns", new Dictionary<BaseAttackDefinition, float> { { def, 0f } });
        MovementModule movement = robot.AddComponent<MovementModule>();
        Blackboard blackboard = robot.AddComponent<Blackboard>();
        blackboard.attackModule = attack;
        blackboard.movementModule = movement;
        blackboard.agent = robot.GetComponent<UnityEngine.AI.NavMeshAgent>();
        movement.SetBlackboard(blackboard);
        Set(attack, "movementModule", movement);
        return attack;
    }

    private static object ConfigureCombatMovement(AttackModule attack, MeleeCombatProfile profile)
    {
        MovementModule movement = attack.GetComponent<MovementModule>();
        Set(movement, "meleeCombatProfile", profile);
        movement.BeginMeleeMovement();
        return typeof(MovementModule).GetField("meleeCombatMovement", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(movement);
    }

    private static void BeginWithdrawal(AttackModule attack, GameEntity target)
    {
        typeof(MovementModule).GetMethod("BeginMeleeWithdrawal", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(attack.GetComponent<MovementModule>(), new object[] { target });
    }

    private static object InvokeController(object controller, string method, params object[] args)
    {
        return controller.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, args);
    }

    private static T Get<T>(object instance, string field)
    {
        return (T)instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    }

    private MovementModule EnableTestNavigation(AttackModule attack, GameEntity target, bool inclinedFloor = false, bool backWall = false)
    {
        Vector3 offset = new Vector3(5000f, 0f, 5000f);
        MovementModule movement = attack.GetComponent<MovementModule>();
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        agent.enabled = false;
        movement.transform.position += offset;
        target.transform.position += offset;
        Quaternion floorRotation = inclinedFloor ? Quaternion.Euler(0f, 0f, 20f) : Quaternion.identity;
        Vector3 floorPosition = offset + Vector3.down * 0.05f;
        if (inclinedFloor)
        {
            GameObject floor = CreateObject("inclined test ground");
            floor.transform.SetPositionAndRotation(floorPosition, floorRotation);
            floor.AddComponent<BoxCollider>().size = new Vector3(60f, 0.1f, 60f);
        }
        var sources = new List<NavMeshBuildSource>
        {
            new() { shape = NavMeshBuildSourceShape.Box, size = new Vector3(60f, 0.1f, 60f),
                transform = Matrix4x4.TRS(floorPosition, floorRotation, Vector3.one), area = 0 }
        };
        if (backWall)
            sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(0.2f, 4f, 8f),
                transform = Matrix4x4.TRS(offset + new Vector3(-0.8f, 2f, 0f), Quaternion.identity, Vector3.one), area = 1 });
        NavMeshData data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(agent.agentTypeID), sources,
            new Bounds(offset, new Vector3(60f, 40f, 60f)), Vector3.zero, Quaternion.identity);
        Assert.IsNotNull(data);
        spawned.Add(data);
        navigationInstance = NavMesh.AddNavMeshData(data);
        movement.gameObject.SetActive(true);
        agent.enabled = true;
        Assert.IsTrue(agent.Warp(movement.transform.position),
            $"Fixture agent failed to register on the baked NavMesh. playing={Application.isPlaying}, enabled={agent.enabled}, onMesh={agent.isOnNavMesh}, position={movement.transform.position}");
        typeof(Unity.Netcode.NetworkBehaviour).GetProperty("IsServer").GetSetMethod(true)
            .Invoke(movement, new object[] { true });
        MovementProfile speeds = CreateAsset<MovementProfile>();
        speeds.runMovementSpeed = 10f;
        speeds.combatCirclingSpeed = 7f;
        speeds.combatCircleDegreesPerSecond = 55f;
        speeds.combatReplanInterval = 0.15f;
        speeds.combatApproachRangeFraction = 0.9f;
        Set(movement, "movementProfile", speeds);
        return movement;
    }

    [UnityTest]
    public IEnumerator NavigationKeepsCirclingMovingAndCorrectsArrivedRushRange()
    {
        // Native agents register on NavMesh only in Play Mode. The EditMode
        // runner provides an isolated test scene and restores the user's scene.
        bool reloadDomain = !EditorSettings.enterPlayModeOptionsEnabled ||
                            (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
        yield return new EnterPlayMode(reloadDomain);
        CircleKeepsNavigationMovingAndRestoresBrakingWhenChaseEnds();
        TearDown();
        ArrivedRushOutsideSurfaceRangeReplacesStoppedPathAndClosesOnlyTheMissingDistance();
        TearDown();
        RushRejectsAnOutOfRangeGoalSampledBackToItsCurrentPosition();
        TearDown();
        RushSamplesLocalNavigationHeightWhenTargetRootIsAboveItsBody();
        TearDown();
        CombatSamplingFindsSlopeFloorBetweenBothEndpointHeights();
    }

    [UnityTearDown]
    public IEnumerator LeaveTestPlayMode()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator CirclingStepsPauseResumeAndReactToCombatChanges()
    {
        bool reloadDomain = !EditorSettings.enterPlayModeOptionsEnabled ||
                            (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
        yield return new EnterPlayMode(reloadDomain);
        try { CheckCirclingSteps(); }
        catch (System.Exception exception) { Assert.Fail(exception.ToString()); }
    }

    private void CheckCirclingSteps()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out _);
        MovementModule movement = EnableTestNavigation(attack, target);
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        MovementProfile speeds = movement.MovementProfile;
        RotationModule rotation = movement.gameObject.AddComponent<RotationModule>();
        rotation.Initialize(agent, null);
        movement.GetComponent<Blackboard>().rotationModule = rotation;
        speeds.combatStepCircling = true;
        speeds.combatCircleStepDistance = 1.5f;
        speeds.combatCirclePauseRange = new Vector2(1f, 1f);
        MeleeCombatProfile profile = CreateAsset<MeleeCombatProfile>();
        profile.holdingDistance = 10f;
        object controller = ConfigureCombatMovement(attack, profile);
        Assert.IsTrue(agent.Warp(target.transform.position + Vector3.left * 15f), "Could not warp to the holding position.");
        Set(controller, "movementTarget", target);
        Set(controller, "sectorYaw", -90f);
        Set(controller, "circleDirection", 1f);
        Set(controller, "meleePhase", MeleeMovementPhase.Circle);
        Set(controller, "circleUntil", Time.time + 100f);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsTrue(agent.autoBraking, "Discrete circle steps must brake before waiting.");
        Assert.IsTrue(agent.hasPath, "The first discrete circle step must have a path.");
        Vector3 step = Get<Vector3>(controller, "meleeDestination");
        Vector3 travel = step - movement.transform.position;
        travel.y = 0f;
        Assert.That(travel.magnitude, Is.LessThanOrEqualTo(1.8f), "Step goals must respect the distance cap plus arrival tolerance.");
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.That(Get<Vector3>(controller, "meleeDestination"), Is.EqualTo(step), "A step must keep its goal until arrival.");

        // Arrive without waiting for a full locomotion animation in this fixture.
        Assert.IsTrue(agent.Warp(step), "Could not warp to the first step.");
        movement.Stop();
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsTrue(movement.IsMeleeCirclingPaused, "The first arrived step must start a pause.");
        Assert.IsTrue(agent.isStopped, "An active pause must stop the native agent.");
        Assert.IsFalse(agent.hasPath);
        Assert.That(agent.speed, Is.Zero);
        Assert.That(rotation.CurrentMode, Is.EqualTo(RotationMode.LookAt), "Waiting must retain target-facing rotation.");
        float pauseEnds = Get<float>(controller, "circlePauseUntil");
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.That(Get<float>(controller, "circlePauseUntil"), Is.EqualTo(pauseEnds), "Ticking a pause must not restart its timer.");

        Set(controller, "circlePauseUntil", Time.time - 0.1f);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsFalse(movement.IsMeleeCirclingPaused);
        Assert.IsTrue(agent.hasPath, "An expired pause must resume circling without player movement.");
        Assert.IsFalse(agent.isStopped);
        Assert.That(agent.speed, Is.EqualTo(speeds.GetCombatCirclingSpeed()));

        // A physically stalled fixed step must not be retained forever.
        Set(controller, "bestCircleStepDistance", 0f);
        Set(controller, "circleStepProgressAt", Time.time - profile.blockedSectorGrace - 1f);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsTrue(agent.hasPath, "The replanned stalled step must have a path.");
        Assert.That(Get<float>(controller, "circleStepProgressAt"), Is.EqualTo(Time.time), "A stalled fixed step must be replanned after the grace.");

        step = Get<Vector3>(controller, "meleeDestination");
        Assert.IsTrue(agent.Warp(step), "Could not warp to the replanned step.");
        movement.Stop();
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsTrue(movement.IsMeleeCirclingPaused, "The replanned step must start a pause on arrival.");
        target.transform.position += Vector3.right * 3f;
        Physics.SyncTransforms();
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsFalse(movement.IsMeleeCirclingPaused, "A spacing change must cancel waiting.");
        Assert.IsTrue(agent.hasPath, "A spacing correction must have a path.");
        Assert.IsFalse(agent.isStopped);

        step = Get<Vector3>(controller, "meleeDestination");
        Assert.IsTrue(agent.Warp(step), "Could not warp to the spacing correction.");
        movement.Stop();
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsTrue(movement.IsMeleeCirclingPaused, "The spacing correction must start a pause on arrival.");
        Set(controller, "circleUntil", Time.time - 0.1f);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Rush));
        Assert.IsFalse(movement.IsMeleeCirclingPaused, "Recovery expiry with a ready attack must interrupt waiting.");
        Assert.IsFalse(agent.isStopped);
        Assert.That(agent.speed, Is.EqualTo(speeds.runMovementSpeed));

        InvokeController(controller, "SetMeleePhase", MeleeMovementPhase.Circle);
        Set(controller, "circleUntil", Time.time + 100f);
        Set(controller, "circlePaused", true);
        Set(controller, "circlePauseUntil", Time.time + 10f);
        target.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsFalse(movement.IsMeleeCirclingPaused, "Leaving the preferred live sector must cancel waiting.");

        // Target changes and chase teardown must not leak an old pause.
        Set(controller, "circlePaused", true);
        GameEntity replacement = CreateObject("replacement target").AddComponent<GameEntity>();
        replacement.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        replacement.gameObject.AddComponent<BoxCollider>().size = Vector3.one * 10f;
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(replacement);
        Assert.IsFalse(movement.IsMeleeCirclingPaused);
        Set(controller, "circlePaused", true);
        movement.EndMeleeMovement();
        Assert.IsFalse(movement.IsMeleeCirclingPaused);
    }

    private void CircleKeepsNavigationMovingAndRestoresBrakingWhenChaseEnds()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out _);
        MovementModule movement = EnableTestNavigation(attack, target);
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        agent.autoBraking = true;
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        Set(controller, "movementTarget", target);
        Set(controller, "sectorYaw", -90f);
        Set(controller, "circleDirection", 1f);
        Set(controller, "meleePhase", MeleeMovementPhase.Circle);
        Set(controller, "circleUntil", Time.time + 10f);
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsFalse(agent.autoBraking, "A continuous circle must not brake at each short waypoint.");
        Assert.That(agent.speed, Is.EqualTo(7f));
        Assert.IsTrue(agent.hasPath);
        Assert.IsFalse(agent.isStopped);
        Vector3 reached = Get<Vector3>(controller, "meleeDestination");
        Assert.IsTrue(agent.Warp(reached));
        movement.Stop();
        movement.MovementProfile.combatDestinationChangeThreshold = 50f;
        Set(controller, "nextMeleePlanAt", 0f);
        movement.UpdateMeleeMovement(target);
        Assert.IsTrue(agent.hasPath, "An arrived circle waypoint must be replaced even below the destination change threshold.");
        Assert.That(agent.speed, Is.EqualTo(7f));
        movement.EndMeleeMovement();
        Assert.IsTrue(agent.autoBraking, "The original locomotion setting must be restored.");
    }

    private void ArrivedRushOutsideSurfaceRangeReplacesStoppedPathAndClosesOnlyTheMissingDistance()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out MeleeAttackDefinition def);
        MovementModule movement = EnableTestNavigation(attack, target);
        Set(def, "maxRange", 1.5f);
        Set(def, "attackRange", 1f);
        Set(def, "attackRadius", 0.5f);
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        // The old planar endpoint puts the origin 1.35m horizontally from the
        // target, but this bone pose is 0.8m above its body: actual range > 1.5m.
        Assert.IsTrue(agent.Warp(movement.transform.position + Vector3.right * 1.15f));
        var definitions = Get<List<AttackModule.AttackData>>(attack, "attackDefinitions");
        Vector3 origin = definitions[0].attackPoint.position;
        origin.y = 2.8f;
        definitions[0].attackPoint.position = origin;
        Physics.SyncTransforms();
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        Set(controller, "movementTarget", target);
        Set(controller, "sectorYaw", -90f);
        Set(controller, "meleePhase", MeleeMovementPhase.Rush);
        Set(controller, "hasMeleeDestination", true);
        Set(controller, "meleeDestination", movement.transform.position);
        Set(controller, "nextMeleePlanAt", 0f);
        Assert.IsFalse(attack.CanStartAttack());
        Vector3 oldPosition = movement.transform.position;
        movement.UpdateMeleeMovement(target);
        Vector3 replacement = Get<Vector3>(controller, "meleeDestination");
        Assert.That(replacement.x - oldPosition.x, Is.InRange(0.1f, 0.5f));
        Assert.That(agent.speed, Is.EqualTo(10f));
        Assert.IsFalse(agent.isStopped, "A reset path must not be reused with zero speed.");
        Assert.IsTrue(agent.hasPath);
        Assert.IsTrue(agent.Warp(replacement));
        Physics.SyncTransforms();
        Set(attack, "attackRangeSamples", null);
        Assert.IsTrue(attack.CanStartAttack(), "The corrected endpoint must satisfy actual surface range.");
    }

    private void RushRejectsAnOutOfRangeGoalSampledBackToItsCurrentPosition()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out MeleeAttackDefinition def);
        MovementModule movement = EnableTestNavigation(attack, target);
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        Vector3 outsideFloor = new Vector3(5035f, 0f, 5000f);
        Assert.IsTrue(movement.TrySampleNavigationPosition(outsideFloor, 10f, out Vector3 edge));
        Assert.IsTrue(agent.Warp(edge));
        target.transform.position = new Vector3(edge.x + 6.1f, 1f, edge.z);
        Set(def, "maxRange", 0.5f);
        Set(def, "attackRange", 0.3f);
        Set(def, "attackRadius", 0.2f);
        Physics.SyncTransforms();
        // Even the first inward correction lands beyond this NavMesh edge;
        // sampling projects it back to the same reached position.
        Assert.IsTrue(movement.TrySampleNavigationPosition(edge + Vector3.right * 0.3f, 1f, out Vector3 projected));
        Vector3 projectedStep = projected - edge;
        projectedStep.y = 0f;
        Assert.That(projectedStep.magnitude, Is.LessThan(0.05f));
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        Set(controller, "movementTarget", target);
        Set(controller, "sectorYaw", -90f);
        Set(controller, "meleePhase", MeleeMovementPhase.Rush);
        Set(controller, "hasMeleeDestination", true);
        Set(controller, "meleeDestination", edge);
        Set(controller, "nextMeleePlanAt", 0f);
        Assert.IsFalse(attack.CanStartAttack());
        movement.UpdateMeleeMovement(target);
        Vector3 step = Get<Vector3>(controller, "meleeDestination") - edge;
        step.y = 0f;
        Assert.That(step.magnitude, Is.GreaterThan(0.1f),
            "A complete path ending at our current position must not suppress reachable alternative approaches.");
        Assert.IsTrue(agent.hasPath);
        Assert.That(agent.speed, Is.GreaterThan(0f));
    }

    private void RushSamplesLocalNavigationHeightWhenTargetRootIsAboveItsBody()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out MeleeAttackDefinition def);
        MovementModule movement = EnableTestNavigation(attack, target);
        target.transform.position += Vector3.up * 2.5f;
        target.GetComponent<BoxCollider>().center = Vector3.down * 2.5f;
        Set(def, "maxRange", 1.5f);
        Set(def, "attackRange", 1f);
        Set(def, "attackRadius", 0.5f);
        Physics.SyncTransforms();
        Assert.IsFalse(movement.TrySampleNavigationPosition(target.transform.position, 1f, out _),
            "The target's root height must be outside the local floor sampling radius in this scenario.");
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        movement.UpdateMeleeMovement(target);
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Rush));
        Assert.IsTrue(Get<bool>(controller, "hasMeleeDestination"),
            "Target root height must not prevent a reachable ground-level approach.");
        Assert.IsTrue(movement.GetComponent<NavMeshAgent>().hasPath);
        Assert.That(movement.GetComponent<NavMeshAgent>().speed, Is.GreaterThan(0f));
    }

    private void CombatSamplingFindsSlopeFloorBetweenBothEndpointHeights()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out _);
        MovementModule movement = EnableTestNavigation(attack, target, inclinedFloor: true);
        Assert.IsTrue(movement.TrySampleNavigationPosition(new Vector3(4985f, -5.5f, 5000f), 1f, out Vector3 start));
        NavMeshAgent agent = movement.GetComponent<NavMeshAgent>();
        Assert.IsTrue(agent.Warp(start));
        Physics.SyncTransforms();
        Vector3 proposed = new Vector3(5000f, 5.5f, 5000f);
        Assert.IsFalse(movement.TrySampleNavigationPosition(proposed, 1f, out _));
        Assert.IsFalse(movement.TrySampleNavigationPosition(
            new Vector3(proposed.x, agent.nextPosition.y, proposed.z), 1f, out _));
        object[] args = { proposed, 1f, Vector3.zero };
        bool found = (bool)typeof(MovementModule).GetMethod("TrySampleMeleeNavigationPosition",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(movement, args);
        Assert.IsTrue(found, "The middle of a slope must use its own floor height, not either endpoint height.");
        Vector3 point = (Vector3)args[2];
        Assert.That(Mathf.Abs(point.y), Is.LessThan(0.5f));
        Assert.IsTrue(movement.MoveToCompletePath(point));
        Assert.IsTrue(agent.hasPath);
        movement.Stop();
        Assert.IsFalse(Get<bool>(movement, "isPathValid"), "A reset path must not keep stale PathComplete debug status.");
    }

    [TestCase(1f)]
    [TestCase(-1f)]
    public void CirclingTurnsAtItsSectorBoundaryWithoutSwitchingFlankSides(float side)
    {
        AttackModule attack = CreateMeleeAttacker(out _, out _);
        MeleeCombatProfile profile = CreateAsset<MeleeCombatProfile>();
        profile.positioning = MeleePositioning.Flank;
        object controller = ConfigureCombatMovement(attack, profile);
        Set(controller, "flankSide", side);
        Set(controller, "circleDirection", side);
        float goal = (float)InvokeController(controller, "GetCirclingAngle", side * 137f, 3f, 6f, 0.3f);
        Assert.That(Mathf.Sign(goal), Is.EqualTo(side));
        Assert.That(Mathf.Abs(goal), Is.InRange(100f, 135f));
    }

    [Test]
    public void WideUnrestrictedOrbitKeepsItsDirectionEvenWhenAngularTravelIsSmall()
    {
        AttackModule attack = CreateMeleeAttacker(out _, out _);
        MeleeCombatProfile profile = CreateAsset<MeleeCombatProfile>();
        profile.positioning = MeleePositioning.Unrestricted;
        object controller = ConfigureCombatMovement(attack, profile);
        Set(controller, "circleDirection", 1f);
        float goal = (float)InvokeController(controller, "GetCirclingAngle", 0f, 3f, 50f, 0.3f);
        Assert.That(goal, Is.GreaterThan(0f), "Small travel must not be mistaken for a sector boundary.");
    }

    [TestCase(72f)]
    [TestCase(-72f)]
    public void BoundaryRecoveryGoalRemainsInsideSectorAfterStoppingTolerance(float initialAngle)
    {
        AttackModule attack = CreateMeleeAttacker(out _, out _);
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        const float radius = 6.4f, tolerance = 0.3f, sampleRadius = 1f;
        float margin = (float)controller.GetType().GetMethod("GetSectorMargin", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { radius, tolerance + sampleRadius });
        float goalAngle = (float)InvokeController(controller, "PreferredAngle", initialAngle, margin);
        float stoppedAngle = Mathf.Abs(goalAngle) + Mathf.Asin(tolerance / radius) * Mathf.Rad2Deg;
        Assert.That(stoppedAngle, Is.LessThan(70f), "Arrival tolerance must not leave the robot outside the Rush sector.");
        float movementDistance = 2f * radius * Mathf.Sin(Mathf.Abs(initialAngle - goalAngle) * Mathf.Deg2Rad * 0.5f);
        Assert.That(movementDistance, Is.GreaterThan(tolerance), "Recovery must issue a goal beyond the old arrived waypoint.");

        float arrivalMargin = (float)controller.GetType().GetMethod("GetSectorMargin", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { radius, tolerance });
        Assert.IsFalse((bool)InvokeController(controller, "IsInPreferredSector", 68f, -arrivalMargin),
            "A NavMesh sample near the boundary must be rejected even if nominally inside the sector.");
        Assert.IsTrue((bool)InvokeController(controller, "IsInPreferredSector", goalAngle, -arrivalMargin));
    }

    [Test]
    public void CompletePathsWithoutSectorProgressEventuallyAllowFallbackAndDiscardOldGoal()
    {
        AttackModule attack = CreateMeleeAttacker(out _, out _);
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        Set(controller, "hasMeleeDestination", true);
        InvokeController(controller, "UpdateSectorRecovery", 72f, 72f, true, 10f);
        InvokeController(controller, "UpdateSectorRecovery", 72f, 72f, true, 11f);
        Assert.IsFalse(Get<bool>(controller, "allowFallbackAttack"));
        InvokeController(controller, "UpdateSectorRecovery", 72f, 72f, true, 12.1f);
        Assert.IsTrue(Get<bool>(controller, "allowFallbackAttack"), "A complete path that never corrects the angle must not cause indefinite waiting.");
        Assert.IsFalse(Get<bool>(controller, "hasMeleeDestination"));
    }

    [Test]
    public void SectorProgressAndUnavailableCooldownDoNotTriggerPrematureFallback()
    {
        AttackModule attack = CreateMeleeAttacker(out _, out _);
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        InvokeController(controller, "UpdateSectorRecovery", 85f, 85f, true, 10f);
        InvokeController(controller, "UpdateSectorRecovery", 80f, 80f, true, 11.5f);
        InvokeController(controller, "UpdateSectorRecovery", 80f, 80f, true, 12.1f);
        Assert.IsFalse(Get<bool>(controller, "allowFallbackAttack"), "Angular progress must restart the blocked-sector grace.");
        InvokeController(controller, "UpdateSectorRecovery", 80f, 80f, false, 15f);
        InvokeController(controller, "UpdateSectorRecovery", 80f, 80f, true, 20f);
        Assert.IsFalse(Get<bool>(controller, "allowFallbackAttack"), "Waiting for cooldown must not consume the recovery grace.");
        attack.GetComponent<MovementModule>().ResetMeleeCombat();
        Assert.That(Get<float>(controller, "sectorRecoverySince"), Is.EqualTo(-1f));
    }

    [Test]
    public void IncreasingApproachSpacingMovesEndpointBackWithoutIncreasingDamageReach()
    {
        AttackModule attack = CreateMeleeAttacker(out _, out MeleeAttackDefinition def);
        Set(def, "attackRange", 1.2f);
        Set(def, "attackRadius", 1.5f);
        MethodInfo clearance = typeof(AttackModule).GetMethod("GetApproachClearance", BindingFlags.Instance | BindingFlags.NonPublic);
        float previous = (float)clearance.Invoke(attack, new object[] { 1f, 0.75f });
        float farther = (float)clearance.Invoke(attack, new object[] { 1f, 0.9f });
        Assert.That(farther - previous, Is.EqualTo(0.405f).Within(0.001f));
        Assert.That(farther, Is.LessThan(def.AttackRange + def.AttackRadius + 0.5f));
        Assert.That(def.MaxRange, Is.EqualTo(3f));
        Assert.That(def.AttackRange, Is.EqualTo(1.2f));
        Assert.IsTrue(attack.CanStartAttack(), "Spacing authoring must not change attack readiness reach.");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AttackRootMotionSuppressionRespectsCommitmentAndStopDuringAttack(bool stopDuringAttack)
    {
        AttackModule attack = CreateMeleeAttacker(out _, out MeleeAttackDefinition def);
        ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        MovementModule movement = attack.GetComponent<MovementModule>();
        Set(def, "stopDuringAttack", stopDuringAttack);
        Set(attack, "currentAttackDefinition", def);
        Assert.IsFalse(movement.SuppressRootMotionForAttack);
        MethodInfo committed = typeof(AttackModule).GetProperty(nameof(AttackModule.IsAttackCommitted)).GetSetMethod(true);
        committed.Invoke(attack, new object[] { true });
        movement.EndMeleeMovement();
        Assert.That(movement.SuppressRootMotionForAttack, Is.EqualTo(stopDuringAttack),
            "Ending chase must preserve translation suppression for a committed stopped attack.");
        committed.Invoke(attack, new object[] { false });
        Assert.IsFalse(movement.SuppressRootMotionForAttack, "Root motion must return when attack commitment ends.");
        committed.Invoke(attack, new object[] { true });
        Set(movement, "meleeCombatProfile", null);
        Assert.IsFalse(movement.SuppressRootMotionForAttack, "Existing unprofiled attack locomotion must stay supported.");
    }

    [Test]
    public void LargeTargetIsEligibleForMeleeAndRecoveryBlocksReadyCooldown()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out _);
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        Assert.IsTrue(attack.CanStartAttack(), "The center is 8m away but the attack point is 2.5m from the surface.");

        BeginWithdrawal(attack, target);
        Assert.IsFalse(attack.CanStartAttack(), "Cooldown readiness must not skip withdrawal.");
        MethodInfo phase = controller.GetType().GetMethod("SetMeleePhase", BindingFlags.Instance | BindingFlags.NonPublic);
        System.Type phaseType = phase.GetParameters()[0].ParameterType;
        phase.Invoke(controller, new[] { System.Enum.Parse(phaseType, "Circle") });
        Assert.IsFalse(attack.CanStartAttack(), "Cooldown readiness must not skip circling.");
        phase.Invoke(controller, new[] { System.Enum.Parse(phaseType, "Approach") });
        Assert.IsTrue(attack.CanStartAttack());
        phase.Invoke(controller, new[] { System.Enum.Parse(phaseType, "Rush") });
        Assert.IsTrue(attack.CanStartAttack(), "A rush must hand off to an attack as soon as strike conditions hold.");
    }

    [Test]
    public void RushStillRequiresSurfaceRangeReadyCooldownAndPreferredSector()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out MeleeAttackDefinition def);
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        Set(controller, "movementTarget", target);
        MethodInfo phase = controller.GetType().GetMethod("SetMeleePhase", BindingFlags.Instance | BindingFlags.NonPublic);
        phase.Invoke(controller, new[] { System.Enum.Parse(phase.GetParameters()[0].ParameterType, "Rush") });
        Assert.IsTrue(attack.CanStartAttack(), "The large target's surface is in range during a rush.");

        Set(attack, "cooldowns", new Dictionary<BaseAttackDefinition, float> { { def, 1f } });
        Assert.IsFalse(attack.CanStartAttack(), "Rushing must not bypass the cooldown.");
        Set(attack, "cooldowns", new Dictionary<BaseAttackDefinition, float> { { def, 0f } });
        target.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Assert.IsFalse(attack.CanStartAttack(), "A player turning away must still enforce the front preference.");
        target.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        Set(def, "maxRange", 1f);
        Assert.IsFalse(attack.CanStartAttack(), "Rushing must not grant extra reach.");
    }

    [Test]
    public void FrontPreferenceAndFlankOverrideGateMeleeAttacks()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out _);
        MeleeCombatProfile profile = CreateAsset<MeleeCombatProfile>();
        ConfigureCombatMovement(attack, profile);
        target.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Assert.IsFalse(attack.CanStartAttack(), "The attacker is behind the target.");
        profile.positioning = MeleePositioning.Unrestricted;
        Assert.IsTrue(attack.CanStartAttack());
        profile.positioning = MeleePositioning.Flank;
        target.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
        Assert.IsTrue(attack.CanStartAttack(), "A configured flanker may attack at 120 degrees.");
    }

    [Test]
    public void RangedAttackSelectionKeepsCenterDistanceAndIgnoresMeleeRecovery()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out _);
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        RangedAttackDefinition ranged = CreateAsset<RangedAttackDefinition>();
        Set(ranged, "maxRange", 3f);
        Set(ranged, "distanceUtilityCurve", AnimationCurve.Constant(0f, 1f, 1f));
        Set(attack, "attackDefinitions", new List<AttackModule.AttackData> { new() { attackDefinition = ranged } });
        Set(attack, "cooldowns", new Dictionary<BaseAttackDefinition, float> { { ranged, 0f } });
        Assert.IsFalse(attack.CanStartAttack(), "Ranged attacks still measure the target's center.");
        Set(ranged, "maxRange", 9f);
        BeginWithdrawal(attack, target);
        Assert.IsTrue(attack.CanStartAttack(), "The melee movement phase must not block ranged attacks.");
    }

    [Test]
    public void RepositionSpeedsAreIndependentAndZeroFallsBackToWalk()
    {
        MovementProfile profile = CreateAsset<MovementProfile>();
        profile.walkMovementSpeed = 1.5f;
        Assert.That(profile.GetCombatWithdrawalSpeed(), Is.EqualTo(1.5f));
        Assert.That(profile.GetCombatCirclingSpeed(), Is.EqualTo(1.5f));
        profile.combatWithdrawalSpeed = 4.5f;
        profile.combatCirclingSpeed = 3.5f;
        Assert.That(profile.GetCombatWithdrawalSpeed(), Is.EqualTo(4.5f));
        Assert.That(profile.GetCombatCirclingSpeed(), Is.EqualTo(3.5f));
    }

    [Test]
    public void MovementResetClearsRecoveryAndFallbackPermission()
    {
        AttackModule attack = CreateMeleeAttacker(out GameEntity target, out _);
        object controller = ConfigureCombatMovement(attack, CreateAsset<MeleeCombatProfile>());
        BeginWithdrawal(attack, target);
        Set(controller, "allowFallbackAttack", true);
        Assert.IsFalse(attack.CanStartAttack(), "Recovery must prevent attacking even with a fallback sector.");
        MovementModule movement = attack.GetComponent<MovementModule>();
        movement.ResetMeleeCombat();
        Assert.That(movement.MeleePhase, Is.EqualTo(MeleeMovementPhase.Approach));
        Assert.IsTrue(attack.CanStartAttack(), "A reset must clear the old target's recovery phase.");
        target.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Assert.IsFalse(attack.CanStartAttack(), "A reset must clear permission to attack from a fallback sector.");
    }

    [Test]
    public void AnimationEventsRequireMatchingCommitmentButPreserveMultiHitCombos()
    {
        AttackModule attack = CreateMeleeAttacker(out _, out MeleeAttackDefinition def);
        int performed = 0;
        attack.OnAttackPerformed += _ => performed++;
        attack.PerformAttack(def);
        Assert.That(performed, Is.Zero, "An animation event outside an attack must not execute.");

        Set(attack, "currentAttackDefinition", def);
        typeof(AttackModule).GetProperty(nameof(AttackModule.IsAttackCommitted)).GetSetMethod(true)
            .Invoke(attack, new object[] { true });
        attack.PerformAttack(null);
        attack.PerformAttack(CreateAsset<MeleeAttackDefinition>());
        Assert.That(performed, Is.Zero, "A different/outgoing clip must not execute the active attack.");
        attack.PerformAttack(def);
        attack.PerformAttack(def);
        Assert.That(performed, Is.EqualTo(2), "Distinct events inside a valid combo must remain supported.");
        typeof(AttackModule).GetProperty(nameof(AttackModule.IsAttackCommitted)).GetSetMethod(true)
            .Invoke(attack, new object[] { false });
        attack.PerformAttack(def);
        Assert.That(performed, Is.EqualTo(2), "Late events after recovery starts must not execute.");
    }

    [Test]
    public void TripixAttackUsesActualClipDurationAndCannotExitWhileAttackCommandIsActive()
    {
        MeleeAttackDefinition def = AssetDatabase.LoadAssetAtPath<MeleeAttackDefinition>(
            "Assets/Project/Systems/AI System/Data/Attacks/SO_AttackDefinition_Swarmbot_LightAttack.asset");
        AnimationClip clip = new SerializedObject(def).FindProperty("animationClip").objectReferenceValue as AnimationClip;
        Assert.IsNotNull(clip);
        Assert.That(def.AttackDuration, Is.EqualTo(clip.length));
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            "Assets/Project/Art/Characters/Elythian Bots/Swarmbot/AnimController_Entity_Swarmbot.controller");
        AnimatorState attackState = null;
        foreach (AnimatorControllerLayer layer in controller.layers)
            attackState = attackState ?? FindState(layer.stateMachine, "Jump Attack");
        Assert.IsNotNull(attackState);
        Assert.That(attackState.transitions.Length, Is.GreaterThan(0));
        foreach (AnimatorStateTransition transition in attackState.transitions)
        {
            bool requiresCommandEnd = false;
            foreach (AnimatorCondition condition in transition.conditions)
                requiresCommandEnd |= condition.parameter == "IsAttacking" && condition.mode == AnimatorConditionMode.IfNot;
            Assert.IsTrue(requiresCommandEnd, "Early exit while IsAttacking is true allows Alarmed -> Attack re-entry.");
        }
    }

    private static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        foreach (ChildAnimatorState state in machine.states)
            if (state.state.name == name) return state.state;
        foreach (ChildAnimatorStateMachine child in machine.stateMachines)
        {
            AnimatorState state = FindState(child.stateMachine, name);
            if (state) return state;
        }
        return null;
    }

    [Test]
    public void TripixPrefabOptsIntoItsOwnFrontCombatProfile()
    {
        const string root = "Assets/Project/Systems/AI System/";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefabs/Entities/P_Entity_Tripix.prefab");
        Assert.IsNotNull(prefab);
        MovementModule movement = prefab.GetComponent<MovementModule>();
        var serialized = new SerializedObject(movement);
        var profile = serialized.FindProperty("meleeCombatProfile").objectReferenceValue as MeleeCombatProfile;
        Assert.IsNull(new SerializedObject(prefab.GetComponent<AttackModule>()).FindProperty("meleeCombatProfile"),
            "Combat movement authoring must belong to MovementModule.");
        Assert.IsNotNull(movement.MovementProfile);
        Assert.That(movement.MovementProfile.runMovementSpeed, Is.EqualTo(9f));
        Assert.That(movement.MovementProfile.combatWithdrawalSpeed, Is.EqualTo(4.5f));
        Assert.That(movement.MovementProfile.combatCirclingSpeed, Is.EqualTo(3.5f));
        Assert.That(movement.MovementProfile.combatApproachRangeFraction, Is.EqualTo(0.9f));
        Assert.That(movement.MovementProfile.combatCircleDegreesPerSecond, Is.EqualTo(35f));
        Assert.That(movement.MovementProfile.combatReplanInterval, Is.EqualTo(0.35f));
        Assert.That(movement.MovementProfile.combatWaypointTolerance, Is.EqualTo(0.3f));
        Assert.IsNotNull(profile);
        Assert.That(profile.positioning, Is.EqualTo(MeleePositioning.Front));
        Assert.That(profile.holdingDistance, Is.EqualTo(6f));
        Assert.That(profile.circleDuration, Is.EqualTo(2.5f));
    }
}
