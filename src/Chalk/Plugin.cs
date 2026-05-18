using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Chalk;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    private const bool EnableWindBursts = true;
    private const bool EnableWaterExplode = true;
    private const bool EnableKnockoutLoot = true;
    private const bool EnableMinedLootBoxes = true;
    private const bool EnableHoleMinefield = true;
    private const bool EnableMatchSpeedBurst = true;

    private float _nextWindBurst;
    private float _windBurstEnd;
    private bool _windBurstActive;
    private int _savedWindSpeed;
    private bool _seededMatch;

    private void Awake()
    {
        Harmony harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll();

        _nextWindBurst = Time.time + UnityEngine.Random.Range(15f, 45f);
        CourseManager.MatchStateChanged += OnMatchStateChanged;
    }

    private void Update()
    {
        if (!EnableWindBursts) return;

        var wind = WindManager.Instance;
        if (wind == null || !wind.isServer || !IsMatchActive()) return;

        float now = Time.time;

        if (!_windBurstActive && now >= _nextWindBurst)
        {
            _savedWindSpeed = WindManager.CurrentWindSpeed;

            int burstSpeed = Mathf.Min(WindManager.MaxPossibleWindSpeed, Mathf.RoundToInt(_savedWindSpeed * 4f));

            wind.NetworkcurrentWindSpeed = burstSpeed;

            _windBurstEnd = now + 10f;
            _windBurstActive = true;
            return;
        }

        if (_windBurstActive && now >= _windBurstEnd)
        {
            wind.NetworkcurrentWindSpeed = _savedWindSpeed;

            _nextWindBurst = now + UnityEngine.Random.Range(15f, 45f); // delay
            _windBurstActive = false;
        }
    }

    private void OnDestroy()
    {
        CourseManager.MatchStateChanged -= OnMatchStateChanged;
    }

    private void OnMatchStateChanged(MatchState previous, MatchState current)
    {
        if (!CourseManager.HasInstance || !CourseManager.Instance.isServer) return;

        if (current == MatchState.TeeOff && !_seededMatch)
        {
            _seededMatch = true;

            StartCoroutine(SeedMinesAfterOneFrame());
        }

        if (current == MatchState.Ended || current == MatchState.Initializing)
        {
            _seededMatch = false;
        }
    }

    private IEnumerator SeedMinesAfterOneFrame()
    {
        yield return null;

        if (EnableMinedLootBoxes) SpawnLootBoxMines();
        if (EnableHoleMinefield) SpawnHoleMinefield();
    }

    private void SpawnLootBoxMines() // TODO
    {
        foreach (var spawner in FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None))
        {
            if (UnityEngine.Random.value > .5f) continue;

            Vector3 pos = spawner.transform.position + Vector3.up * .25f;

            PlayerInfo[] players = FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None);
            if (players.Length == 0) return;

            PlayerInfo owner = players[UnityEngine.Random.Range(0, players.Length)]; // random owner
            ItemUseId useId = new ItemUseId(0uL, 1, ItemType.Landmine);

            CourseManager.ServerSpawnLandmine(pos, Quaternion.identity, Vector3.zero, Vector3.zero, LandmineArmType.Planted, useId, owner.Inventory);
        }
    }

    private void SpawnHoleMinefield()
    {
        if (!GolfHoleManager.HasInstance || GolfHoleManager.MainHole == null) return;

        Vector3 flagPos = GolfHoleManager.MainHole.transform.position;
        const float r = 1.5f;

        Vector3[] offsets = {
            new(0, 0, r),
            new(0, 0, -r),
            new(r, 0, 0),
            new(-r, 0, 0)
        };

        foreach (var off in offsets) // TODO
        {
            PlayerInfo[] players = FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None);
            if (players.Length == 0) return;

            PlayerInfo owner = players[UnityEngine.Random.Range(0, players.Length)]; // random owner
            ItemUseId useId = new ItemUseId(0uL, 1, ItemType.Landmine);

            CourseManager.ServerSpawnLandmine(flagPos + off + new Vector3(0f, .1f, 0f), Quaternion.identity, Vector3.zero, Vector3.zero, LandmineArmType.Planted, useId, owner.Inventory);
        }
    }

    private static bool IsMatchActive() => CourseManager.MatchState is MatchState.TeeOff or MatchState.Ongoing or MatchState.CountingDownToEnd or MatchState.Overtime;
}

[HarmonyPatch]
internal static class Chaos
{
    private const bool EnableWaterExplode = true;
    private const bool EnableKnockoutLoot = true;

    [HarmonyPatch(typeof(GolfBall), "ServerReturnToBounds")]
    [HarmonyPostfix]
    private static void ballReturnToBounds(GolfBall __instance)
    {
        if (!EnableWaterExplode || __instance == null || !__instance.isServer) return;

        LevelBoundsTracker tracker = __instance.AsEntity.LevelBoundsTracker;

        bool inWater = (tracker.AuthoritativeBoundsState.HasState(BoundsState.InMainOutOfBoundsHazard) && (MainOutOfBoundsHazard.Type == OutOfBoundsHazard.Water || MainOutOfBoundsHazard.Type == OutOfBoundsHazard.Fog)) || (tracker.AuthoritativeBoundsState.HasState(BoundsState.InSecondaryOutOfBoundsHazard) && tracker.CurrentSecondaryHazardLocalOnly != null && tracker.CurrentSecondaryHazardLocalOnly.Type == OutOfBoundsHazard.Water);
        if (!inWater) return;

        var playerInfo = __instance.Networkowner?.PlayerInfo;
        if (playerInfo == null) return;

        VfxManager.ServerPlayPooledVfxForAllClients(VfxType.MineExplosion, playerInfo.transform.position, Quaternion.identity);
        playerInfo.AsGolfer.ServerEliminate(EliminationReason.OutOfBounds);
    }

    [HarmonyPatch(typeof(CourseManager), "InformPlayerKnockedOutInternal")]
    [HarmonyPostfix]
    private static void knockedOutByPlayer(PlayerMovement knockedOutPlayer, PlayerInfo responsiblePlayer, KnockoutType knockoutType, ref bool knockoutCounted)
    {
        if (!EnableKnockoutLoot || !knockoutCounted || responsiblePlayer == null || knockedOutPlayer == null) return;

        ItemType[] possible = [ItemType.Coffee, ItemType.SpringBoots, ItemType.GolfCart, ItemType.Airhorn];
        ItemType randomItem = possible[UnityEngine.Random.Range(0, possible.Length)];

        if (GameManager.AllItems.TryGetItemData(randomItem, out var itemData))
        {
            if (responsiblePlayer.Inventory.HasSpaceForItem(out _))
            {
                responsiblePlayer.Inventory.ServerTryAddItem(randomItem, itemData.MaxUses);
                responsiblePlayer.RpcPopUp(PlayerTextPopupType.Comeback, 0);
            }
        }
    }
}