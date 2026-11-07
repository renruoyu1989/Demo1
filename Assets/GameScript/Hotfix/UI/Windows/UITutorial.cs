using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hotfix
{
	/// <summary>
	/// 教程气泡覆盖层。坐标严格按原版 1000×562 设计稿（Y 向下）：
	/// 气泡文本左上角命中 (x, y)，箭头从 (ax1, ay1) 指向 (ax2, ay2)，
	/// 全部通过棋盘相机投影到 UI 画布，并在相机横移时实时跟随。
	/// 点击任意处进入下一步。
	/// </summary>
	[Window(EWindowType.UITutorial, EWindowLayer.Guide)]
	public class UITutorial : UIWindow
	{
		private static readonly Color BubbleColor = new Color(0.99f, 0.97f, 0.90f, 0.97f);
		private static readonly Color BubbleTextColor = new Color(0.20f, 0.13f, 0.07f, 1f);
		private static readonly Color ArrowColor = new Color(0.99f, 0.86f, 0.40f, 0.95f);

		private RectTransform _layer;
		private RectTransform _canvasRect;
		private Camera _uiCamera;
		private Text _counter;
		private List<CfgTutorialStepTab> _steps = new List<CfgTutorialStepTab>();
		private readonly List<RectTransform> _follow = new List<RectTransform>();
		private readonly List<Vector3> _followWorld = new List<Vector3>();
		private int _index;

		public override void OnCreate()
		{
			RectTransform root = FerryUIBuilder.CreateFullScreen(Go.transform, "Root");
			_canvasRect = Go.transform as RectTransform;
			_uiCamera = UIManager.Instance.GetUICamera();

			// 全屏遮罩同时充当「点击任意处继续」的按钮，并拦住棋盘点击
			Image blockerImage = FerryUIBuilder.CreateBackground(root, new Color(0f, 0f, 0f, 0.32f));
			Button blockerButton = blockerImage.gameObject.AddComponent<Button>();
			blockerButton.targetGraphic = blockerImage;
			blockerButton.onClick.AddListener(Next);
			blockerImage.transform.SetAsFirstSibling();

			_layer = FerryUIBuilder.CreateFullScreen(root, "TutorialLayer");

			// 放在顶部条下方，避开 HUD 底部操作条与提示条
			FerryUIBuilder.CreateBar(root, "CounterBar", new Vector2(0f, 375f),
				new Vector2(620f, 48f), new Color(0.04f, 0.06f, 0.10f, 0.62f));
			_counter = FerryUIBuilder.CreateText(root, "Counter", string.Empty, 24, TextAnchor.MiddleCenter,
				new Color(1f, 0.95f, 0.85f, 1f), new Vector2(0f, 375f), new Vector2(900f, 46f));
			FerryUIBuilder.AddOutline(_counter, new Color(0f, 0f, 0f, 0.7f), new Vector2(1.5f, -1.5f));
		}

		public override void OnDestroy()
		{
			_steps.Clear();
			_follow.Clear();
			_followWorld.Clear();
		}

		public override void OnRefresh()
		{
			_steps = FerryConfigService.Instance.GetTutorialSteps(FerryGameFlow.Instance.LevelId);
			_index = 0;

			if (_steps.Count == 0)
			{
				UIManager.Instance.CloseWindow(EWindowType.UITutorial);
				return;
			}

			Show(0);
		}

		public override void OnUpdate()
		{
			// 棋盘相机会随船横移，气泡与箭头必须每帧重新投影
			Camera boardCamera = FerryBoardView.ActiveCamera;
			if (boardCamera == null || _canvasRect == null)
				return;

			for (int i = 0; i < _follow.Count; i++)
			{
				if (_follow[i] != null)
					_follow[i].anchoredPosition = ToCanvas(boardCamera, _followWorld[i]);
			}
		}

		private void Next()
		{
			_index++;
			if (_index >= _steps.Count)
			{
				UIManager.Instance.CloseWindow(EWindowType.UITutorial);
				return;
			}

			Show(_index);
		}

		#region 单步构建
		private void Show(int index)
		{
			ClearArt();

			CfgTutorialStepTab step = _steps[index];
			string text = step.Text;
			if (string.IsNullOrEmpty(text))
				return;

			float unit = Unit();
			BuildArrow(step, unit);
			BuildCallout(step, text, unit);

			_counter.text = string.Format("{0}   {1}/{2}",
				LocalizationManager.Get("Ferry_TutorialNext"), index + 1, _steps.Count);
		}

		private void BuildArrow(CfgTutorialStepTab step, float unit)
		{
			Vector2 from = new Vector2(step.Ax1, step.Ay1);
			Vector2 to = new Vector2(step.Ax2, step.Ay2);
			Vector2 delta = new Vector2((to.x - from.x) * unit, -(to.y - from.y) * unit);
			if (delta.sqrMagnitude < 0.01f)
				return;

			Vector2 dir = delta.normalized;
			float len = delta.magnitude;
			RectTransform arrowRoot = FerryUIBuilder.CreateRect(_layer, "Arrow", Vector2.zero, Vector2.zero);
			Follow(arrowRoot, DesignToWorld(from));

			FerryUIBuilder.CreateLine(arrowRoot, "Shaft", Vector2.zero,
				delta - dir * Mathf.Min(24f * unit, len * 0.3f), 4f * unit, ArrowColor);

			Vector2 back = -dir;
			for (int i = -1; i <= 1; i += 2)
			{
				float a = 28f * Mathf.Deg2Rad * i;
				float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
				Vector2 side = new Vector2(back.x * ca - back.y * sa, back.x * sa + back.y * ca);
				FerryUIBuilder.CreateLine(arrowRoot, "Wing", delta, delta + side * 18f * unit, 4f * unit, ArrowColor);
			}
		}

		private void BuildCallout(CfgTutorialStepTab step, string text, float unit)
		{
			const float padX = 16f, padY = 11f;
			float width = step.Width > 0f ? step.Width : 300f;

			RectTransform bubble = FerryUIBuilder.CreateRect(_layer, "Bubble", Vector2.zero, Vector2.zero);
			Image background = bubble.gameObject.AddComponent<Image>();
			background.color = BubbleColor;
			background.raycastTarget = false;

			Text body = FerryUIBuilder.CreateText(bubble, "Body", text,
				Mathf.RoundToInt(15f * unit),
				step.Center == 1 ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft,
				BubbleTextColor, Vector2.zero, new Vector2(width * unit, 10f));

			float textHeight = Mathf.Max(body.preferredHeight, 18f * unit);
			body.rectTransform.sizeDelta = new Vector2(width * unit, textHeight);
			bubble.sizeDelta = new Vector2(width * unit + 2f * padX * unit, textHeight + 2f * padY * unit);

			// 文本左上角命中 (x, y)：气泡中心再往右下挪半个气泡
			float bubbleW = width + 2f * padX;
			float bubbleH = textHeight / unit + 2f * padY;
			float centerX = step.X + bubbleW * 0.5f;
			float centerY = step.Y + bubbleH * 0.5f;

			if (step.Center == 1)
			{
				Camera cam = FerryBoardView.ActiveCamera;
				centerX = cam != null ? cam.transform.position.x : FerryDefine.DesignWidth * 0.5f;
				centerY = FerryDefine.DesignHeight * 0.5f;
			}

			Follow(bubble, DesignToWorld(new Vector2(centerX, centerY)));
		}

		private void ClearArt()
		{
			_follow.Clear();
			_followWorld.Clear();
			if (_layer == null)
				return;

			for (int i = _layer.childCount - 1; i >= 0; i--)
				Object.Destroy(_layer.GetChild(i).gameObject);
		}

		private void Follow(RectTransform rect, Vector3 world)
		{
			_follow.Add(rect);
			_followWorld.Add(world);
			Camera cam = FerryBoardView.ActiveCamera;
			if (cam != null)
				rect.anchoredPosition = ToCanvas(cam, world);
		}
		#endregion

		#region 坐标换算
		/// <summary>设计稿像素 → 世界单位（与 FerryBoardAnchor.ToWorld 一致）</summary>
		private static Vector3 DesignToWorld(Vector2 design)
		{
			return new Vector3(design.x, FerryDefine.DesignHeight - design.y, 0f);
		}

		/// <summary>1 设计像素在 UI 画布上的单位数</summary>
		private float Unit()
		{
			Camera cam = FerryBoardView.ActiveCamera;
			if (cam == null || Go == null)
				return 1f;

			Canvas canvas = Go.GetComponent<Canvas>();
			float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
			return Screen.height / Mathf.Max(1f, cam.orthographicSize * 2f) / scale;
		}

		private Vector2 ToCanvas(Camera boardCamera, Vector3 world)
		{
			Vector3 screen = boardCamera.WorldToScreenPoint(world);
			Vector2 local;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, _uiCamera, out local))
				return local;
			return Vector2.zero;
		}
		#endregion
	}
}
