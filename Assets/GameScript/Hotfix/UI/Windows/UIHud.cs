using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using MotionFramework.Window;

namespace Hotfix
{
	/// <summary>
	/// 关卡内 HUD。对齐原版操作界面：顶部信息条（关卡 / 码头 / 船位 / 计时 / 功能键）、
	/// 底部操作条（左右划船 / 船位指示 / 遣散 / 等待队列）、中央提示与常驻操作说明。
	/// 所有玩法操作只发意图给 FerryGameCommands，不直接改逻辑层。
	/// </summary>
	[Window((int)EWindowLayer.HUD, false)]
	public class UIHud : WindowBase
	{
		private const int MaxBoatDots = 6;

		private Text _levelText;
		private Text _dockText;
		private Text _timerText;
		private Text _boatText;
		private Text _boatLabel;
		private Text _queueText;
		private Text _toastText;
		private Text _tipText;
		private GameObject _toastBox;
		private Button _dismissButton;
		private Button _leftButton;
		private Button _rightButton;
		private readonly List<Image> _boatDots = new List<Image>();
		private readonly List<KeyValuePair<Button, string>> _labelButtons = new List<KeyValuePair<Button, string>>();
		private float _toastLife;

		public override void OnCreate()
		{
			RectTransform root = FerryUIBuilder.CreateFullScreen(Go.transform, "Root");

			BuildTopBar(root);
			BuildBottomBar(root);
			BuildToast(root);

			FerryUIBuilder.CreateBar(root, "TipBar", new Vector2(0f, -400f),
				new Vector2(1200f, 42f), new Color(0.04f, 0.06f, 0.10f, 0.55f));
			_tipText = FerryUIBuilder.CreateText(root, "OperationTip",
				LocalizationManager.Get("Ferry_OperationTip"), 22, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorText, new Vector2(0f, -400f), new Vector2(1200f, 40f));
			FerryUIBuilder.AddOutline(_tipText, new Color(0f, 0f, 0f, 0.7f), new Vector2(1f, -1f));

			AddEventListener(HotfixEventMessageTag.FerryEvent, Handle_FerryEvent);
			RefreshHud();
		}

		#region 构建
		private void BuildTopBar(RectTransform root)
		{
			Image bar = FerryUIBuilder.CreateBar(root, "TopBar", new Vector2(0f, 490f),
				new Vector2(1900f, 92f), new Color(0.04f, 0.06f, 0.10f, 0.74f));

			// 左侧留出屏幕左上角常驻的调试按钮空间，别让关卡名被压住
			_levelText = FerryUIBuilder.CreateText(bar.transform, "Level", string.Empty, 26, TextAnchor.MiddleLeft,
				FerryUIBuilder.ColorText, new Vector2(-630f, 16f), new Vector2(380f, 42f));

			_dockText = FerryUIBuilder.CreateText(bar.transform, "Dock", string.Empty, 20, TextAnchor.MiddleLeft,
				FerryUIBuilder.ColorTextDim, new Vector2(-630f, -20f), new Vector2(380f, 34f));

			_timerText = FerryUIBuilder.CreateText(bar.transform, "Timer", "00 : 00", 32, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorAccent, new Vector2(-290f, 0f), new Vector2(280f, 60f));

			_boatText = FerryUIBuilder.CreateText(bar.transform, "Boat", string.Empty, 24, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorText, new Vector2(-60f, 0f), new Vector2(260f, 60f));

			float[] xs = { 540f, 654f, 768f, 882f };
			MakeTopButton(bar.transform, "Ferry_Rules", xs[0], () => WindowManager.Instance.OpenWindow<UIRules>());
			MakeTopButton(bar.transform, "Ferry_Hint", xs[1], ShowHint);
			MakeTopButton(bar.transform, "Ferry_Help", xs[2], () => WindowManager.Instance.OpenWindow<UIHelp>());
			MakeTopButton(bar.transform, "Ferry_Retry", xs[3], Restart);

			// 第五个功能键放在顶部条右上角外部，避免挤占按钮间距
			Button selectButton = FerryUIBuilder.CreateSmallButton(root, string.Empty,
				new Vector2(882f, 386f), new Vector2(104f, 50f), FerryUIBuilder.ColorButton,
				GoSelect, 20);
			RegisterLabel(selectButton, "Ferry_Select");
		}

		private Button MakeTopButton(Transform parent, string key, float x, Action onClick)
		{
			Button button = FerryUIBuilder.CreateSmallButton(parent, string.Empty,
				new Vector2(x, 0f), new Vector2(104f, 56f), FerryUIBuilder.ColorButton, onClick, 22);
			RegisterLabel(button, key);
			return button;
		}

		private void RegisterLabel(Button button, string key)
		{
			_labelButtons.Add(new KeyValuePair<Button, string>(button, key));
			RefreshLabel(button, key);
		}

		/// <summary>切换语言后静态标签也要跟着变（动态文本在 RefreshHud 里刷新）</summary>
		private void RefreshLabels()
		{
			for (int i = 0; i < _labelButtons.Count; i++)
				RefreshLabel(_labelButtons[i].Key, _labelButtons[i].Value);

			if (_boatLabel != null)
				_boatLabel.text = LocalizationManager.Get("Ferry_Boat");
			if (_tipText != null)
				_tipText.text = LocalizationManager.Get("Ferry_OperationTip");
		}

		private static void RefreshLabel(Button button, string key)
		{
			if (button == null)
				return;

			FerryUIBuilder.SetButtonLabel(button, LocalizationManager.Get(key));
			Text label = button.GetComponentInChildren<Text>();
			if (label != null)
				label.fontSize = LocalizationManager.Language == ELanguage.ChineseSimplified ? 20 : 15;
		}

		private void BuildBottomBar(RectTransform root)
		{
			Image bar = FerryUIBuilder.CreateBar(root, "BottomBar", new Vector2(0f, -470f),
				new Vector2(1900f, 108f), new Color(0.04f, 0.06f, 0.10f, 0.74f));

			_leftButton = FerryUIBuilder.CreateButton(bar.transform, "<", new Vector2(-850f, 0f),
				new Vector2(130f, 80f), () => FerryGameCommands.Row(-1), 44);

			_rightButton = FerryUIBuilder.CreateButton(bar.transform, ">", new Vector2(850f, 0f),
				new Vector2(130f, 80f), () => FerryGameCommands.Row(1), 44);

			_boatLabel = FerryUIBuilder.CreateText(bar.transform, "BoatLabel", string.Empty,
				22, TextAnchor.MiddleRight, FerryUIBuilder.ColorTextDim,
				new Vector2(-680f, 0f), new Vector2(120f, 60f));

			for (int i = 0; i < MaxBoatDots; i++)
			{
				Image dot = FerryUIBuilder.CreateDot(bar.transform, "BoatSlot_" + i,
					new Vector2(-590f + i * 34f, 0f), 24f, FerryUIBuilder.ColorButton);
				_boatDots.Add(dot);
			}

			_dismissButton = FerryUIBuilder.CreateButton(bar.transform, string.Empty,
				new Vector2(-180f, 0f), new Vector2(230f, 74f), () => FerryGameCommands.Dismiss(), 26);
			RegisterLabel(_dismissButton, "Ferry_Dismiss");

			_queueText = FerryUIBuilder.CreateText(bar.transform, "Queue", string.Empty, 22, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorText, new Vector2(340f, 0f), new Vector2(680f, 70f));
		}

		private void BuildToast(RectTransform root)
		{
			RectTransform box = FerryUIBuilder.CreateRect(root, "ToastBox", new Vector2(0f, 300f), new Vector2(900f, 76f));
			_toastBox = box.gameObject;
			Image background = box.gameObject.AddComponent<Image>();
			background.color = new Color(0.05f, 0.07f, 0.11f, 0.88f);
			background.raycastTarget = false;

			_toastText = FerryUIBuilder.CreateText(box, "Text", string.Empty, 30, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorAccent, Vector2.zero, new Vector2(880f, 70f));

			_toastBox.SetActive(false);
		}
		#endregion

		#region 生命周期
		public override void OnDestroy()
		{
		}

		public override void OnRefresh()
		{
			RefreshHud();
		}

		public override void OnUpdate()
		{
			FerryGameSession session = FerryGameFlow.Instance.Session;
			if (session != null)
				_timerText.text = session.ElapsedText();

			if (_toastLife > 0f)
			{
				_toastLife -= Time.deltaTime;
				if (_toastLife <= 0f)
					HideToast();
			}

			HandleShortcuts();
		}

		private void HandleShortcuts()
		{
			if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
				FerryGameCommands.Row(-1);
			else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
				FerryGameCommands.Row(1);
			else if (Input.GetKeyDown(KeyCode.R))
				Restart();
			else if (Input.GetKeyDown(KeyCode.H))
				ShowHint();
			else if (Input.GetKeyDown(KeyCode.Escape))
				GoSelect();
		}

		private void Handle_FerryEvent(IHotfixEventMessage message)
		{
			if (message is FerryEvent.SessionChanged)
			{
				RefreshHud();
				return;
			}

			FerryEvent.Toast toast = message as FerryEvent.Toast;
			if (toast != null)
			{
				ShowToast(toast.Message);
			}
		}
		#endregion

		#region 显示
		private void ShowToast(string message)
		{
			if (_toastText == null || string.IsNullOrEmpty(message))
				return;

			_toastText.text = message;
			_toastBox.SetActive(true);
			_toastLife = 2.4f;
		}

		private void HideToast()
		{
			if (_toastBox != null)
				_toastBox.SetActive(false);
		}

		/// <summary>按当前局面给出下一步建议（原版 Hint）</summary>
		private void ShowHint()
		{
			FerryGameSession session = FerryGameFlow.Instance.Session;
			if (session == null)
				return;

			if (session.IsOver)
			{
				ShowToast(LocalizationManager.Get("Ferry_HintRestart"));
				return;
			}

			if (session.Survival && session.DismissNeed > 0 && session.CanDismiss)
			{
				ShowToast(LocalizationManager.Get("Ferry_HintDismiss"));
				return;
			}

			if (session.Boat.Count == 0)
			{
				ShowToast(LocalizationManager.Get("Ferry_HintBoard"));
				return;
			}

			if (session.BoatDock == session.Docks.Count - 1)
			{
				ShowToast(LocalizationManager.Get("Ferry_HintUnload"));
				return;
			}

			ShowToast(LocalizationManager.Get("Ferry_HintRow"));
		}

		private void RefreshHud()
		{
			if (_levelText == null)
				return;

			RefreshLabels();

			FerryGameSession session = FerryGameFlow.Instance.Session;
			if (session == null)
				return;

			CfgLevelTab level = session.Level.Dto;
			string modeName = LocalizationManager.Get(session.Survival ? "Ferry_Combo" : "Ferry_Classic");
			_levelText.text = string.Format("{0}   ·   {1}", level.Code, modeName);
			_dockText.text = string.Format("{0} {1}/{2}", LocalizationManager.Get("Ferry_Dock"),
				session.BoatDock + 1, session.Docks.Count);
			_boatText.text = string.Format("{0}/{1}", session.BoatUsedSpace, session.BoatCapacity);
			_timerText.text = session.ElapsedText();

			int capacity = Mathf.Clamp(session.BoatCapacity, 0, MaxBoatDots);
			for (int i = 0; i < _boatDots.Count; i++)
			{
				bool visible = i < capacity;
				_boatDots[i].gameObject.SetActive(visible);
				if (visible)
				{
					_boatDots[i].color = i < session.BoatUsedSpace
						? FerryUIBuilder.ColorAccent
						: FerryUIBuilder.ColorButton;
				}
			}

			bool survival = session.Survival;
			_dismissButton.gameObject.SetActive(survival);
			if (survival)
				_dismissButton.interactable = session.CanDismiss;

			_queueText.text = survival
				? string.Format("{0} {1}    {2} {3}    {4} ×{5}",
					LocalizationManager.Get("Ferry_Queue"), session.Queue.Count,
					LocalizationManager.Get("Ferry_FerryIn"), session.FerryCount,
					LocalizationManager.Get("Ferry_Dismiss"), session.DismissNeed)
				: string.Format("{0} {1}    {2} {3}",
					LocalizationManager.Get("Ferry_Score"), session.Score,
					LocalizationManager.Get("Ferry_Moves"), session.MoveCount);

			_leftButton.interactable = session.BoatDock > 0;
			_rightButton.interactable = session.BoatDock < session.Docks.Count - 1;
		}
		#endregion

		#region 意图
		private void Restart()
		{
			WindowManager.Instance.CloseWindow<UIResult>();
			FerryGameCommands.Restart();
		}

		private void GoSelect()
		{
			WindowManager.Instance.CloseWindow<UIResult>();
			FerryGameCommands.GoSelect();
		}
		#endregion
	}
}
