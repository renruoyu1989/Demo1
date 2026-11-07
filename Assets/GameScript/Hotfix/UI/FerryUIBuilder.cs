using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hotfix
{
	/// <summary>
	/// 过河界面的代码化 UI 构建工具
	/// 说明：窗口 prefab 只保留 Canvas + GraphicRaycaster + UIManifest 结构，
	/// 具体控件在 OnCreate 里用本工具生成，避免手写 prefab 的节点路径
	/// </summary>
	public static class FerryUIBuilder
	{
		public static readonly Color ColorBackdrop = new Color(0.055f, 0.07f, 0.11f, 1f);
		public static readonly Color ColorPanel = new Color(0.13f, 0.16f, 0.22f, 0.96f);
		public static readonly Color ColorButton = new Color(0.21f, 0.26f, 0.34f, 1f);
		public static readonly Color ColorAccent = new Color(0.96f, 0.72f, 0.25f, 1f);
		public static readonly Color ColorText = new Color(0.94f, 0.95f, 0.97f, 1f);
		public static readonly Color ColorTextDim = new Color(0.66f, 0.70f, 0.76f, 1f);

		public static Font Font { get { return UIFontProvider.DefaultFont; } }

		/// <summary>铺满父节点的容器</summary>
		public static RectTransform CreateFullScreen(Transform parent, string name)
		{
			GameObject go = new GameObject(name, typeof(RectTransform));
			RectTransform rect = go.GetComponent<RectTransform>();
			rect.SetParent(parent, false);
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			rect.localScale = Vector3.one;
			return rect;
		}

		/// <summary>创建一个定尺寸、以中心定位的节点</summary>
		public static RectTransform CreateRect(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
		{
			GameObject go = new GameObject(name, typeof(RectTransform));
			RectTransform rect = go.GetComponent<RectTransform>();
			rect.SetParent(parent, false);
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = size;
			rect.anchoredPosition = anchoredPos;
			rect.localScale = Vector3.one;
			return rect;
		}

		/// <summary>全屏纯色背景</summary>
		public static Image CreateBackground(Transform parent, Color color)
		{
			RectTransform rect = CreateFullScreen(parent, "Background");
			Image image = rect.gameObject.AddComponent<Image>();
			image.color = color;
			image.raycastTarget = true;
			return image;
		}

		/// <summary>面板底色</summary>
		public static Image CreatePanel(Transform parent, Vector2 anchoredPos, Vector2 size, Color color)
		{
			RectTransform rect = CreateRect(parent, "Panel", anchoredPos, size);
			Image image = rect.gameObject.AddComponent<Image>();
			image.color = color;
			return image;
		}

		/// <summary>HUD 上的半透明信息条：本身不吃点击，只有条上的按钮拦截</summary>
		public static Image CreateBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
		{
			RectTransform rect = CreateRect(parent, name, anchoredPos, size);
			Image image = rect.gameObject.AddComponent<Image>();
			image.color = color;
			image.raycastTarget = false;
			return image;
		}

		/// <summary>船位圆点（方形近似，够小即可）</summary>
		public static Image CreateDot(Transform parent, string name, Vector2 anchoredPos, float size, Color color)
		{
			RectTransform rect = CreateRect(parent, name, anchoredPos, new Vector2(size, size));
			Image image = rect.gameObject.AddComponent<Image>();
			image.color = color;
			image.raycastTarget = false;
			return image;
		}

		/// <summary>
		/// 画一条线段（教程箭头）。from/to 是同一父节点下的画布坐标，pivot 居中旋转。
		/// </summary>
		public static Image CreateLine(Transform parent, string name, Vector2 from, Vector2 to, float thickness, Color color)
		{
			Vector2 delta = to - from;
			RectTransform rect = CreateRect(parent, name, (from + to) * 0.5f, new Vector2(delta.magnitude, thickness));
			rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
			Image image = rect.gameObject.AddComponent<Image>();
			image.color = color;
			image.raycastTarget = false;
			return image;
		}

		/// <summary>给浅色底板上的文字加一圈描边，保证在棋盘背景上可读</summary>
		public static void AddOutline(Text text, Color color, Vector2 distance)
		{
			if (text == null)
				return;
			Outline outline = text.gameObject.AddComponent<Outline>();
			outline.effectColor = color;
			outline.effectDistance = distance;
		}

		public static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Color color, Vector2 anchoredPos, Vector2 size)
		{
			RectTransform rect = CreateRect(parent, name, anchoredPos, size);
			Text text = rect.gameObject.AddComponent<Text>();
			text.font = Font;
			text.text = content;
			text.fontSize = fontSize;
			text.alignment = anchor;
			text.color = color;
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Overflow;
			text.raycastTarget = false;
			return text;
		}

		/// <summary>创建一个按钮</summary>
		public static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Action onClick, int fontSize = 30)
		{
			RectTransform rect = CreateRect(parent, "Button_" + label, anchoredPos, size);

			Image image = rect.gameObject.AddComponent<Image>();
			image.color = ColorButton;

			Button button = rect.gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			ColorBlock colors = button.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
			colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
			button.colors = colors;

			if (onClick != null)
				button.onClick.AddListener(() => onClick());

			CreateText(rect, "Label", label, fontSize, TextAnchor.MiddleCenter, ColorText, Vector2.zero, size);
			return button;
		}

		/// <summary>小号着色按钮（用于模式页签等）</summary>
		public static Button CreateSmallButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Color color, Action onClick, int fontSize = 22)
		{
			Button button = CreateButton(parent, label, anchoredPos, size, onClick, fontSize);
			Image image = button.targetGraphic as Image;
			if (image != null)
				image.color = color;
			return button;
		}

		/// <summary>修改按钮文字</summary>
		public static void SetButtonLabel(Button button, string label)
		{
			if (button == null)
				return;

			Text text = button.GetComponentInChildren<Text>();
			if (text != null)
				text.text = label;
		}

		/// <summary>设置按钮底色</summary>
		public static void SetButtonColor(Button button, Color color)
		{
			if (button == null)
				return;

			Image image = button.targetGraphic as Image;
			if (image != null)
				image.color = color;
		}
	}
}
