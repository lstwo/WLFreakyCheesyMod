using System.Reflection;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using HarmonyLib;
using Rewired;
using Rewired.Utils;
using UnityEngine.Localization;

namespace WLButSlenderman;

public static class Patches
{
    [HarmonyPatch(typeof(VCA), "setVolume")]
    [HarmonyPrefix]
    public static bool CreateOptionsPage_Internal(ref VCA __instance, ref float volume)
    {
        if (__instance.getPath(out var path) == RESULT.OK && path == "vca:/Music")
        {
            volume = 0f;
        }

        return true;
    }
    
    [HarmonyPatch(typeof(PlayerNPCController), "OnEnable")]
    [HarmonyPostfix]
    public static void PlayerNPCController_OnEnable_Postfix(ref PlayerNPCController __instance)
    {
        __instance.gameObject.SetActive(false);
    }
}