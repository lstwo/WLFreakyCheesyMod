using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using HawkNetworking;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace WLButSlenderman;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static Plugin Instance;

	private bool registeredPrefabs;
	
	public static AssetBundle LoadFromEmbeddedResources(string assetBundleResourceName)
	{
		return AssetBundle.LoadFromMemory(ReadFully(Assembly.GetCallingAssembly().GetManifestResourceStream(assetBundleResourceName)));
	}
    
	private static byte[] ReadFully(Stream input)
	{
		using var ms = new MemoryStream();
		var buffer = new byte[81920];
		int read;
        
		while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
		{
			ms.Write(buffer, 0, read);
		}
        
		return ms.ToArray();
	}

    private void Awake()
    {
	    FakePlugin.startCoroutine += _StartCoroutine;
	    
        Instance = this;

        FakePlugin.bundle = LoadFromEmbeddedResources("WLButSlenderman.Resources.lstwo.wlbutslenderman.bundle");

        FakePlugin.enemyPrefab = FakePlugin.bundle.LoadAsset<GameObject>("Freak");
        FakePlugin.collectiblePrefab = FakePlugin.bundle.LoadAsset<GameObject>("Quad");
        FakePlugin.chasingPostProcessing = FakePlugin.bundle.LoadAsset<PostProcessProfile>("pp");
        FakePlugin.enemyBulletPrefab = FakePlugin.bundle.LoadAsset<GameObject>("Freak Small");

        GameInstance.onAssignedPlayerController += controller =>
        {
	        Enemy.deadPlayers.Add(controller, false);
	        var go = new GameObject("revive");
	        var revive = go.AddComponent<PlayerRevive>();
	        var source = go.AddComponent<AudioSource>();
	        source.playOnAwake = false;
	        source.loop = false;
	        source.clip = FakePlugin.freakyCheesyClip;
	        revive.audioSource = source;
	        revive.playerController = controller;
	        FakePlugin.playerRevives.Add(controller, revive);
        };
        
        GameInstance.onAssignedPlayerCharacter += character =>
        {
	        StartCoroutine(OnAssignedPlayerCharacter(character));
        };
        
        GameInstance.onUnassignedPlayerCharacter += character =>
        {
	        character.GetPlayerController().SetAllowedToRespawn(FakePlugin.playerRevives, true);
        };
        
        FakePlugin.freakyCheesyTex = AssetLoader.LoadTexture(Application.streamingAssetsPath + "/freakycheesy.png");
        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/freakycheesy.wav", clip => FakePlugin.freakyCheesyClip = clip);
        AssetLoader.LoadAudio(Application.streamingAssetsPath + "/shoot.wav", clip => FakePlugin.uhh = clip);
        
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

    private IEnumerator OnAssignedPlayerCharacter(PlayerCharacter character)
    {
	    yield return new WaitUntil(() => character.GetPlayerController() != null);
	    
	    if (Enemy.deadPlayers[character.GetPlayerController()])
	    {
		    FakePlugin.playerRevives[character.GetPlayerController()].Kill();
	    }
    }

    private IEnumerator OnWobblyIslandSceneLoaded()
    {
        yield return new WaitUntil(() => GameInstance.InstanceExists && HawkNetworkManager.InstanceExists);
        
        {
	        var obj = new GameObject("Chasing Post Processing");
	        obj.layer = LayerMask.NameToLayer("PostProcessing");
	        FakePlugin.chasingVolume = obj.AddComponent<PostProcessVolume>();
	        FakePlugin.chasingVolume.sharedProfile = FakePlugin.chasingPostProcessing;
	        FakePlugin.chasingVolume.isGlobal = true;
        }

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
		    GUI.Box(new(5, 5, 300, 32), $"Collected Pictures: {CollectibleManager.CollectedPfps} / {CollectibleManager.totalPfps}");
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
			new(622.9495f, 19.2098f, -1195.892f), new(-369.6806f, 42.5922f, -318.5412f), new(246.609f, 49.4004f, -221.48f),
			new(506.0774f, 43.1418f, -238.0443f), new(572.5466f, 42.2941f, -471.4037f), new(498.7458f, 14.3197f, -1292.79f),
			new(635.316f, 20.1038f, -980.0083f), new(781.0983f, 40.5622f, -946.0887f), new(988.8947f, 119.9751f, -549.9417f),
			new(506.3011f, 41.4432f, -7.5293f), new(254.0866f, 41.5806f, -182.1587f), new(-383.5231f, 42.8141f, -81.185f),
			new(-1041.381f, 58.6886f, 52.1031f), new(-1113.82f, 58.7989f, -132.1369f), new(-1201.591f, 64.3791f, 1025.008f),
			new(-611.4904f, 130.2777f, 994.1306f), new(-193.4097f, 131.7193f, 978.3655f), new(365.6877f, 25.8435f, 269.9019f),
			new(498.4528f, 41.9183f, -13.7343f), new(-748.7183f, 14.4481f, -651.1661f), new(-762.3329f, 73.2099f, -333.3555f),
			new(-101.4237f, 41.0892f, -90.2869f), new(-194.5528f, 41.6513f, -453.3484f), new(529.0341f, 16.6598f, -1073.509f),
			new(713.16f, 9.8806f, 41.5867f), new(-245.3932f, 108.6252f, -234.352f), new(262.1812f, 34.4148f, 12.4593f)];

	    for (var i = 0; i < FakePlugin.collectiblesCount * 2; i++)
	    {
		    var spot = spots[Random.Range(0, spots.Length)];
		    var newSpotsList = spots.ToList();
		    newSpotsList.Remove(spot);
		    spots = newSpotsList.ToArray();

		    NetworkPrefab.SpawnNetworkPrefab(FakePlugin.collectiblePrefab.gameObject, spot, bSendTransform: true);
	    }
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