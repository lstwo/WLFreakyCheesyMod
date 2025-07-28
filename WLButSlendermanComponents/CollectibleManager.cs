using System.Diagnostics;
using UnityEngine;

namespace WLButSlenderman;

public static class CollectibleManager
{
    private static int collectedPfps;
    public static int totalPfps => FakePlugin.collectiblesCount;
    
    public static int CollectedPfps
    {
        get => collectedPfps;
        set
        {
            collectedPfps = value;

            if (collectedPfps == (int)totalPfps / 2)
            {
                if(FakePlugin.heartBeatSource != null)
                    FakePlugin.heartBeatSource.PlayOneShot(FakePlugin.uhh);
            }
		    
            if (collectedPfps >= totalPfps)
            {
                Process.Start($"{Application.streamingAssetsPath}/end.wav");
                //Application.Quit();
            }
        }
    }
}
