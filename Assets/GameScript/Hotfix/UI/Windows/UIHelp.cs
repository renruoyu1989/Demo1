using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Hotfix
{
	/// <summary>帮助说明：操作三步、胜负与计分、连击模式、快捷键，文案走多语言表</summary>
	[Window(EWindowType.UIHelp, EWindowLayer.Panel)]
	public class UIHelp : UIWindow
	{
		private Text _body;

		public override void OnCreate()
		{
			RectTransform root = FerryUIBuilder.CreateFullScreen(Go.transform, "Root");
			FerryUIBuilder.CreateBackground(root, new Color(0f, 0f, 0f, 0.68f));
			FerryUIBuilder.CreatePanel(root, new Vector2(0f, 10f), new Vector2(940f, 760f), FerryUIBuilder.ColorPanel);

			FerryUIBuilder.CreateText(root, "Title", LocalizationManager.Get("Ferry_Help"), 42, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorAccent, new Vector2(0f, 330f), new Vector2(860f, 70f));

			_body = UIRules.CreateScrollableText(root, new Vector2(0f, 10f), new Vector2(840f, 560f), 24);
			_body.text = BuildHelpText();

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Close"), new Vector2(0f, -330f),
				new Vector2(260f, 64f), () => UIManager.Instance.CloseWindow(EWindowType.UIHelp));
		}

		private static string BuildHelpText()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 1; i <= 5; i++)
				sb.Append(LocalizationManager.Get("Ferry_HelpStep" + i)).Append("\n\n");

			sb.Append("<b>").Append(LocalizationManager.Get("Ferry_ComboMode")).Append("</b>\n\n");
			sb.Append(LocalizationManager.Get("Ferry_HelpCombo"));
			return sb.ToString();
		}

		public override void OnDestroy()
		{
		}

		public override void OnRefresh()
		{
			if (_body != null)
				_body.text = BuildHelpText();
		}

		public override void OnUpdate()
		{
		}
	}
}
