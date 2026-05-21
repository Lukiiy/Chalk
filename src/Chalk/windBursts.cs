using HarmonyLib;
using UnityEngine;

namespace Chalk;

[HarmonyPatch]
public static class WindBursts
{
    public static bool toggled = true;

    private static float next; // time
    private static float end; // time
    private static int stored;

    public static void Start() => next = Time.time + UnityEngine.Random.Range(15f, 45f);

    public static void Update()
    {
        if (!toggled) return;

        var w = WindManager.Instance;
        if (w == null || !w.isServer || !Plugin.IsMatchActive()) return;

        float t = Time.time;

        if (end == 0f && t >= next)
        {
            stored = WindManager.CurrentWindSpeed;
            w.NetworkcurrentWindSpeed = Mathf.Min(WindManager.MaxPossibleWindSpeed, stored * (int) UnityEngine.Random.Range(3.5f, 5f));
            end = t + 10f;
        }
        else if (end != 0f && t >= end)
        {
            w.NetworkcurrentWindSpeed = stored;
            end = 0f;
            next = t + UnityEngine.Random.Range(15f, 45f);
        }
    }
}