
using MotionFramework.Window;

namespace Hotfix
{
	/// <summary>
	/// 主菜单状态
	/// </summary>
	public class HotfixStateMenu : HotfixFsmState
	{
		public HotfixStateMenu(EHotfixStateType type) : base(type)
		{
		}

		public override void Enter()
		{
			WindowManager.Instance.OpenWindow<UIMenu>();
		}

		public override void Execute()
		{
		}

		public override void Exit()
		{
			WindowManager.Instance.CloseWindow<UIMenu>();
			WindowManager.Instance.CloseWindow<UIHelp>();
			WindowManager.Instance.CloseWindow<UIOptions>();
		}
	}
}
