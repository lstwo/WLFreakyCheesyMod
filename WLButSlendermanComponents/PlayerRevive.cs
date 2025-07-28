using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace WLButSlenderman;

public class PlayerRevive : MonoBehaviour
{
    public PlayerController playerController;
    public AudioSource audioSource;

    private float reviveTime;

    public void Kill()
    {
        if (playerController.GetPlayerCharacter().HasEnteredAction())
        {
            playerController.GetPlayerControllerInteractor().GetEnteredAction().RequestExit(playerController);
        }
        
        playerController.GetPlayerCharacter().GetRagdollController().Knockout();
        playerController.GetPlayerCharacter().GetRagdollController().LockRagdollState();
        playerController.SetAllowedToRespawn(this, false);
        playerController.GetPlayerCharacter().GetPlayerCharacterInput().DisableControls(playerController);
        playerController.GetPlayerControllerInteractor().SetInteratorInputEnabled(this, false);
        Enemy.deadPlayers[playerController] = true;
    }

    public void Revive()
    {
        playerController.GetPlayerCharacter().GetRagdollController().UnlockRagdollState();
        playerController.GetPlayerCharacter().GetRagdollController().Wakeup();
        playerController.SetAllowedToRespawn(this, true);
        playerController.GetPlayerCharacter().GetPlayerCharacterInput().EnableControls(playerController);
        playerController.GetPlayerControllerInteractor().SetInteratorInputEnabled(this, true);
        Enemy.deadPlayers[playerController] = false;
    }

    private void Update()
    {
        if (playerController.IsDestroyed() || !Enemy.deadPlayers.ContainsKey(playerController))
        {
            Destroy(gameObject);
            return;
        }
        
        if (!Enemy.deadPlayers[playerController])
        {
            return;
        }
        
        var myPos = playerController.GetPlayerCharacter().GetPlayerPosition();
        var isReviving = false;
        
        foreach (var player in GameInstance.Instance.GetPlayerCharacters().Where(x => !Enemy.deadPlayers[x.GetPlayerController()]))
        {
            if (playerController.GetPlayerCharacter() == player)
            {
                continue;
            }

            var pos = player.GetPlayerPosition();
            var distance = Vector3.Distance(myPos, pos);

            if (distance <= 15f)
            {
                isReviving = true;
                break;
            }
        }

        if (isReviving)
        {
            reviveTime += Time.deltaTime;

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            reviveTime = 0;
            audioSource.Stop();
        }

        if (reviveTime >= 10f)
        {
            Revive();
            reviveTime = 0;
        }
    }
}