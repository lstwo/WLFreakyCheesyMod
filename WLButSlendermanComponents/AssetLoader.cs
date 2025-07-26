using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace WLButSlenderman;

public static class AssetLoader
{
    public static void LoadAudio(string path, Action<AudioClip> callback = null)
    {
        FakePlugin.StartCoroutine(LoadWav(path, callback));
    }
    
    private static IEnumerator LoadWav(string path, Action<AudioClip> callback)
    {
        var url = "file://" + path;

        using var www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load WAV: " + www.error);
            yield break;
        }
        
        var clip = DownloadHandlerAudioClip.GetContent(www);
        callback?.Invoke(clip);
    }
    
    public static Texture2D LoadTexture(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var fileData = File.ReadAllBytes(filePath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(fileData);
        return tex;
    }
}