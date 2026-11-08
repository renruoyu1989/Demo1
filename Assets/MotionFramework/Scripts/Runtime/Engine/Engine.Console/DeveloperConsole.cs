//--------------------------------------------------
// Motion Framework
// Copyright©2019-2020 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionFramework.Console
{
	/// <summary>
	/// 控制台
	/// </summary>
	public static class DeveloperConsole
	{
		private class WindowWrapper : IComparer<WindowWrapper>, IComparable<WindowWrapper>
		{
			public string Title;
			public int Priority;
			public IConsoleWindow Instance;

			public int CompareTo(WindowWrapper other)
			{
				return Compare(this, other);
			}
			public int Compare(WindowWrapper a, WindowWrapper b)
			{
				return a.Priority.CompareTo(b.Priority);
			}
		}

		/// <summary>
		/// 控制台节点列表
		/// </summary>
		private readonly static List<WindowWrapper> _wrappers = new List<WindowWrapper>();

		// GUI相关
		private static bool _visible = false;
		private static int _showIndex = 0;
		private static Texture _bgTexture;
		private static string[] _toolbarTitles;

		/// <summary>
		/// 初始化控制台：窗口直接静态注册，不再扫程序集找特性
		/// </summary>
		public static void Initialize()
		{
			// 加载背景纹理
			string textureName = "console_background";
			_bgTexture = Resources.Load<Texture>(textureName);
			if (_bgTexture == null)
				UnityEngine.Debug.LogWarning($"Not found {textureName} texture in Resources folder.");

			_wrappers.Clear();
			Register("游戏模块", 100, new GameModuleWindow());
			Register("游戏日志", 101, new GameLogWindow());
			Register("应用详情", 102, new ApplicationWindow());
			Register("引用池", 103, new ReferencePoolWindow());
			Register("游戏对象池", 105, new GameObjectPoolWindow());

			// 根据优先级排序
			_wrappers.Sort();

			// 标题列表
			List<string> titles = new List<string>();
			for (int i = 0; i < _wrappers.Count; i++)
			{
				titles.Add(_wrappers[i].Title);
			}
			_toolbarTitles = titles.ToArray();
		}

		private static void Register(string title, int priority, IConsoleWindow instance)
		{
			_wrappers.Add(new WindowWrapper()
			{
				Title = title,
				Priority = priority,
				Instance = instance,
			});
		}

		/// <summary>
		/// 绘制控制台
		/// 注意：该接口必须在OnGUI函数内调用
		/// </summary>
		public static void Draw()
		{
			// 注意：GUI接口只能在OnGUI内部使用
			ConsoleGUI.InitGlobalStyle();

			float posX = Screen.safeArea.x;
			float posY = Screen.height - Screen.safeArea.height - Screen.safeArea.y;

			if (_visible == false)
			{
				// 显示按钮
				if (GUI.Button(new Rect(posX + 20, posY + 20, ConsoleGUI.XStyle.fixedWidth, ConsoleGUI.XStyle.fixedHeight), "X", ConsoleGUI.XStyle))
					_visible = true;
			}
			else
			{
				Rect windowRect = new Rect(posX, posY, Screen.safeArea.width, Screen.safeArea.height);
				GUI.Window(0, windowRect, DrawWindow, string.Empty);
			}
		}
		private static void DrawWindow(int windowID)
		{
			// 绘制背景
			if (_visible && _bgTexture != null)
				GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bgTexture, ScaleMode.StretchToFill, true);

			GUILayout.BeginHorizontal();
			{
				// 隐藏按钮
				if (GUILayout.Button("X", ConsoleGUI.ButtonStyle, GUILayout.Width(ConsoleGUI.ButtonStyle.fixedHeight)))
					_visible = false;

				// 绘制按钮栏
				_showIndex = GUILayout.Toolbar(_showIndex, _toolbarTitles, ConsoleGUI.ToolbarStyle);
			}
			GUILayout.EndHorizontal();

			// 绘制选中窗口
			for (int i = 0; i < _wrappers.Count; i++)
			{
				if (_showIndex != i)
					continue;
				WindowWrapper wrapper = _wrappers[i];
				wrapper.Instance.OnGUI();
			}
		}
	}
}