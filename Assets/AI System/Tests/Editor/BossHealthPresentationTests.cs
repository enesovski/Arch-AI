using System.Collections.Generic;
using System.Reflection;
using Artifika.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class BossHealthPresentationTests
{
    private readonly List<Object> created = new();
    private HealthBarManager previousManager;
    private HealthBarManager manager;
    private BossHealthBar bar;
    private GameObject panel;
    private Michsky.UI.Reach.ProgressBar progress;

    private GameObject CreateObject(string name)
    {
        var obj = new GameObject(name);
        obj.SetActive(false);
        created.Add(obj);
        return obj;
    }

    private static void Set(object target, string name, object value)
    {
        for (System.Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) continue;
            field.SetValue(target, value);
            return;
        }
        Assert.Fail("Missing field " + name);
    }

    private static void SetManager(HealthBarManager value) =>
        typeof(HealthBarManager).GetProperty("Instance").SetValue(null, value);

    private static void Refresh(BossHealthComponent boss, Transform player) =>
        typeof(BossHealthComponent).GetMethod("RefreshProximityPresentation", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(boss, new object[] { player });

    [SetUp]
    public void SetUp()
    {
        previousManager = HealthBarManager.Instance;
        manager = CreateObject("test health bar manager").AddComponent<HealthBarManager>();
        SetManager(manager);
        GameObject barObject = CreateObject("test boss bar");
        progress = barObject.AddComponent<Michsky.UI.Reach.ProgressBar>();
        var delayedProgress = barObject.AddComponent<DelayableProgressBar>();
        Set(delayedProgress, "progressBar", progress);
        bar = barObject.AddComponent<BossHealthBar>();
        panel = CreateObject("boss bar panel");
        var text = CreateObject("boss name").AddComponent<TextMeshProUGUI>();
        Set(bar, "progressBar", delayedProgress);
        Set(bar, "healthBarParent", panel);
        Set(bar, "nameText", text);
        manager.bossHealthBar = bar;
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = created.Count - 1; i >= 0; i--)
            if (created[i]) Object.DestroyImmediate(created[i]);
        created.Clear();
        SetManager(previousManager);
    }

    [Test]
    public void ProximityUsesDifferentEnterAndExitDistancesAndClearsOnDespawn()
    {
        BossHealthComponent boss = CreateObject("boss").AddComponent<BossHealthComponent>();
        Transform player = CreateObject("local player").transform;
        player.position = Vector3.forward * 41f;
        Refresh(boss, player);
        Assert.IsFalse(panel.activeSelf);

        player.position = Vector3.forward * 39f;
        Refresh(boss, player);
        Assert.AreEqual(boss, bar.DisplayedBoss);
        Assert.IsTrue(panel.activeSelf);

        player.position = Vector3.forward * 44f;
        Refresh(boss, player);
        Assert.IsTrue(panel.activeSelf, "Leaving the entry radius must not flicker the bar.");
        player.position = Vector3.forward * 46f;
        Refresh(boss, player);
        Assert.IsFalse(panel.activeSelf);
        Assert.IsNull(bar.DisplayedBoss);

        player.position = Vector3.zero;
        Refresh(boss, player);
        Refresh(boss, null);
        Assert.IsFalse(panel.activeSelf, "Losing the local player must clear its presentation.");
        Refresh(boss, player);
        boss.OnNetworkDespawn();
        Assert.IsFalse(panel.activeSelf);
        Assert.IsNull(bar.DisplayedBoss);
    }

    [Test]
    public void ShowingBeforeHudExistsCanRetryAfterHudArrives()
    {
        BossHealthComponent boss = CreateObject("boss").AddComponent<BossHealthComponent>();
        SetManager(null);
        boss.ShowBossHealthBar();
        SetManager(manager);
        boss.ShowBossHealthBar();
        Assert.AreEqual(boss, bar.DisplayedBoss);
        Assert.IsTrue(panel.activeSelf);
    }

    [Test]
    public void NearbyBossCannotStealUpdateOrHideAnotherBossBar()
    {
        BossHealthComponent first = CreateObject("first boss").AddComponent<BossHealthComponent>();
        BossHealthComponent second = CreateObject("arena boss").AddComponent<BossHealthComponent>();
        Transform player = CreateObject("local player").transform;
        first.ShowBossHealthBar();
        second.ShowBossHealthBar(); // Explicit arena presentation may replace a proximity bar.
        progress.SetValue(37f);

        Refresh(first, player);
        typeof(BossHealthComponent).GetMethod("UpdateHealthBar", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(first, null);
        first.HideBossHealthBar();
        Assert.AreEqual(second, bar.DisplayedBoss);
        Assert.AreEqual(37f, progress.currentValue);
        Assert.IsTrue(panel.activeSelf);

        second.HideBossHealthBar();
        Refresh(first, player);
        Assert.AreEqual(first, bar.DisplayedBoss, "A nearby boss may show once the HUD is free.");
    }
}
