using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Chalk;

[HarmonyPatch]
public class ExtraMines
{
    private static readonly MethodInfo ExplodeMethod = AccessTools.Method(typeof(Landmine), "ServerExplode", []);

    public static bool seeded = true;

    public static IEnumerator Start()
    {
        yield return null;

        if (Chalk.minedLootboxes.Value) SpawnLootBoxMines();
        if (Chalk.holeMinefield.Value) SpawnHoleMinefield();

        seeded = true;
    }

    private static void SpawnLootBoxMines()
    {
        foreach (var spawner in UnityEngine.Object.FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None))
        {
            if (UnityEngine.Random.value > .65f) continue;

            Chalk.SpawnServerMine(spawner.transform.position + Vector3.up * .25f);
        }
    }

    private static void SpawnHoleMinefield()
    {
        if (!GolfHoleManager.HasInstance || GolfHoleManager.MainHole == null) return;

        Vector3 flagPos = GolfHoleManager.MainHole.transform.position;
        const float r = 1.5f;

        Vector3[] offsets = [
            new(0, 0, r),
            new(0, 0, -r),
            new(r, 0, 0),
            new(-r, 0, 0)
        ];

        foreach (var off in offsets) Chalk.SpawnServerMine(flagPos + off + new Vector3(0f, .1f, 0f));
    }

    [HarmonyPatch(typeof(Landmine), "OnCollisionEnter")]
    [HarmonyPostfix]
    private static void MineChain(Landmine __instance, Collision collision)
    {
        if (!__instance.isServer || !__instance.IsArmed) return;

        if (!collision.collider.TryGetComponent(out Landmine otherMine)) return;
        if (otherMine == null || !otherMine.IsArmed) return;

        ExplodeMethod.Invoke(__instance, null);
        ExplodeMethod.Invoke(otherMine, null);
    }
}