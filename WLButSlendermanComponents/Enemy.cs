using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HawkNetworking;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
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
    private Light light;
    private AudioClip heartBeatClip;

    private float timeSinceLastBlast;
    
    public static Dictionary<PlayerController, bool> deadPlayers = new();
    private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");

    private byte RPC_SOUND;
    private byte RPC_PLAYER_DIE;
    private byte RPC_INFORM_PLAYER;

    protected override void Awake()
    {
        base.Awake();

        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/Chase.wav", clip => sounds.Add(State.Chasing, clip));
        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/Following.wav", clip => sounds.Add(State.Following, clip));
        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/Lost.wav", clip => sounds.Add(State.Searching, clip));
        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/heartbeat.wav", clip => heartBeatClip = clip);
        chasingTex = AssetLoader.LoadTexture(Application.streamingAssetsPath + "/freakycheesychase.png");
        
        GetComponent<SphereCollider>().isTrigger = true;
        
        HawkNetworkManager.DefaultInstance.onPlayerAccepted += connection =>
        {
            networkObject.SendRPC(RPC_INFORM_PLAYER, connection, CollectibleManager.CollectedPfps);
        };
    }

    private void ClientInformPlayer(HawkNetReader reader, HawkRPCInfo info)
    {
        CollectibleManager.CollectedPfps = reader.ReadInt32();
    }

    private void OnTriggerEnter(Collider other)
    {
        var character = other.gameObject.GetComponentInParent<PlayerCharacter>();
        
        if (character && !deadPlayers[character.GetPlayerController()])
        {
            var id = character.networkObject.GetNetworkID();
            networkObject.SendRPC(RPC_PLAYER_DIE, RPCRecievers.All, id);
            
            FakePlugin.playerRevives[character.GetPlayerController()].Kill();

            transform.position = new(0, 500, 0);
        }
    }

    private void ClientPlayerDie(HawkNetReader reader, HawkRPCInfo info)
    {
        var id = reader.ReadUInt32();
        var character = GameInstance.Instance.GetPlayerCharacterByNetworkID(id);
        deadPlayers[character.GetPlayerController()] = true;

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
        var playerId = reader.ReadUInt32();
        var player = GameInstance.Instance.GetPlayerControllerByNetworkID(playerId);

        if (timeSinceLastSound > 6)
        {
            audioSource.PlayOneShot(sounds[state]);
            timeSinceLastSound = 0;
        }
        
        if (state == State.Chasing)
        {
            meshRenderer.material.mainTexture = chasingTex;
            
            print(player);
            print(player?.networkObject);
            print(player?.networkObject?.IsOwner());
            print(player?.GetGameplayCamera());

            if (player?.networkObject == null || !player.networkObject.IsOwner() || !player.GetGameplayCamera())
            {
                return;
            }

            if (FakePlugin.heartBeatSource == null)
            {
                FakePlugin.heartBeatSource = player.GetGameplayCamera().gameObject.AddComponent<AudioSource>();
                FakePlugin.heartBeatSource.loop = true;
                FakePlugin.heartBeatSource.clip = heartBeatClip;
            }
            
            FakePlugin.heartBeatSource.Play();
            FakePlugin.ToggleEffects(true);
        }
        else
        {
            meshRenderer.material.mainTexture = regularTex;

            if (FakePlugin.heartBeatSource != null && FakePlugin.heartBeatSource.isPlaying)
            {
                FakePlugin.heartBeatSource?.Stop();
            }
            
            FakePlugin.ToggleEffects(false);
        }
    }

    protected override void RegisterRPCs(HawkNetworkObject networkObject)
    {
        base.RegisterRPCs(networkObject);

        RPC_PLAYER_DIE = networkObject.RegisterRPC(ClientPlayerDie);
        RPC_SOUND = networkObject.RegisterRPC(ClientPlaySound);
        RPC_INFORM_PLAYER = networkObject.RegisterRPC(ClientInformPlayer);
    }

    protected override void NetworkStart(HawkNetworkObject networkObject)
    {
        base.NetworkStart(networkObject);
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        audioSource = GetComponent<AudioSource>();

        audioSource.dopplerLevel = 0;
        audioSource.spatialBlend = 1;

        light = GetComponentInChildren<Light>();
        
        var mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = FakePlugin.freakyCheesyTex;
        mat.SetFloat(Glossiness, 0);
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

            var players = GameInstance.Instance.GetPlayerControllers().Where(x => !deadPlayers[x]);
            
            PlayerCharacter closestVisiblePlayer = null;
            var closestVisiblePlayerDistance = float.MaxValue;
            var closestPlayerDistance = float.MaxValue;

            foreach (var player in players)
            {
                if (player.GetPlayerCharacter() is null)
                {
                    continue;
                }
                
                var playerPos = player.GetPlayerCharacter().GetPlayerPosition();
                var distanceToPlayer = Vector3.Distance(playerPos, transform.position);
                var directionToPlayer = (playerPos - transform.position).normalized;
                var seesPlayer = Physics.Raycast(transform.position, directionToPlayer, out var hit, 1000)
                                 && (hit.collider.CompareTag("Player") || (hit.rigidbody && hit.rigidbody.TryGetComponent<ActionEnterExitInteract>(out _)));
                
                if (distanceToPlayer < closestVisiblePlayerDistance && seesPlayer)
                {
                    closestVisiblePlayer = player.GetPlayerCharacter();
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
                var closeByPlayers = players.Select(player => Vector3.Distance(player.GetPlayerCharacter().GetPlayerPosition(), closestVisiblePlayer.GetPlayerPosition())).Count(distance => distance <= 10);

                if (false && CollectibleManager.CollectedPfps >= CollectibleManager.totalPfps / 2f &&
                    Random.value <= 0.25f + 0.05f * closeByPlayers)
                {
                    var bounds = new Bounds();
                    
                    foreach (var player in players)
                    {
                        if (Vector3.Distance(player.GetPlayerCharacter().GetPlayerPosition(),
                                closestVisiblePlayer.GetPlayerPosition()) <= 10f)
                        {
                            bounds.Encapsulate(player.GetPlayerCharacter().GetPlayerPosition());
                        }
                    }

                    StartCoroutine(ShootRoutine(4 + closeByPlayers, transform.position + bounds.center.normalized * 1000f, 5f + closeByPlayers * 4f));
                }
                
                targetPos = closestVisiblePlayer.GetPlayerPosition();
                state = State.Chasing;
                timeSinceLastSeen = 0;
                currentlyChasingPlayer = closestVisiblePlayer;

                networkObject.SendRPC(RPC_SOUND, RPCRecievers.All, (byte)State.Chasing, currentlyChasingPlayer.GetPlayerController().networkObject.GetNetworkID());
                
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
                    networkObject.SendRPC(RPC_SOUND, RPCRecievers.All, (byte)State.Following, 0u);
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
                networkObject.SendRPC(RPC_SOUND, RPCRecievers.All, (byte)State.Searching, 0u);
            }
                    
            state = State.Searching;
        }
    }

    private IEnumerator ShootRoutine(int count, Vector3 targetPos, float broadness)
    {
        for (var i = 0; i < count; i++)
        {
            var bulletBehaviour = NetworkPrefab.SpawnNetworkPrefab(FakePlugin.enemyBulletPrefab, transform.position, Quaternion.identity) as EnemyBullet;
            bulletBehaviour.finalTargetPos = Quaternion.AngleAxis(Random.Range(-broadness, broadness), Random.insideUnitSphere) * targetPos;
            print("BULLET");
            yield return new WaitForSeconds(0.1f);
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
        
        var targetRot = Quaternion.LookRotation(targetPos - transform.position);
        light.transform.localRotation = Quaternion.Slerp(light.transform.localRotation, targetRot, Time.deltaTime * 10f);
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
        
        var progress = Mathf.Clamp01((float)CollectibleManager.CollectedPfps / CollectibleManager.totalPfps);
        var speedMultiplier = 1f + progress;

        var direction = (targetPos - transform.position).normalized;
        transform.position += direction * (speed * speedMultiplier * Time.fixedDeltaTime);
    }

    private enum State : byte
    {
        Following,
        Chasing,
        Searching
    }
}