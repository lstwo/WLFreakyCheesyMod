using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FMODUnity;
using UnityEngine;
using Channel = FMOD.Channel;
using ChannelGroup = FMOD.ChannelGroup;
using MODE = FMOD.MODE;
using OPENSTATE = FMOD.OPENSTATE;
using RESULT = FMOD.RESULT;
using Sound = FMOD.Sound;
using VCA = FMOD.Studio.VCA;

namespace WLButSlenderman;

public static class FMODAudio
{
    private const string MasterBusPath = "bus:/";
    private const string SfxVcaPath = "vca:/SFX";

    private static readonly List<Sound> loadedSounds = new();

    private static ChannelGroup masterGroup;
    private static bool hasMasterGroup;
    private static VCA sfxVca;

    public static bool IsReady => RuntimeManager.IsInitialized && RuntimeManager.HaveMasterBanksLoaded;

    public static float SfxVolume
    {
        get
        {
            if (!sfxVca.hasHandle())
            {
                if (!IsReady || RuntimeManager.StudioSystem.getVCA(SfxVcaPath, out sfxVca) != RESULT.OK)
                {
                    return 1f;
                }
            }

            return sfxVca.getVolume(out var volume) == RESULT.OK ? volume : 1f;
        }
    }

    private static ChannelGroup MasterGroup
    {
        get
        {
            if (hasMasterGroup || !IsReady)
            {
                return masterGroup;
            }

            if (RuntimeManager.StudioSystem.getBus(MasterBusPath, out var bus) != RESULT.OK ||
                bus.lockChannelGroup() != RESULT.OK)
            {
                return masterGroup;
            }

            RuntimeManager.StudioSystem.flushCommands();

            if (bus.getChannelGroup(out var group) == RESULT.OK)
            {
                masterGroup = group;
                hasMasterGroup = true;
            }

            return masterGroup;
        }
    }

    public static void LoadSound(string path, Action<Sound> callback)
    {
        FakePlugin.StartCoroutine(LoadRoutine(path, callback));
    }

    private static IEnumerator LoadRoutine(string path, Action<Sound> callback)
    {
        while (!IsReady)
        {
            yield return null;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"[WLButSlenderman] Sound not found: {path}");
            yield break;
        }

        var result = CoreSystem.createSound(path, MODE.CREATESAMPLE | MODE._3D | MODE.NONBLOCKING, out var sound);

        if (result != RESULT.OK)
        {
            Debug.LogError($"[WLButSlenderman] Failed to create sound {path}: {result}");
            yield break;
        }

        while (true)
        {
            if (sound.getOpenState(out var state, out _, out _, out _) != RESULT.OK || state == OPENSTATE.ERROR)
            {
                Debug.LogError($"[WLButSlenderman] Failed to load sound: {path}");
                sound.release();
                yield break;
            }

            if (state == OPENSTATE.READY)
            {
                break;
            }

            yield return null;
        }

        loadedSounds.Add(sound);
        callback?.Invoke(sound);
    }

    public static Channel PlayPaused(Sound sound)
    {
        if (!sound.hasHandle() || !IsReady)
        {
            return default;
        }

        return CoreSystem.playSound(sound, MasterGroup, true, out var channel) == RESULT.OK ? channel : default;
    }

    public static void PlayOneShot(Sound sound, float volume = 1f)
    {
        var channel = PlayPaused(sound);

        if (!channel.hasHandle())
        {
            return;
        }

        channel.setMode(MODE._2D | MODE.LOOP_OFF);
        channel.setLoopCount(0);
        channel.setVolume(volume * SfxVolume);
        channel.setPaused(false);
    }

    public static bool IsPlaying(Channel channel)
    {
        return channel.hasHandle() && channel.isPlaying(out var playing) == RESULT.OK && playing;
    }

    public static void Stop(ref Channel channel)
    {
        if (channel.hasHandle())
        {
            channel.stop();
        }

        channel.clearHandle();
    }

    public static void ReleaseAll()
    {
        foreach (var sound in loadedSounds)
        {
            if (sound.hasHandle())
            {
                sound.release();
            }
        }

        loadedSounds.Clear();
    }

    private static FMOD.System CoreSystem => RuntimeManager.CoreSystem;
}
