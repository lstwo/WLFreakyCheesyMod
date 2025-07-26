using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HawkNetworking;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WLButSlenderman;

public class Enemy : HawkNetworkBehaviour
{
    private Vector3 targetPos;
    private State state;
    private AudioSource audioSource;
    private float timeSinceLastSeen;
    private PlayerCharacter currentlyChasingPlayer;
    private float timeSinceLastSound = 0;
    private Dictionary<State, AudioClip> sounds = new();
    private Texture2D regularTex;
    private Texture2D chasingTex;
    private MeshRenderer meshRenderer;
    
    public static Dictionary<PlayerCharacter, bool> deadPlayers = new();

    private byte RPC_SOUND;
    private byte RPC_PLAYER_DIE;

    protected override void Awake()
    {
        base.Awake();

        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/Chase.wav", clip => sounds.Add(State.Chasing, clip));
        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/Following.wav",
            clip => sounds.Add(State.Following, clip));
        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/Lost.wav", clip => sounds.Add(State.Searching, clip));
        chasingTex = AssetLoader.LoadTexture(Application.streamingAssetsPath + "/freakycheesychase.png");
        
        GetComponent<SphereCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var character = other.gameObject.GetComponentInParent<PlayerCharacter>();
        
        if (character && !deadPlayers[character])
        {
            var id = character.networkObject.GetNetworkID();
            networkObject.SendRPC(RPC_PLAYER_DIE, RPCRecievers.All, id);
            
            FakePlugin.playerRevives[character].Kill();

            transform.position = new(0, 200, 0);
        }
    }

    private void ClientPlayerDie(HawkNetReader reader, HawkRPCInfo info)
    {
        var id = reader.ReadUInt32();
        var character = GameInstance.Instance.GetPlayerCharacterByNetworkID(id);
        deadPlayers[character] = true;

        if (character.GetPlayerController().networkObject.IsOwner())
        {
            Process.Start(Application.streamingAssetsPath + "/jump.mp4");
        }
        
        if (deadPlayers.All(x => x.Value || !x.Key))
        {
            Application.Quit();
        }
    }

    private void ClientPlaySound(HawkNetReader reader, HawkRPCInfo info)
    {
        var state = (State)reader.ReadByte();

        if (timeSinceLastSound > 6)
        {
            audioSource.PlayOneShot(sounds[state]);
            timeSinceLastSound = 0;
        }
        
        if (state == State.Chasing)
        {
            meshRenderer.material.mainTexture = chasingTex;
        }
        else
        {
            meshRenderer.material.mainTexture = regularTex;
        }
    }

    protected override void RegisterRPCs(HawkNetworkObject networkObject)
    {
        base.RegisterRPCs(networkObject);

        RPC_PLAYER_DIE = networkObject.RegisterRPC(ClientPlayerDie);
        RPC_SOUND = networkObject.RegisterRPC(ClientPlaySound);
    }

    protected override void NetworkStart(HawkNetworkObject networkObject)
    {
        base.NetworkStart(networkObject);
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        var mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = FakePlugin.freakyCheesyTex;
        meshRenderer.material = mat;
        regularTex = FakePlugin.freakyCheesyTex;
    }

    protected override void NetworkPost(HawkNetworkObject networkObject)
    {
        base.NetworkPost(networkObject);
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        if (!networkObject.IsServer())
        {
            yield break;
        }
        
        var wait = new WaitForSeconds(0.25f);

        while (GameInstance.Instance.GetGamemode())
        {
            yield return wait;

            var players = GameInstance.Instance.GetPlayerCharacters().Where(x => !deadPlayers[x]);
            
            PlayerCharacter closestVisiblePlayer = null;
            var closestVisiblePlayerDistance = float.MaxValue;
            var closestPlayerDistance = float.MaxValue;

            foreach (var player in players)
            {
                var playerPos = player.GetPlayerPosition();
                var distanceToPlayer = Vector3.Distance(playerPos, transform.position);
                var directionToPlayer = (playerPos - transform.position).normalized;
                var seesPlayer = Physics.Raycast(transform.position, directionToPlayer, out var hit, 1000)
                                 && (hit.collider.CompareTag("Player") || (hit.rigidbody && hit.rigidbody.TryGetComponent<ActionEnterExitInteract>(out _)));
                
                if (distanceToPlayer < closestVisiblePlayerDistance && seesPlayer)
                {
                    closestVisiblePlayer = player;
                    closestVisiblePlayerDistance = distanceToPlayer;
                }

                if (distanceToPlayer < closestPlayerDistance)
                {
                    closestPlayerDistance = distanceToPlayer;
                }
            }

            var distanceToTargetPos = Vector3.Distance(transform.position, targetPos);

            if (state == State.Chasing && currentlyChasingPlayer && !currentlyChasingPlayer.IsDestroyed())
            {
                if (Vector3.Distance(transform.position, currentlyChasingPlayer.GetPlayerPosition()) <= 150f)
                {
                    targetPos = currentlyChasingPlayer.GetPlayerPosition();
                    timeSinceLastSeen = 0;
                    continue;
                }

                state = State.Following;
            }
            
            if (closestVisiblePlayer is not null && closestVisiblePlayerDistance <= 150f)
            {
                targetPos = closestVisiblePlayer.GetPlayerPosition();
                state = State.Chasing;
                timeSinceLastSeen = 0;
                currentlyChasingPlayer = closestVisiblePlayer;

                networkObject.SendRPCUnreliable(RPC_SOUND, RPCRecievers.All, (byte)State.Chasing);
                
                continue;
            }

            if (closestPlayerDistance <= 150f && closestVisiblePlayer is null)
            {
                if (distanceToTargetPos <= 1f && state == State.Searching)
                {
                    var searchPos = transform.position +
                                    new Vector3(Random.Range(-100, 100), 1000, Random.Range(-100, 100));
                    Physics.Raycast(searchPos, Vector3.down, out var searchHit, 2000);
                    targetPos = searchHit.point + Vector3.up * 2;
                }
                    
                state = State.Searching;
                continue;
            }

            if (closestVisiblePlayer is not null && closestVisiblePlayerDistance > 150f)
            {
                if (state == State.Searching)
                {
                    networkObject.SendRPCUnreliable(RPC_SOUND, RPCRecievers.All, (byte)State.Following);
                }
                
                targetPos = closestVisiblePlayer.GetPlayerPosition();
                state = State.Following;
                timeSinceLastSeen = 0;
                continue;
            }
            
            if (Vector3.Distance(transform.position, targetPos) <= 1f && state == State.Searching)
            {
                var searchPos = transform.position +
                                new Vector3(Random.Range(-50, 50), 1000, Random.Range(-50, 50));
                if (timeSinceLastSeen < 10)
                {
                    Physics.Raycast(searchPos, Vector3.down, out var searchHit, 3000);
                    targetPos = searchHit.point + Vector3.up * 2;
                }
                else
                {
                    Physics.Raycast(searchPos, Vector3.down, out var searchHit, 3000);
                    targetPos = searchHit.point + Vector3.up * 100;
                }
            }

            if (state != State.Searching)
            {
                networkObject.SendRPCUnreliable(RPC_SOUND, RPCRecievers.All, (byte)State.Searching);
            }
                    
            state = State.Searching;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new(5, 50, 250, 24), $"FC State: {Enum.GetName(typeof(State), state)}");
    }

    private void Update()
    {
        timeSinceLastSound += Time.deltaTime;

        if (networkObject == null || !networkObject.IsServer())
        {
            return;
        }
        
        timeSinceLastSeen += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (networkObject == null || !networkObject.IsServer())
        {
            return;
        }
        
        var speed = state switch
        {
            State.Chasing => 30,
            State.Searching => 10,
            State.Following => 15,
        };

        var direction = (targetPos - transform.position).normalized;
        transform.position += direction * (speed * Time.fixedDeltaTime);
    }

    private enum State : byte
    {
        Following,
        Chasing,
        Searching
    }
}