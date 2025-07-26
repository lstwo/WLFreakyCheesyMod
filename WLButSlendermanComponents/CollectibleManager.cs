using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace WLButSlenderman;

public static class CollectibleManager
{
    private static int collectedPfps = 0;
    public static int totalPfps = 0;
    public static List<uint> collectedPfpsIds = new();
    
    public static int CollectedPfps
    {
        get => collectedPfps;
        set
        {
            collectedPfps = value;
		    
            if (collectedPfps >= totalPfps)
            {
                Process.Start($"{Application.streamingAssetsPath}/end.wav");
                Application.Quit();
            }
        }
    }
}