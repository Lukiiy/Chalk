using UnityEngine;

namespace Chalk.utils;

public class FakeInventory
{
    private static PlayerInventory? instance;

    public static PlayerInventory? Get()
    {
        if (instance != null) return instance;

        var real = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
        if (real == null) return null;

        GameObject fake = UnityEngine.Object.Instantiate(real.gameObject);

        fake.name = "ChalkInventory";
        fake.hideFlags = HideFlags.HideAndDontSave;

        // Disable renderers & colliders?????????????????
        foreach (var renderer in fake.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (var c in fake.GetComponentsInChildren<Collider>(true)) c.enabled = false;

        UnityEngine.Object.DontDestroyOnLoad(fake);

        instance = fake.GetComponent<PlayerInventory>();

        return instance;
    }
}