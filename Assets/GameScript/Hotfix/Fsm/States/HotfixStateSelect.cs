
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
			UIManager.Instance.OpenWindow(EWindowType.UISelect, FerryGameCommands.PendingSelectMode);
		}

		public override void Execute()
		{
		}

		public override void Exit()
		{
			UIManager.Instance.CloseWindow(EWindowType.UISelect);
		}
	}
}
