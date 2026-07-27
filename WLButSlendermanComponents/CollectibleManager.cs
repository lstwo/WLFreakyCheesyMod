using System.Diagnostics;
using BepInEx;
using HawkNetworking;
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
            
            if (collectedPfps is 10 or 30)
            {
                //FakePlugin.SpawnNewEnemy();
            }

            if (collectedPfps == 20)
            {
                FMODAudio.PlayOneShot(FakePlugin.uhh, FakePlugin.uhhVolume);

                /*if (HawkNetworkManager.DefaultInstance.IsOffline() || HawkNetworkManager.DefaultInstance.GetMe().IsHost)
                {
                    foreach (var player in GameInstance.Instance.GetPlayerControllers())
                    {
                        FakePlugin.startCoroutine(FakePlugin.TeleportRoutine(player));
                    }
                }*/
            }
        }
    }
}
