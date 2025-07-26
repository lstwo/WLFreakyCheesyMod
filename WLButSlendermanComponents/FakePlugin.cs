using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace WLButSlenderman;

public class FakePlugin
{
    public static AssetBundle bundle;
    public static Texture2D freakyCheesyTex;
    public static AudioClip freakyCheesyClip;

    public static GameObject enemyPrefab;
    public static GameObject collectiblePrefab;
    
    public static ManualLogSource Logger;
    
    public static Dictionary<PlayerCharacter, PlayerRevive> playerRevives = new();
    public static string[] texFileNames = ["freakyelephant", "freakycae", "freakygabtinte", "freakydojo", "freakyday", "freakyelephant", "freakycae", "freakygabtinte", "freakydojo", "freakyday"];

    public static Action<IEnumerator> startCoroutine;
    
    public static void StartCoroutine(IEnumerator routine)
    {
        startCoroutine?.Invoke(routine);
    }
}