
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
			UIManager.Instance.OpenWindow(EWindowType.UIMenu);
		}

		public override void Execute()
		{
		}

		public override void Exit()
		{
			UIManager.Instance.CloseWindow(EWindowType.UIMenu);
			UIManager.Instance.CloseWindow(EWindowType.UIHelp);
			UIManager.Instance.CloseWindow(EWindowType.UIOptions);
		}
	}
}
