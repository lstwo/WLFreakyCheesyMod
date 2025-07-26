using HawkNetworking;
using UnityEngine;

namespace WLButSlenderman;

public class Collectible : ActionInteract
{
    private static int index;
    
    private void Start()
    {
        var tex = AssetLoader.LoadTexture($"{Application.streamingAssetsPath}/{FakePlugin.texFileNames[index]}.png");
        
        var mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = tex;
        GetComponent<MeshRenderer>().material = mat;
        
        index++;
    }
    
    protected override void OnInteract(PlayerController playerController)
    {
        Collect();
    }

    public void Collect()
    {
        CollectibleManager.CollectedPfps++;
        VanishComponent.VanishAndDestroy(gameObject);
    }
}