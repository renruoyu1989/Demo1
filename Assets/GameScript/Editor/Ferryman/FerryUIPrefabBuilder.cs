using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ferryman.EditorTools
{
	/// <summary>
	/// 批量生成过河游戏的窗口 prefab
	/// 说明：窗口内容由 FerryUIBuilder 在运行时用代码构建，
	/// prefab 只保留「Canvas + CanvasScaler + GraphicRaycaster + UIManifest」这套框架必需结构
	/// </summary>
	public static class FerryUIPrefabBuilder
	{
		private const string PanelDir = "Assets/GameRes/UIPanel";

		private static readonly string[] Windows =
		{
			"UIMenu",
			"UISelect",
			"UIHud",
			"UIResult",
			"UIRules",
			"UIHelp",
			"UIOptions",
			"UITutorial",
		};

		[MenuItem("Tools/Ferryman/1. 生成 UI Prefab", false, 1)]
		public static void BuildAll()
		{
			if (Directory.Exists(PanelDir) == false)
				Directory.CreateDirectory(PanelDir);

			for (int i = 0; i < Windows.Length; i++)
			{
				BuildOne(Windows[i]);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log($"Ferry UI prefab 生成完毕，共 {Windows.Length} 个，目录 {PanelDir}");
		}

		private static void BuildOne(string name)
		{
			GameObject root = new GameObject(name);
			RectTransform rect = root.AddComponent<RectTransform>();
			rect.sizeDelta = new Vector2(1920f, 1080f);

			Canvas canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 0;

			CanvasScaler scaler = root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0.5f;

			root.AddComponent<GraphicRaycaster>();
			root.AddComponent<UIManifest>();

			string path = $"{PanelDir}/{name}.prefab";
			PrefabUtility.SaveAsPrefabAsset(root, path);
			Object.DestroyImmediate(root);
			Debug.Log($"生成 prefab : {path}");
		}
	}
}
