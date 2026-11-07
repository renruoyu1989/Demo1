using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 游戏业务逻辑入口
	/// 注意：单机版没有热更程序集，业务代码直接编译进工程
	/// </summary>
	public sealed class HotfixMain : IGameLogic
	{
		private static HotfixMain _instance;

		/// <summary>
		/// 注册到启动层
		/// 注意：AfterSceneLoad 早于场景对象的 Awake，能保证启动层拿到实例
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Register()
		{
			_instance = new HotfixMain();
			GameLogicBridge.Register(_instance);
		}

		public void Start()
		{
			HotfixLogger.Log("game logic start");

			AudioPlayerSetting.InitAudioSetting();

			// 初始化数据管理器
			DataManager.Instance.Start();

			// 初始化界面管理器
			UIManager.Instance.Start();

			// 初始化状态机管理器
			var prepare = new HotfixStatePrepare(EHotfixStateType.Prepare);
			var menu = new HotfixStateMenu(EHotfixStateType.Menu);
			var select = new HotfixStateSelect(EHotfixStateType.Select);
			var playing = new HotfixStatePlaying(EHotfixStateType.Playing);
			HotfixFsmManager.Instance.AddState(prepare);
			HotfixFsmManager.Instance.AddState(menu);
			HotfixFsmManager.Instance.AddState(select);
			HotfixFsmManager.Instance.AddState(playing);
			HotfixFsmManager.Instance.Run(EHotfixStateType.Prepare);
		}

		public void Update()
		{
			// 先消费 UI 发起的挂起状态切换，避免在 UI 回调里同步 Exit/Enter
			FerryGameCommands.ProcessPendingState();

			HotfixFsmManager.Instance.Update();
			DataManager.Instance.Update();
			UIManager.Instance.Update();
		}

		public void LateUpdate()
		{
		}
	}
}
