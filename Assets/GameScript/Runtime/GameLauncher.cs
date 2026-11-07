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

	void Awake()
	{
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

		InitializationOperation initializationOperation;
		if (PlayMode == EPlayMode.EditorSimulateMode)
		{
			// 编辑器模拟模式：直接读取工程内资源
			PackageInvokeBuildResult buildResult = EditorSimulateModeHelper.SimulateBuild(PackageName);
			EditorSimulateModeParameters createParameters = new EditorSimulateModeParameters();
			createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
			initializationOperation = package.InitializeAsync(createParameters);
		}
		else
		{
			// 单机离线模式：读取包体内置资源
			OfflinePlayModeParameters createParameters = new OfflinePlayModeParameters();
			createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
			initializationOperation = package.InitializeAsync(createParameters);
		}

		yield return initializationOperation;
		if (initializationOperation.Status != EOperationStatus.Succeed)
		{
			UnityEngine.Debug.LogError($"YooAsset 初始化失败 : {initializationOperation.Error}");
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

		// 初始化图集延迟绑定（必须在资源系统创建之后）
		SpriteAtlasLoader.Initialize();

		// 初始化多语言系统（必须在资源系统创建之后）
		LocalizationManager.Initialize();

		// 启动游戏业务逻辑
		StartGameLogic();
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
