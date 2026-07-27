using System.Collections.Generic;
using UnityEngine;
using Channel = FMOD.Channel;
using MODE = FMOD.MODE;
using Sound = FMOD.Sound;

namespace WLButSlenderman;

public class FMODAudioSource : MonoBehaviour
{
    public Sound clip;
    public bool loop;
    public bool playOnAwake;
    public float volume = 1f;

    public bool spatial;

    public float dopplerLevel;
    public float minDistance = 1f;
    public float maxDistance = 500f;

    private Channel channel;
    private readonly List<OneShot> oneShots = new();

    public bool isPlaying => FMODAudio.IsPlaying(channel);

    public static FMODAudioSource ReplaceOn(GameObject gameObject)
    {
        foreach (var legacy in gameObject.GetComponentsInChildren<AudioSource>(true))
        {
            Destroy(legacy);
        }

        return gameObject.AddComponent<FMODAudioSource>();
    }

    public static FMODAudioSource AddTo(GameObject gameObject)
    {
        return gameObject.AddComponent<FMODAudioSource>();
    }

    public void Play()
    {
        Stop();
        channel = StartChannel(clip, loop);
    }

    public void PlayOneShot(Sound sound, float volumeScale = 1f)
    {
        var oneShot = StartChannel(sound, false, volumeScale);

        if (oneShot.hasHandle())
        {
            oneShots.Add(new OneShot { channel = oneShot, volumeScale = volumeScale });
        }
    }

    public void Stop()
    {
        FMODAudio.Stop(ref channel);
    }

    private Channel StartChannel(Sound sound, bool looped, float volumeScale = 1f)
    {
        var newChannel = FMODAudio.PlayPaused(sound);

        if (!newChannel.hasHandle())
        {
            return default;
        }

        // Linear rolloff is what the prefabs were authored with; FMOD's default inverse rolloff
        // drops off far faster and makes the enemy inaudible at chase range.
        newChannel.setMode((spatial ? MODE._3D | MODE._3D_WORLDRELATIVE | MODE._3D_LINEARROLLOFF : MODE._2D)
                           | (looped ? MODE.LOOP_NORMAL : MODE.LOOP_OFF));
        newChannel.setLoopCount(looped ? -1 : 0);
        Apply(newChannel, volumeScale);
        newChannel.setPaused(false);

        return newChannel;
    }

    private void Apply(Channel target, float volumeScale)
    {
        if (!target.hasHandle())
        {
            return;
        }

        target.setVolume(volume * volumeScale * FMODAudio.SfxVolume);

        if (!spatial)
        {
            return;
        }

        var position = FMODUnity.RuntimeUtils.ToFMODVector(transform.position);
        var velocity = new FMOD.VECTOR();

        target.set3DAttributes(ref position, ref velocity);
        target.set3DMinMaxDistance(minDistance, maxDistance);
        target.set3DDopplerLevel(dopplerLevel);
    }

    private void Start()
    {
        if (playOnAwake)
        {
            Play();
        }
    }

    private void LateUpdate()
    {
        Apply(channel, 1f);

        for (var i = oneShots.Count - 1; i >= 0; i--)
        {
            if (!FMODAudio.IsPlaying(oneShots[i].channel))
            {
                oneShots.RemoveAt(i);
                continue;
            }

            Apply(oneShots[i].channel, oneShots[i].volumeScale);
        }
    }

    private void OnDestroy()
    {
        Stop();

        for (var i = 0; i < oneShots.Count; i++)
        {
            var oneShot = oneShots[i].channel;
            FMODAudio.Stop(ref oneShot);
        }

        oneShots.Clear();
    }

    private struct OneShot
    {
        public Channel channel;
        public float volumeScale;
    }
}
