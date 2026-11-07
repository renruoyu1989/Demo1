using System.IO;
using UnityEditor;
using UnityEngine;

class EditorInitialize
{
	/// <summary>
	/// 检测Unity编辑器版本
	/// </summary>
	[InitializeOnLoadMethod]
	static void CheckEditorVersion()
	{
		/*
		// TODO：这里设置你团队当前使用的Unity版本
		string version = "2018.4.6f1";
		Debug.Log($"当前使用Unity版本：{Application.unityVersion}");
		if (Application.unityVersion != version)
		{
			if(EditorUtility.DisplayDialog("错误", $"请安装Unity{version}，详情咨询客户端！", "OK"))
			{
				EditorApplication.Exit(0);
			}
		}
		*/
	}

	/// <summary>
	/// 创建MotionEngien依赖文件夹
	/// </summary>
	[InitializeOnLoadMethod]
	static void CreateMotionEngineDependFolder()
	{
		// 注意：不要在这里创建 UIDefine.StrMyUIAtlasFolderPath（Assets/GameRes/UIAtlas）。
		// Directory.CreateDirectory 会连带创建所有不存在的父级目录，导致编辑器每次加载/重编译都生成空目录；
		// 空目录无法纳入版本管理，会反复出现。该目录目前没有代码使用，需要时按需创建即可。

		// 精灵目录 Assets/GameArt/UISprite 已纳入版本管理，此判断仅用于防止被误删后图集工具找不到路径
		if (Directory.Exists(UIDefine.StrMyUISpriteFolderPath) == false)
			Directory.CreateDirectory(UIDefine.StrMyUISpriteFolderPath);
	}
}