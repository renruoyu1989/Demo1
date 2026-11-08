using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using MotionFramework.Window;

using MotionFramework.Data;

namespace Hotfix
{
	/// <summary>主菜单：经典 / 连击入口、设置、帮助、退出与通关进度</summary>
	[Window((int)EWindowLayer.Panel, true)]
	public class UIMenu : WindowBase
	{
		private Text _progress;

		public override void OnCreate()
		{
			RectTransform root = FerryUIBuilder.CreateFullScreen(Go.transform, "Root");
			FerryUIBuilder.CreateBackground(root, FerryUIBuilder.ColorBackdrop);

			FerryUIBuilder.CreateText(root, "Title", "FERRYMAN", 96, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorAccent, new Vector2(0f, 265f), new Vector2(1200f, 130f));

			FerryUIBuilder.CreateText(root, "SubTitle", "A river crossing puzzle", 28, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorTextDim, new Vector2(0f, 185f), new Vector2(1200f, 50f));

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Classic"), new Vector2(0f, 90f),
				new Vector2(420f, 72f), () => OpenSelect(0));

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_ComboMode"), new Vector2(0f, 5f),
				new Vector2(420f, 72f), () => OpenSelect(1));

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Options"), new Vector2(0f, -80f),
				new Vector2(420f, 72f), () => WindowManager.Instance.OpenWindow<UIOptions>());

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Help"), new Vector2(0f, -165f),
				new Vector2(420f, 72f), () => WindowManager.Instance.OpenWindow<UIHelp>());

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Quit"), new Vector2(0f, -250f),
				new Vector2(420f, 64f), () => Application.Quit(), 24);

			_progress = FerryUIBuilder.CreateText(root, "Progress", string.Empty, 24, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorTextDim, new Vector2(0f, -350f), new Vector2(1200f, 60f));
		}

		private void OpenSelect(int mode)
		{
			FerryGameCommands.GoSelectMode(mode);
		}

		public override void OnDestroy()
		{
		}

		public override void OnRefresh()
		{
			if (_progress == null)
				return;

			DataFerryProfile data = DataManager.Instance.GetData<DataFerryProfile>();
			List<int> keys = CfgLevel.Instance.GetKeys();
			int cleared = data != null ? CountCleared(data, keys) : 0;
			int score = data != null ? data.TotalScore : 0;

			_progress.text = string.Format("{0} {1}/{2}     {3} {4}",
				LocalizationManager.Get("Ferry_Cleared"), cleared, keys.Count,
				LocalizationManager.Get("Ferry_Total"), score);
		}

		private static int CountCleared(DataFerryProfile data, List<int> keys)
		{
			int count = 0;
			for (int i = 0; i < keys.Count; i++)
			{
				if (data.IsCleared(keys[i]))
					count++;
			}
			return count;
		}

		public override void OnUpdate()
		{
		}
	}
}
