using UnityEngine;

namespace WLButSlenderman;

public class Collectible : ActionInteract
{
    private void Start()
    {
        var tex = AssetLoader.LoadTexture($"{Application.streamingAssetsPath}/DefaultIcon.png");
        
        var mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = tex;
        GetComponent<MeshRenderer>().material = mat;
    }
    
    protected override void OnInteract(PlayerController playerController)
    {
        GetComponent<CollectibleNetwork>().Collect();
    }
}