
using MotionFramework.Window;

namespace Hotfix
{
	/// <summary>
	/// 关卡选择状态
	/// </summary>
	public class HotfixStateSelect : HotfixFsmState
	{
		public HotfixStateSelect(EHotfixStateType type) : base(type)
		{
		}

		public override void Enter()
		{
			WindowManager.Instance.OpenWindow<UISelect>(FerryGameCommands.PendingSelectMode);
		}

		public override void Execute()
		{
		}

		public override void Exit()
		{
			WindowManager.Instance.CloseWindow<UISelect>();
		}
	}
}
