using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Video;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace WLButSlenderman;

public class FakePlugin
{
    public static Action SpawnNewEnemy;
    
    public static AssetBundle bundle;
    public static Texture2D freakyCheesyTex;
    public static AudioClip freakyCheesyClip;
    public static AudioClip uhh;

    public static GameObject enemyPrefab;
    public static GameObject collectiblePrefab;
    public static GameObject enemyBulletPrefab;
    public static PostProcessProfile chasingPostProcessing;
    
    public static PostProcessVolume chasingVolume;
    public static AudioSource heartBeatSource;

    public static AudioClip shootSound;
    
    public static Dictionary<PlayerController, PlayerRevive> playerRevives = new();
    public static int collectiblesCount = 40;

    public static VideoPlayer jumpscare;

    public static Action<IEnumerator> startCoroutine;
    
    public static void StartCoroutine(IEnumerator routine)
    {
        startCoroutine?.Invoke(routine);
    }

    public static void ToggleEffects(bool b)
    {
        if (chasingVolume.weight == 0 && b)
        {
            chasingVolume.weight = 1f;
            var layer = Object.FindObjectOfType<GameplayCamera>().GetComponent<PostProcessLayer>();
            layer.enabled = false;
            layer.enabled = true;
        }
        
        if (chasingVolume.weight == 1 && !b)
        {
            chasingVolume.weight = 0f;
            var layer = Object.FindObjectOfType<GameplayCamera>().GetComponent<PostProcessLayer>();
            layer.enabled = false;
            layer.enabled = true;
        }
    }

    public static IEnumerator TeleportRoutine(PlayerController player)
    {
        while (GameInstance.Instance.GetGamemode())
        {
            yield return new WaitForSeconds(Random.Range(30f, 300f));

            if (!player.GetPlayerCharacter())
            {
                continue;
            }

            if (player.GetPlayerCharacter().HasEnteredAction())
            {
                if (player.GetPlayerControllerInteractor().GetEnteredAction() is ActionEnterExitInteract
                    actionEnterExitInteract)
                    actionEnterExitInteract.EvacuatePlayer(player, false);
                else
                    player.GetPlayerControllerInteractor().GetEnteredAction().RequestExit(player);
            }

            Vector3? pos = null;

            for (var i = 0; i < 1000; i++)
            {
                var checkPos = new Vector3(Random.Range(-1000f, 1000f), 1000, Random.Range(-1000f, 1000f));

                if (!Physics.Raycast(checkPos, Vector3.down, out var hit, 5000f) ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
                {
                    continue;
                }

                pos = new Vector3(checkPos.x, hit.point.y + 5f, checkPos.z);
                break;
            }

            if (pos.HasValue)
            {
                player.GetPlayerCharacter().SetPlayerPosition(pos.Value);
            }
        }
    }
}