using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionFramework;
using MotionFramework.Console;
using MotionFramework.Resource;
using MotionFramework.Event;
using MotionFramework.Config;
using MotionFramework.Audio;
using MotionFramework.Scene;
using MotionFramework.Pool;

public class GameLauncher : MonoBehaviour
{
	[Tooltip("资源系统的运行模式：Resources=编辑器模拟运行（直接读工程内资源），AssetBundle=从StreamingAssets加载")]
	public EAssetSystemMode AssetSystemMode = EAssetSystemMode.Resources;

	void Awake()
	{
		// 不销毁游戏对象
		DontDestroyOnLoad(gameObject);

		// 注册日志系统
		MotionLog.RegisterCallback(HandleMotionFrameworkLog);

		// 初始化框架
		MotionEngine.Initialize(this);

		// 初始化控制台
		if (Application.isEditor || Debug.isDebugBuild)
			DeveloperConsole.Initialize();

		// 初始化应用
		InitAppliaction();
	}
	void Start()
	{
		CreateGameModules();

		// 初始化多语言系统（必须在资源系统创建之后）
		LocalizationManager.Initialize();

		// 启动游戏业务逻辑
		StartGameLogic();
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
			DeveloperConsole.DrawGUI();
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
	private void HandleMotionFrameworkLog(ELogLevel logType, string log)
	{
		if (logType == ELogLevel.Log)
		{
			UnityEngine.Debug.Log(log);
		}
		else if (logType == ELogLevel.Error)
		{
			UnityEngine.Debug.LogError(log);
		}
		else if (logType == ELogLevel.Warning)
		{
			UnityEngine.Debug.LogWarning(log);
		}
		else if (logType == ELogLevel.Exception)
		{
			UnityEngine.Debug.LogError(log);
		}
		else
		{
			throw new NotImplementedException($"{logType}");
		}
	}

	/// <summary>
	/// 创建游戏模块
	/// </summary>
	private void CreateGameModules()
	{
		// 创建事件管理器
		MotionEngine.CreateModule<EventManager>();

		// 创建资源管理器
		// 注意：单机版没有CDN和补丁流程，资源要么在编辑器里模拟运行，要么直接从StreamingAssets里加载
		var resourceCreateParam = new ResourceManager.CreateParameters();
		resourceCreateParam.LocationRoot = GameDefine.AssetRootPath;
		resourceCreateParam.SimulationOnEditor = AssetSystemMode == EAssetSystemMode.Resources;
		resourceCreateParam.BundleServices = SimulationBundleServices();
		MotionEngine.CreateModule<ResourceManager>(resourceCreateParam);

		// 创建音频管理器
		MotionEngine.CreateModule<AudioManager>();

		// 创建场景管理器
		MotionEngine.CreateModule<SceneManager>();

		// 创建对象池管理器
		MotionEngine.CreateModule<GameObjectPoolManager>();
	}

	/// <summary>
	/// 获取AssetBundle服务
	/// </summary>
	private IBundleServices SimulationBundleServices()
	{
		if (AssetSystemMode == EAssetSystemMode.Resources)
			return null;

		return LocalBundleServices.Instance;
	}

	/// <summary>
	/// 启动游戏业务逻辑
	/// </summary>
	private void StartGameLogic()
	{
		if (GameLogicBridge.Logic == null)
		{
			MotionLog.Log(ELogLevel.Error, "The game logic is not registered. Check the business assembly.");
			return;
		}

		GameLogicBridge.Logic.Start();
	}
}
