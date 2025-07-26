using System;
using System.Linq;
using HawkNetworking;
using UnityEngine;

namespace WLButSlenderman;

public class PlayerRevive : MonoBehaviour
{
    public PlayerCharacter playerCharacter;
    public AudioSource audioSource;

    private float reviveTime;

    public void Kill()
    {
        if (playerCharacter.HasEnteredAction())
        {
            playerCharacter.GetPlayerController().GetPlayerControllerInteractor().GetEnteredAction().RequestExit(playerCharacter.GetPlayerController());
        }
        
        playerCharacter.GetRagdollController().Knockout();
        playerCharacter.GetRagdollController().LockRagdollState();
        playerCharacter.GetPlayerController().SetAllowedToRespawn(this, false);
        playerCharacter.GetPlayerCharacterInput().DisableControls(playerCharacter.GetPlayerController());
        Enemy.deadPlayers[playerCharacter] = true;
    }

    public void Revive()
    {
        playerCharacter.GetRagdollController().UnlockRagdollState();
        playerCharacter.GetRagdollController().Wakeup();
        playerCharacter.GetPlayerController().SetAllowedToRespawn(this, true);
        playerCharacter.GetPlayerCharacterInput().EnableControls(playerCharacter.GetPlayerController());
        Enemy.deadPlayers[playerCharacter] = false;
    }

    private void Update()
    {
        if (playerCharacter.IsDestroyed() || !Enemy.deadPlayers.ContainsKey(playerCharacter))
        {
            Destroy(gameObject);
            return;
        }
        
        if (!Enemy.deadPlayers[playerCharacter])
        {
            return;
        }
        
        var myPos = playerCharacter.GetPlayerPosition();
        var isReviving = false;
        
        foreach (var player in GameInstance.Instance.GetPlayerCharacters().Where(x => !Enemy.deadPlayers[x]))
        {
            if (playerCharacter == player)
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