using UnityEngine;

public static class SelectedBeatMap
{
    public static BeatMap Current { get; private set; }

    public static void Set(BeatMap map)
    {
        Current = map;
    }

    public static bool HasSelection => Current != null;

    public static void Clear()
    {
        Current = null;
    }
}

