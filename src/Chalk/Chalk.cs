using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Chalk.utils;
using HarmonyLib;
using UnityEngine;

namespace Chalk;

[BepInAutoPlugin]
public partial class Chalk : BaseUnityPlugin
{
    internal static ConfigEntry<bool> windBursts = null!;
    internal static ConfigEntry<bool> minedLootboxes = null!;
    internal static ConfigEntry<bool> holeMinefield = null!;
    internal static ConfigEntry<bool> ballSwap = null!;
    internal static ConfigEntry<bool> instaKills = null!;
    internal static ConfigEntry<bool> extraLoots = null!;
    internal static ConfigEntry<bool> airHornExtra = null!;
    internal static ConfigEntry<bool> mineChain = null!;
    internal static ConfigEntry<bool> mineFlashing = null!;
    internal static ConfigEntry<bool> holeBlocker = null!;

    internal static ManualLogSource Log = null!;

    private void Awake()
    {
        Harmony harmony = new(Info.Metadata.GUID);

        windBursts = Config.Bind("Chalk", "Wind Bursts", true, "Toggles random bursts of wind mid match");
        minedLootboxes = Config.Bind("Chalk", "Mined Lootboxes", true, "Some loot boxes spawn with a land mine below");
        holeMinefield = Config.Bind("Chalk", "Hole Minefield", true, "Places 4 mines near the hole");
        ballSwap = Config.Bind("Chalk", "Ball Swap", false, "Swaps players' balls at the start of a match");
        instaKills = Config.Bind("Chalk", "Instant Kills", true, "Throwing a ball out of bounds makes you explode");
        extraLoots = Config.Bind("Chalk", "Extra Loot", true, "Knocking out players gives you loot");
        airHornExtra = Config.Bind("Chalk", "Extra Air Horn interactions", true, "Some extra air horn interactions :)");
        mineChain = Config.Bind("Chalk", "Mine Chain", true, "An activated landmine will blow up if it collides with another activated landmine");
        mineFlashing = Config.Bind("Chalk", "Mine Flashing", true, "Mines can be detonated with a Flash Camera");
        holeBlocker = Config.Bind("Chalk", "Hole Blocker", true, "Places a mine on the hole to block it for the first 15 seconds!");

        harmony.PatchAll();
        CourseManager.MatchStateChanged += OnMatchStateChanged;
        WindBursts.Start();
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
            StartCoroutine(ExtraMines.Start());

            if (ballSwap.Value && UnityEngine.Random.value > .5f) StartCoroutine(BallPatches.SwapBalls());
            StartCoroutine(BallPatches.HoleBlocker());
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

        PlayerInventory? inv = FakeInventory.Get();
        if (inv == null) return;

        ItemUseId useId = new(0uL, 1, ItemType.Landmine, false);

        CourseManager.ServerSpawnLandmine(pos, Quaternion.identity, Vector3.zero, Vector3.zero, LandmineArmType.Planted, useId, inv);
    }
}