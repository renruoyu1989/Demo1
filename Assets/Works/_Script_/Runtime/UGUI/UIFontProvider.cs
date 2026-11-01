using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 界面字体提供者
/// 注意：演示工程里的UI文本使用的是Unity内置Arial，不包含中文字形，
/// 这里在运行时替换为包含中文的系统字体（工程里放入Resources/UIFont.ttf可覆盖）
/// </summary>
public static class UIFontProvider
{
	private static Font _cachedFont;
	private static bool _isCreated;

	/// <summary>
	/// 默认界面字体
	/// </summary>
	public static Font DefaultFont
	{
		get
		{
			if (_isCreated == false)
			{
				_isCreated = true;
				_cachedFont = CreateFont();
			}
			return _cachedFont;
		}
	}

	/// <summary>
	/// 把界面里使用内置字体的文本替换成可用字体
	/// </summary>
	public static void ApplyFont(GameObject root)
	{
		if (root == null)
			return;

		Font font = DefaultFont;
		if (font == null)
			return;

		Text[] texts = root.GetComponentsInChildren<Text>(true);
		for (int i = 0; i < texts.Length; i++)
		{
			if (IsBuiltinFont(texts[i].font))
				texts[i].font = font;
		}
	}

	private static Font CreateFont()
	{
		// 1.优先使用工程内字体（需要把字体放到任意Resources目录下并命名为UIFont）
		Font font = null;
		try
		{
			font = Resources.Load<Font>("UIFont");
		}
		catch (Exception)
		{
			font = null;
		}
		if (font != null)
			return font;

		// 2.使用操作系统自带的字体（包含简繁中文字形）
		string[] candidates = new string[]
		{
			"Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC", "Heiti SC",
			"Noto Sans CJK SC", "Noto Sans SC", "SimHei", "SimSun", "Arial Unicode MS"
		};
		try
		{
			font = Font.CreateDynamicFontFromOSFont(candidates, 24);
		}
		catch (Exception)
		{
			font = null;
		}
		if (font != null)
			return font;

		// 3.兜底使用内置字体
		try
		{
			return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static bool IsBuiltinFont(Font font)
	{
		if (font == null)
			return true;

		string fontName = font.name;
		return fontName == "Arial" || fontName == "LegacyRuntime";
	}
}
