using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MotionFramework;
using YooAsset;

/// <summary>
/// 支持的语言
/// 注意：枚举值同时作为多语言表里的列索引，顺序必须和表头保持一致
/// </summary>
public enum ELanguage
{
	ChineseSimplified = 0,
	ChineseTraditional = 1,
	English = 2,
}

/// <summary>
/// 多语言管理器
/// 数据来源：Localization/Localization.csv（CSV格式，一行一个Key，三列语言）
/// </summary>
public static class LocalizationManager
{
	/// <summary>
	/// 多语言表在资源系统里的定位地址
	/// </summary>
	public const string TableLocation = GameDefine.AssetRootPath + "/Config/Localization";

	private const string LanguagePrefsKey = "GAME_LANGUAGE";
	private const char FieldSeparator = ',';

	private static readonly Dictionary<string, string[]> _tables = new Dictionary<string, string[]>();
	private static readonly int[] _languageColumns = new int[3] { 1, 2, 3 };
	private static bool _isInitialized = false;

	/// <summary>
	/// 当前语言
	/// </summary>
	public static ELanguage Language { get; private set; }

	/// <summary>
	/// 语言切换回调
	/// </summary>
	public static event Action<ELanguage> OnLanguageChanged;

	/// <summary>
	/// 是否已经初始化
	/// </summary>
	public static bool IsInitialized
	{
		get { return _isInitialized; }
	}

	/// <summary>
	/// 初始化
	/// 注意：必须在资源系统创建完毕之后调用
	/// </summary>
	public static bool Initialize()
	{
		if (_isInitialized)
			return true;

		if (LoadTable() == false)
		{
			// 失败时不置位初始化标记：资源系统就绪后可以重试，避免整局界面显示 key
			Debug.LogError($"Failed to load localization table : {TableLocation}");
			return false;
		}

		Language = LoadSavedLanguage();
		_isInitialized = true;
		return true;
	}

	/// <summary>
	/// 切换语言
	/// </summary>
	public static void SetLanguage(ELanguage language)
	{
		if (Language == language)
			return;

		Language = language;
		PlayerPrefs.SetInt(LanguagePrefsKey, (int)language);
		PlayerPrefs.Save();

		if (OnLanguageChanged != null)
			OnLanguageChanged.Invoke(language);
	}

	/// <summary>
	/// 获取本地化文本
	/// </summary>
	public static string Get(string key)
	{
		if (string.IsNullOrEmpty(key))
			return string.Empty;

		string[] values;
		if (_tables.TryGetValue(key, out values))
		{
			int index = (int)Language;
			if (index >= 0 && index < values.Length && string.IsNullOrEmpty(values[index]) == false)
				return values[index];
		}

		// 找不到时回退为Key本身，方便定位缺失文案
		return key;
	}

	/// <summary>
	/// 获取本地化文本并格式化
	/// </summary>
	public static string Get(string key, params object[] args)
	{
		string text = Get(key);
		if (args == null || args.Length == 0)
			return text;

		try
		{
			return string.Format(text, args);
		}
		catch (Exception)
		{
			return text;
		}
	}

	/// <summary>
	/// 是否包含该Key
	/// </summary>
	public static bool Contains(string key)
	{
		return string.IsNullOrEmpty(key) == false && _tables.ContainsKey(key);
	}

	/// <summary>
	/// 获取当前语言在表格里的列索引
	/// </summary>
	public static int GetLanguageColumn(ELanguage language)
	{
		return _languageColumns[(int)language];
	}

	#region 内部实现
	private static bool LoadTable()
	{
		AssetHandle handle = YooAssets.LoadAssetSync<TextAsset>(TableLocation);
		TextAsset asset = handle.IsValid ? handle.AssetObject as TextAsset : null;
		if (asset == null)
		{
			if (handle.IsValid)
				handle.Release();
			return false;
		}

		ParseCsv(asset.text);

		// 文本已经解析到内存里，这里释放资源引用
		handle.Release();
		return _tables.Count > 0;
	}

	private static void ParseCsv(string content)
	{
		_tables.Clear();
		if (string.IsNullOrEmpty(content))
			return;

		List<string[]> rows = ParseCsvRows(content);
		if (rows.Count <= 1)
			return;

		// 通过表头确定每种语言所在的列
		string[] header = rows[0];
		for (int i = 0; i < header.Length; i++)
		{
			string name = header[i].Trim();
			if (string.Equals(name, "ChineseSimplified", StringComparison.OrdinalIgnoreCase))
				_languageColumns[0] = i;
			else if (string.Equals(name, "ChineseTraditional", StringComparison.OrdinalIgnoreCase))
				_languageColumns[1] = i;
			else if (string.Equals(name, "English", StringComparison.OrdinalIgnoreCase))
				_languageColumns[2] = i;
		}

		int maxColumn = 0;
		for (int i = 0; i < _languageColumns.Length; i++)
			maxColumn = Mathf.Max(maxColumn, _languageColumns[i]);

		for (int i = 1; i < rows.Count; i++)
		{
			string[] fields = rows[i];
			if (fields.Length == 0)
				continue;

			string key = fields[0].Trim();
			if (string.IsNullOrEmpty(key))
				continue;

			// 以 # 开头的行是注释行
			if (key.StartsWith("#"))
				continue;

			string[] values = new string[3];
			for (int j = 0; j < 3; j++)
			{
				int column = _languageColumns[j];
				values[j] = (column < fields.Length) ? fields[column].Trim() : string.Empty;
			}

			if (maxColumn >= fields.Length)
				Debug.LogWarning($"Localization row is incomplete : {key}");

			_tables[key] = values;
		}
	}

	/// <summary>
	/// CSV解析（支持双引号包裹字段、字段内逗号、""转义）
	/// </summary>
	private static List<string[]> ParseCsvRows(string content)
	{
		List<string[]> rows = new List<string[]>();
		List<string> fields = new List<string>();
		StringBuilder buffer = new StringBuilder();
		bool isQuoted = false;

		// 去掉UTF-8 BOM
		if (content.Length > 0 && content[0] == '\uFEFF')
			content = content.Substring(1);

		for (int i = 0; i < content.Length; i++)
		{
			char c = content[i];

			if (isQuoted)
			{
				if (c == '"')
				{
					// 连续两个双引号代表一个双引号字符
					if (i + 1 < content.Length && content[i + 1] == '"')
					{
						buffer.Append('"');
						i++;
					}
					else
					{
						isQuoted = false;
					}
				}
				else
				{
					buffer.Append(c);
				}
				continue;
			}

			if (c == '"')
			{
				isQuoted = true;
			}
			else if (c == FieldSeparator)
			{
				fields.Add(buffer.ToString());
				buffer.Length = 0;
			}
			else if (c == '\r')
			{
				// 忽略，等待 \n
			}
			else if (c == '\n')
			{
				fields.Add(buffer.ToString());
				buffer.Length = 0;
				rows.Add(fields.ToArray());
				fields.Clear();
			}
			else
			{
				buffer.Append(c);
			}
		}

		if (buffer.Length > 0 || fields.Count > 0)
		{
			fields.Add(buffer.ToString());
			rows.Add(fields.ToArray());
		}

		return rows;
	}

	private static ELanguage LoadSavedLanguage()
	{
		if (PlayerPrefs.HasKey(LanguagePrefsKey))
		{
			int value = PlayerPrefs.GetInt(LanguagePrefsKey, (int)ELanguage.ChineseSimplified);
			if (value >= (int)ELanguage.ChineseSimplified && value <= (int)ELanguage.English)
				return (ELanguage)value;
		}

		return GetSystemLanguage();
	}

	private static void SaveLanguage(ELanguage language)
	{
		PlayerPrefs.SetInt(LanguagePrefsKey, (int)language);
		PlayerPrefs.Save();
	}

	/// <summary>
	/// 根据系统语言推断默认语言
	/// </summary>
	private static ELanguage GetSystemLanguage()
	{
		switch (Application.systemLanguage)
		{
			case SystemLanguage.ChineseTraditional:
				return ELanguage.ChineseTraditional;

			case SystemLanguage.Chinese:
			case SystemLanguage.ChineseSimplified:
				return ELanguage.ChineseSimplified;

			default:
				return ELanguage.English;
		}
	}
	#endregion
}
