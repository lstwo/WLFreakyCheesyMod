using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace WLButSlenderman;

public class FakePlugin
{
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
        
        if (chasingVolume.weight == 1f && !b)
        {
            chasingVolume.weight = 0f;
            var layer = Object.FindObjectOfType<GameplayCamera>().GetComponent<PostProcessLayer>();
            layer.enabled = false;
            layer.enabled = true;
        }
    }
}