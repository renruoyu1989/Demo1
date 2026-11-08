using MotionFramework.Window;

namespace Hotfix
{
	/// <summary>
	/// 游戏业务逻辑入口：由 GameLauncher 在资源系统就绪后直接静态调用
	/// </summary>
	public sealed class HotfixMain
	{
		public static readonly HotfixMain Instance = new HotfixMain();

		public void Start()
		{
			HotfixLogger.Log("game logic start");

			AudioPlayerSetting.InitAudioSetting();

			// 注意：DataManager / WindowManager / ConfigManager 由 MotionEngine 统一创建与轮询

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
		}
	}
}
