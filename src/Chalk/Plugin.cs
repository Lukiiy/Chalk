using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Chalk;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    private bool _seededMatch;

    private void Awake()
    {
        Harmony harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll();

        WindBursts.Start();
        CourseManager.MatchStateChanged += OnMatchStateChanged;
    }

    private void Update()
    {
        WindBursts.Update();
    }

    private void OnDestroy()
    {
        CourseManager.MatchStateChanged -= OnMatchStateChanged;
    }

    private void OnMatchStateChanged(MatchState previous, MatchState current)
    {
        if (!CourseManager.HasInstance || !CourseManager.Instance.isServer) return;

        if (current == MatchState.TeeOff)
        {
            StartCoroutine(ExtraMines.SeedMinesAfterOneFrame());
        }

        if (current == MatchState.Ended || current == MatchState.Initializing)
        {
            ExtraMines.seeded = false;
        }
    }

    public static bool IsMatchActive() => CourseManager.MatchState is MatchState.TeeOff or MatchState.Ongoing or MatchState.CountingDownToEnd or MatchState.Overtime;

    public static void SpawnServerMine(Vector3 pos)
    {
        if (!CourseManager.HasInstance || !CourseManager.Instance.isServer) return;

        PlayerInfo[] players = FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        PlayerInfo owner = players[UnityEngine.Random.Range(0, players.Length)]; // random owner
        ItemUseId useId = new(0uL, 1, ItemType.Landmine);

        CourseManager.ServerSpawnLandmine(pos, Quaternion.identity, Vector3.zero, Vector3.zero, LandmineArmType.Planted, useId, owner.Inventory);
    }
}