using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionFramework;
using MotionFramework.Console;
using MotionFramework.Event;
using MotionFramework.Audio;
using MotionFramework.Scene;
using MotionFramework.Pool;
using MotionFramework.Config;
using YooAsset;

public class GameLauncher : MonoBehaviour
{
	/// <summary>
	/// 资源运行模式：编辑器下强制走模拟模式，打包后走单机离线模式
	/// </summary>
	[Tooltip("EditorSimulateMode=编辑器模拟（不打包直接读工程内资源），OfflinePlayMode=读取包体内置资源")]
	public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

	/// <summary>
	/// YooAsset 资源包名称
	/// </summary>
	public const string PackageName = "DefaultPackage";

	/// <summary>
	/// 资源系统初始化最大重试次数
	/// </summary>
	private const int MaxInitRetryCount = 3;

	private static GameLauncher _instance;

	void Awake()
	{
		// 单例保护：场景重载或误放多个启动器时会重复初始化框架
		if (_instance != null && _instance != this)
		{
			UnityEngine.Debug.LogError($"{nameof(GameLauncher)} 已存在，销毁重复实例 : {gameObject.name}");
			Destroy(gameObject);
			return;
		}
		_instance = this;

		// 不销毁游戏对象
		DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
		PlayMode = EPlayMode.EditorSimulateMode;
#else
		PlayMode = EPlayMode.OfflinePlayMode;
#endif

		// 初始化框架（日志回调由框架统一转发）
		MotionEngine.Initialize(this, HandleMotionFrameworkLog);

		// 初始化控制台
		if (Application.isEditor || Debug.isDebugBuild)
			DeveloperConsole.Initialize("Script");

		// 初始化应用
		InitAppliaction();
	}
	void Start()
	{
		StartCoroutine(CreateGameModules());
	}
	void OnApplicationQuit()
	{
		// 释放常驻的图集句柄，避免退出/热重载时残留
		SpriteAtlasLoader.ReleaseAll();

		// 逆序销毁所有框架模块，保证下次可以重新初始化
		MotionEngine.Shutdown();
		_instance = null;
	}
	void Update()
	{
		MotionEngine.Update();

		if (GameLogicBridge.Logic != null)
			GameLogicBridge.Logic.Update();
	}
	void LateUpdate()
	{
		if (GameLogicBridge.Logic != null)
			GameLogicBridge.Logic.LateUpdate();
	}
	void OnGUI()
	{
		if (Application.isEditor || Debug.isDebugBuild)
			DeveloperConsole.Draw();
	}

	/// <summary>
	/// 初始化应用
	/// </summary>
	private void InitAppliaction()
	{
		UnityEngine.Debug.Log($"Game run platform : {Application.platform}");
		UnityEngine.Debug.Log($"Version of the runtime : {Application.unityVersion}");

		Application.runInBackground = true;
		Application.backgroundLoadingPriority = ThreadPriority.High;

		// 设置最大帧数
		Application.targetFrameRate = 60;

		// 屏幕不休眠
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
	}

	/// <summary>
	/// 监听框架日志
	/// </summary>
	private void HandleMotionFrameworkLog(ELogLevel logLevel, string log)
	{
		if (logLevel == ELogLevel.Log)
		{
			UnityEngine.Debug.Log(log);
		}
		else if (logLevel == ELogLevel.Error)
		{
			UnityEngine.Debug.LogError(log);
		}
		else if (logLevel == ELogLevel.Warning)
		{
			UnityEngine.Debug.LogWarning(log);
		}
		else if (logLevel == ELogLevel.Exception)
		{
			UnityEngine.Debug.LogError(log);
		}
		else
		{
			throw new NotImplementedException($"{logLevel}");
		}
	}

	/// <summary>
	/// 创建游戏模块
	/// </summary>
	private IEnumerator CreateGameModules()
	{
		// 创建事件管理器
		MotionEngine.CreateModule<EventManager>();

		// 1. 初始化 YooAsset 资源系统
		YooAssets.Initialize();
		ResourcePackage package = YooAssets.CreatePackage(PackageName);
		YooAssets.SetDefaultPackage(package);

		// 2. 初始化资源包（失败自动重试，避免弱网/包体缺失直接黑屏）
		bool initialized = false;
		for (int i = 0; i < MaxInitRetryCount; i++)
		{
			InitializationOperation initializationOperation = package.InitializeAsync(CreateInitializeParameters());
			yield return initializationOperation;
			if (initializationOperation.Status == EOperationStatus.Succeed)
			{
				initialized = true;
				break;
			}

			UnityEngine.Debug.LogError($"YooAsset 初始化失败({i + 1}/{MaxInitRetryCount}) : {initializationOperation.Error}");
			if (i + 1 < MaxInitRetryCount)
				yield return new WaitForSeconds(1f);
		}
		if (initialized == false)
		{
			UnityEngine.Debug.LogError("YooAsset 初始化最终失败，游戏无法启动。请检查 StreamingAssets 内是否已构建资源包（Tools/YooAsset/构建 AssetBundle）。");
			yield break;
		}

		// 2. 获取资源包版本
		RequestPackageVersionOperation versionOperation = package.RequestPackageVersionAsync();
		yield return versionOperation;
		if (versionOperation.Status != EOperationStatus.Succeed)
		{
			UnityEngine.Debug.LogError($"获取资源包版本失败 : {versionOperation.Error}");
			yield break;
		}

		// 3. 更新资源清单
		UpdatePackageManifestOperation manifestOperation = package.UpdatePackageManifestAsync(versionOperation.PackageVersion);
		yield return manifestOperation;
		if (manifestOperation.Status != EOperationStatus.Succeed)
		{
			UnityEngine.Debug.LogError($"更新资源清单失败 : {manifestOperation.Error}");
			yield break;
		}

		// 创建音频管理器
		MotionEngine.CreateModule<AudioManager>();

		// 创建场景管理器
		MotionEngine.CreateModule<SceneManager>();

		// 创建对象池管理器
		MotionEngine.CreateModule<GameObjectPoolManager>();

		// 创建配表管理器（当前工程未使用配表，但保证 Instance 不为空，避免后续使用时踩空）
		MotionEngine.CreateModule<ConfigManager>();

		// 注意：WindowManager / AssemblyManager 当前工程未使用（UI 与热更由 GameScript 自行实现），
		// 如需启用请在此处 CreateModule，不要直接访问它们的 Instance

		// 初始化图集延迟绑定（必须在资源系统创建之后）
		SpriteAtlasLoader.Initialize();

		// 初始化多语言系统（必须在资源系统创建之后）
		LocalizationManager.Initialize();

		// 启动游戏业务逻辑
		StartGameLogic();
	}

	/// <summary>
	/// 创建资源系统初始化参数
	/// </summary>
	private InitializeParameters CreateInitializeParameters()
	{
		if (PlayMode == EPlayMode.EditorSimulateMode)
		{
			// 编辑器模拟模式：直接读取工程内资源
			PackageInvokeBuildResult buildResult = EditorSimulateModeHelper.SimulateBuild(PackageName);
			EditorSimulateModeParameters parameters = new EditorSimulateModeParameters();
			parameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
			return parameters;
		}

		// 单机离线模式：读取包体内置资源
		OfflinePlayModeParameters offlineParameters = new OfflinePlayModeParameters();
		offlineParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
		return offlineParameters;
	}

	/// <summary>
	/// 启动游戏业务逻辑
	/// </summary>
	private void StartGameLogic()
	{
		if (GameLogicBridge.Logic == null)
		{
			UnityEngine.Debug.LogError("The game logic is not registered. Check the business assembly.");
			return;
		}

		GameLogicBridge.Logic.Start();
	}
}
