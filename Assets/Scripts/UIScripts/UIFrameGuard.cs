using UnityEngine;

public static class UIFrameGuard
{
    public static bool ConsumedPointerDownThisFrame { get; set; }

    public static void LateClear() => ConsumedPointerDownThisFrame = false;
}

