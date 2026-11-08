using System;
using System.Collections;
using System.Collections.Generic;

using MotionFramework.Window;

namespace Hotfix
{
	public class HotfixStatePrepare : HotfixFsmState
	{
		private bool _isPreload = false;

		public HotfixStatePrepare(EHotfixStateType type) : base(type)
		{
		}

		public override void Enter()
		{
		}

		public override void Execute()
		{
			if (WindowManager.Instance.IsRootPrepare())
			{
				if (_isPreload == false)
				{
					_isPreload = true;
					WindowManager.Instance.PreloadWindow<UILoading>();
				}

				if (WindowManager.Instance.IsLoading() == false)
				{
					HotfixFsmManager.Instance.ChangeState(EHotfixStateType.Menu);
				}
			}
		}

		public override void Exit()
		{
		}
	}
}