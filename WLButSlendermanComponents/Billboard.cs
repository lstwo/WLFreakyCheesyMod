using UnityEngine;

namespace WLButSlenderman;

public class Billboard : MonoBehaviour
{
    private PlayerController player;
    
    private void Start()
    {
        player = GameInstance.Instance.GetPlayerController(SteamP2PNetworkManager.SteamInstance.GetMe(), 0);
    }

    private void Update()
    {
        transform.LookAt(player.GetGameplayCamera().transform);
        transform.forward = -transform.forward;
    }
}