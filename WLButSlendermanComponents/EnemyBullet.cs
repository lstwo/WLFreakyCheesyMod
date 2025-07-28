using HawkNetworking;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WLButSlenderman;

public class EnemyBullet : HawkNetworkBehaviour
{
    private Vector3 targetPos;
    private Vector3 startPosition;
    private bool isFollowing;
    private float timer;
    private AudioSource audioSource;
    
    public float offset = 1;
    public Vector3 finalTargetPos;

    protected override void NetworkPost(HawkNetworkObject networkObject)
    {
        base.NetworkPost(networkObject);

        if (!networkObject.IsServer())
        {
            return;
        }
        
        targetPos = transform.position + Random.onUnitSphere * 5;
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = FakePlugin.shootSound;
    }

    private void Update()
    {
        if (networkObject == null || !networkObject.IsServer())
        {
            return;
        }

        if (Vector3.Distance(transform.position, targetPos) > 0.1f && !isFollowing)
        {
            isFollowing = true;
            audioSource.PlayOneShot(audioSource.clip);
        }

        if (!isFollowing)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
            return;
        }
        
        timer += Time.deltaTime;

        if(timer >= offset)
        {
            transform.position = Vector3.Lerp(startPosition, finalTargetPos, (timer - offset) / 50f);
        }
    }
}