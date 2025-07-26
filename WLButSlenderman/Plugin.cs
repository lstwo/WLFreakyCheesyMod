using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HawkNetworking;
using ShadowLib;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace WLButSlenderman;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static Plugin Instance;

	private bool registeredPrefabs = false;

    private void Awake()
    {
	    FakePlugin.startCoroutine += _StartCoroutine;
	    
        Instance = this;
        FakePlugin.Logger = base.Logger;

        FakePlugin.bundle = AssetUtils.LoadFromEmbeddedResources("WLButSlenderman.Resources.lstwo.wlbutslenderman.bundle");

        FakePlugin.enemyPrefab = FakePlugin.bundle.LoadAsset<GameObject>("Freak");
        FakePlugin.collectiblePrefab = FakePlugin.bundle.LoadAsset<GameObject>("Quad");

        GameInstance.onAssignedPlayerCharacter += character =>
        {
	        Enemy.deadPlayers.Add(character, false);
	        var go = new GameObject("revive");
	        var revive = go.AddComponent<PlayerRevive>();
	        var source = go.AddComponent<AudioSource>();
	        source.playOnAwake = false;
	        source.loop = false;
	        source.clip = FakePlugin.freakyCheesyClip;
	        revive.audioSource = source;
	        revive.playerCharacter = character;
	        
	        FakePlugin.playerRevives.Add(character, revive);
        };
        
        GameInstance.onUnassignedPlayerCharacter += character =>
        {
	        Enemy.deadPlayers.Remove(character);
	        FakePlugin.playerRevives.Remove(character);
        };
        
        FakePlugin.freakyCheesyTex = AssetLoader.LoadTexture(Application.streamingAssetsPath + "/freakycheesy.png");
        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/freakycheesy.wav", clip => FakePlugin.freakyCheesyClip = clip);
        
        SceneManager.sceneLoaded += (scene, loadMode) =>
        {
            if (DayNightCycle.InstanceExists)
            {
                var dayNightCycle = DayNightCycle.Instance;
                dayNightCycle.SetLightsEnabled(false);
                dayNightCycle.SetMidnight();
                dayNightCycle.SetSpeed(0);
                dayNightCycle.gameObject.SetActive(false);
            }
            
            if (WeatherSystem.InstanceExists)
            {
                WeatherSystem.Instance.gameObject.SetActive(false);
            }

            if (PlayerAmbientManager.InstanceExists)
            {
                PlayerAmbientManager.Instance.gameObject.SetActive(false);
            }
            
            RenderSettings.skybox = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(0f, 0f, 0f)
            };
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogDensity = 0.025f;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = 500f;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.black;
            
            if (loadMode == LoadSceneMode.Additive)
            {
                return;
            }
            
            if (scene.name == "MainMenu")
            {
	            if (!registeredPrefabs)
	            {
		            HawkNetworkManager.DefaultInstance.RegisterPrefab(FakePlugin.enemyPrefab.GetComponent<HawkNetworkBehaviour>());
		            HawkNetworkManager.DefaultInstance.RegisterPrefab(FakePlugin.collectiblePrefab.GetComponent<HawkNetworkBehaviour>());
		            registeredPrefabs = true;
	            }
	            
                StartCoroutine(MainMenuTomfoolery(scene));
            }
            else if (scene.name == "WobblyIsland")
            {
                StartCoroutine(OnWobblyIslandSceneLoaded());
            }
        };
        
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(Patches));
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    

    private IEnumerator OnWobblyIslandSceneLoaded()
    {
        yield return new WaitUntil(() => GameInstance.InstanceExists && HawkNetworkManager.InstanceExists);

        CreateEnemy();
        CreateAllPfps();
        FindObjectOfType<Camera>().clearFlags = CameraClearFlags.Nothing;
    }
    
    public static void _StartCoroutine(IEnumerator routine)
    {
        Instance.StartCoroutine(routine);
    }
    
    private static IEnumerator MainMenuTomfoolery(Scene scene)
    {
	    yield return null;
        
        FindObjectOfType<Camera>().clearFlags = CameraClearFlags.Nothing;

        Button.ButtonClickedEvent onPlayBtnClick = null;
        Button.ButtonClickedEvent onArcadeBtnClick = null;

        Button playBtn = null;
        Button arcadeBtn = null;

        foreach (var obj in scene.GetRootGameObjects())
        {
            if (obj.name is "MainMenu World Canvas")
            {
                onPlayBtnClick = obj.transform.GetChild(0).GetChild(3).GetChild(0).GetChild(0).GetChild(1)
                    .GetComponent<Button>().onClick;
                onArcadeBtnClick = obj.transform.GetChild(0).GetChild(3).GetChild(2).GetChild(0).GetChild(1)
	                .GetComponent<Button>().onClick;
            }
            else if (obj.name == "Main Menu Objects and Animation Events")
            {
                for (var i = 0; i < obj.transform.childCount; i++)
                {
                    var child = obj.transform.GetChild(i);
                    
                    if (child.name != "Grannys Front Door")
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
            else if (obj.name == "MainMenu-Canvas")
            {
                var mainMenuCanvasContent = obj.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1);

                for (var i = 0; i < mainMenuCanvasContent.childCount; i++)
                {
                    var child = mainMenuCanvasContent.GetChild(i);

                    if (child.name is "Title")
                    {
                        child.GetChild(0).GetChild(1).gameObject.SetActive(false);
                    }
                    else if (child.name is "Aracde" or "Arcade Button Graphic Pieces" or "World Button Graphic Pieces" 
                             or "Options" or "Quit" or "China" or "Merch")
                    {
                        child.gameObject.SetActive(false);
                    }
                    else if (child.name is "Play World")
                    {
                        playBtn = child.GetComponent<Button>();
                    }
                    else if (child.name is "Play Arcade")
                    {
	                    arcadeBtn = child.GetComponent<Button>();
                    }
                }
            }
            else if (obj.name is "City")
            {
                obj.gameObject.SetActive(false);
            }
        }
        
        playBtn.onClick = onPlayBtnClick;
        arcadeBtn.onClick = onArcadeBtnClick;
    }

    private void CreateEnemy()
    {
	    if (!HawkNetworkManager.InstanceExists || !HawkNetworkManager.DefaultInstance.GetMe().IsHost)
	    {
		    return;
	    }
		
	    NetworkPrefab.SpawnNetworkPrefab(FakePlugin.enemyPrefab.gameObject, new Vector3(0, 1000, 0));
    }
    
    private void OnGUI()
    {
	    if (GameInstance.InstanceExists && GameInstance.Instance.GetGamemode())
	    {	    
		    GUI.Box(new(5, 5, 300, 32), $"Collected Freaky Pictures: {CollectibleManager.CollectedPfps} / {CollectibleManager.totalPfps}");
	    }
    }

    private void CreateAllPfps()
    {
	    if (!HawkNetworkManager.InstanceExists || !HawkNetworkManager.DefaultInstance.GetMe().IsHost)
	    {
		    return;
	    }
	    
	    Vector3[] spots = [new(294f, 49f, -259f), new(-57f, 49f, -247f), new(-387f, 46f, -168f), new(-597f, 60f, -286f), 
		    new(-1189f, 66f, 170f), new(-1158.403f, 61f, 810.6613f), new(-913f, 76f, 922f), new(-393f, 227f, 709f),
			new(-92.5635f, 252f, 857.1072f), new(912f, 75f, 572f), new(936.3882f, 88.4171f, -867.5385f),
			new(622.9495f, 19.2098f, -1195.892f), new(-369.6806f, 42.5922f, -318.5412f)];

	    foreach (var _ in FakePlugin.texFileNames)
	    {
		    var spot = spots[Random.Range(0, spots.Length)];
		    var newSpotsList = spots.ToList();
		    newSpotsList.Remove(spot);
		    spots = newSpotsList.ToArray();

		    NetworkPrefab.SpawnNetworkPrefab(FakePlugin.collectiblePrefab.gameObject, spot, bSendTransform: true);
	    }
	    
	    CollectibleManager.totalPfps = FakePlugin.texFileNames.Length;
    }
    
    /*private bool GetValidSpot(out Vector3 validSpot)
	{
		validSpot = Vector3.zero;
		
		while(true)
		{
			var randomPointInside = new Vector3(Random.Range(-1500, 1500), 100000f, Random.Range(-1500, 1500));

			if (!CheckPoint(randomPointInside, out validSpot))
			{
				continue;
			}
			
			var num4 = 1 & ((CheckPoint(randomPointInside + new Vector3(3f, 0f, 3f), out _)) ? 1 : 0);
			var num5 = num4 & ((num4 != 0 && CheckPoint(randomPointInside + new Vector3(-3f, 0f, 3f), out _)) ? 1 : 0);
			var num6 = num5 & ((num5 != 0 && CheckPoint(randomPointInside + new Vector3(3f, 0f, -3f), out _)) ? 1 : 0);
				
			if ((num6 & ((num6 != 0 && CheckPoint(randomPointInside + new Vector3(-3f, 0f, -3f), out _)) ? 1 : 0)) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private bool CheckPoint(Vector3 point, out Vector3 validPoint)
	{
		var checkSystem = TerrainBuildingTexturePhysicsCheckSystem.Instance;
		
		var disallowMask = LayerMask.GetMask("Terrain", "Building", "World", "WorldLarge", "Water", "WaterCollide", "MeshDisplacement", "Road", "Default");
		var allowMask = LayerMask.GetMask("Terrain", "MeshDisplacement");
		
		point.y = 100000f;
		validPoint = Vector3.zero;
		
		if (checkSystem && !checkSystem.IsAreaValid(point))
		{
			return false;
		}
		
		if (Physics.Raycast(point, Vector3.down, out var hit, 3.4028235E+38f, disallowMask, QueryTriggerInteraction.Collide))
		{
			if (hit.collider.GetComponentElseParent<BuyableHouse>())
			{
				return false;
			}
			if (hit.collider.GetComponentElseParent<Water>())
			{
				return false;
			}
		}

		if (!Physics.Raycast(point, Vector3.down, out hit, 3.4028235E+38f, disallowMask,
			    QueryTriggerInteraction.Ignore) || ((1 << hit.collider.gameObject.layer) & allowMask) == 0 ||
		    !(Vector3.Dot(Vector3.up, hit.normal) >= 0.8f))
		{
			return false;
		}
		
		validPoint = hit.point;
		return true;
	}*/
}