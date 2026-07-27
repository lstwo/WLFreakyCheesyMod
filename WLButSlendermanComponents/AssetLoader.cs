using System;
using System.IO;
using UnityEngine;
using Sound = FMOD.Sound;

namespace WLButSlenderman;

public static class AssetLoader
{
    public static void LoadAudio(string path, Action<Sound> callback = null)
    {
        FMODAudio.LoadSound(path, callback);
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