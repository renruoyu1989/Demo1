//--------------------------------------------------
// Motion Framework
// Copyright©2019-2020 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using MotionFramework.Utility;
using UnityEngine;

namespace MotionFramework.Console
{
	public static class ConsoleGUI
	{
		private static bool _initGlobalStyle = false;

		public static GUIStyle HorizontalScrollbarStyle { private set; get; }
		public static GUIStyle HorizontalScrollbarThumbStyle { private set; get; }
		public static GUIStyle VerticalScrollbarStyle { private set; get; }
		public static GUIStyle VerticalScrollbarThumbStyle { private set; get; }
		public static GUIStyle XStyle { private set; get; }
		public static GUIStyle ToolbarStyle { private set; get; }
		public static GUIStyle ButtonStyle { private set; get; }
		public static GUIStyle ToogleStyle1 { private set; get; }
		public static GUIStyle ToogleStyle2 { private set; get; }
		public static GUIStyle TextFieldStyle { private set; get; }
		public static GUIStyle LableStyle { private set; get; }
		public static GUIStyle RichLabelStyle { private set; get; }
		public static GUIStyle WhiteLabelStyle { private set; get; }
		public static GUIStyle RedLabelStyle { private set; get; }
		public static GUIStyle YellowLabelStyle { private set; get; }
		public static int RichLabelFontSize { private set; get; }

		private static GUIStyle _cachedHorizontalScrollbarThumb;
		private static GUIStyle _cachedVerticalScrollbarThumb;

		/// <summary>
		/// 创建一些高度和字体大小固定的控件样式
		/// 控制台的标准分辨率为 : 1920X1080
		/// </summary>
		internal static void InitGlobalStyle()
		{
			if (_initGlobalStyle == false)
			{
				_initGlobalStyle = true;

				float scale;
				if (Screen.height > Screen.width)
				{
					// 竖屏Portrait
					scale = Screen.width / 1080f;
				}
				else
				{
					// 横屏Landscape
					scale = Screen.width / 1920f;
				}

				HorizontalScrollbarStyle = new GUIStyle(GUI.skin.horizontalScrollbar);
				HorizontalScrollbarStyle.fixedHeight = (int)(30 * scale);
				HorizontalScrollbarThumbStyle = new GUIStyle(GUI.skin.horizontalScrollbarThumb);
				HorizontalScrollbarThumbStyle.fixedHeight = (int)(30 * scale);

				VerticalScrollbarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
				VerticalScrollbarStyle.fixedWidth = (int)(30 * scale);
				VerticalScrollbarThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb);
				VerticalScrollbarThumbStyle.fixedWidth = (int)(30 * scale);

				XStyle = new GUIStyle(GUI.skin.button);
				XStyle.fontSize = (int)(38 * scale);
				XStyle.fixedWidth = (int)(60 * scale);
				XStyle.fixedHeight = (int)(60 * scale);

				ToolbarStyle = new GUIStyle(GUI.skin.button);
				ToolbarStyle.fontSize = (int)(28 * scale);
				ToolbarStyle.fixedHeight = (int)(40 * scale);

				ButtonStyle = new GUIStyle(GUI.skin.button);
				ButtonStyle.fontSize = (int)(28 * scale);
				ButtonStyle.fixedHeight = (int)(40 * scale);

				ToogleStyle1 = new GUIStyle(GUI.skin.button);
				ToogleStyle1.fontSize = (int)(26 * scale);
				ToogleStyle1.fixedHeight = (int)(35 * scale);

				ToogleStyle2 = new GUIStyle(GUI.skin.box);
				ToogleStyle2.fontSize = (int)(26 * scale);
				ToogleStyle2.fixedHeight = (int)(35 * scale);

				TextFieldStyle = new GUIStyle(GUI.skin.textField);
				TextFieldStyle.fontSize = (int)(22 * scale);
				TextFieldStyle.fixedHeight = (int)(30 * scale);

				LableStyle = new GUIStyle(GUI.skin.label);
				LableStyle.fontSize = (int)(24 * scale);

				// 注意：不能直接改 GUIStyle.none，那是全局共享实例
				RichLabelStyle = new GUIStyle(GUI.skin.label);
				RichLabelStyle.richText = true;
				RichLabelStyle.fontSize = (int)(24 * scale);
				RichLabelFontSize = (int)(24 * scale);

				// 预先构建不同颜色的样式，避免每次输出都拼接富文本字符串（控制台打开时每帧都会产生 GC）
				WhiteLabelStyle = new GUIStyle(RichLabelStyle);
				WhiteLabelStyle.normal.textColor = Color.white;
				RedLabelStyle = new GUIStyle(RichLabelStyle);
				RedLabelStyle.normal.textColor = Color.red;
				YellowLabelStyle = new GUIStyle(RichLabelStyle);
				YellowLabelStyle.normal.textColor = Color.yellow;
			}
		}

		public static Vector2 BeginScrollView(Vector2 pos, float offset = 0f)
		{
			// 设置滑动条皮肤
			_cachedHorizontalScrollbarThumb = GUI.skin.horizontalScrollbarThumb;
			_cachedVerticalScrollbarThumb = GUI.skin.verticalScrollbarThumb;
			GUI.skin.horizontalScrollbarThumb = HorizontalScrollbarThumbStyle;
			GUI.skin.verticalScrollbarThumb = VerticalScrollbarThumbStyle;

			float scrollWidth = Screen.safeArea.width - VerticalScrollbarStyle.fixedWidth;
			float scrollHeight = Screen.safeArea.height - ToolbarStyle.fixedHeight * 2 - offset;
			return GUILayout.BeginScrollView(pos, HorizontalScrollbarStyle, VerticalScrollbarStyle, GUILayout.Width(scrollWidth), GUILayout.Height(scrollHeight));
		}
		public static void EndScrollView()
		{
			GUILayout.EndScrollView();

			// 还原滑动条皮肤
			GUI.skin.horizontalScrollbarThumb = _cachedHorizontalScrollbarThumb;
			GUI.skin.verticalScrollbarThumb = _cachedVerticalScrollbarThumb;
		}
		public static bool Toggle(string name, bool checkFlag)
		{
			GUIStyle style = checkFlag ? ToogleStyle1 : ToogleStyle2;
			if (GUILayout.Button(name, style))
			{
				checkFlag = !checkFlag;
			}
			return checkFlag;
		}
		public static void Lable(string text)
		{
			GUILayout.Label(text, WhiteLabelStyle);
		}
		public static void RedLable(string text)
		{
			GUILayout.Label(text, RedLabelStyle);
		}
		public static void YellowLable(string text)
		{
			GUILayout.Label(text, YellowLabelStyle);
		}
	}
}