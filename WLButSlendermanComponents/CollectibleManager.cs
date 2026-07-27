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
                if (FakePlugin.heartBeatSource == null)
                {
                    FakePlugin.heartBeatSource = FMODAudioSource.AddTo(GameInstance.Instance.GetFirstLocalPlayerController().GetGameplayCamera().gameObject);
                    FakePlugin.heartBeatSource.loop = true;
                    FakePlugin.heartBeatSource.clip = Enemy.heartBeatClip;
                    FakePlugin.heartBeatSource.volume = 0.25f;
                }

                FakePlugin.heartBeatSource.PlayOneShot(FakePlugin.uhh);

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
