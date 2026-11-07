using System;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// YooAsset 资源包构建入口
/// </summary>
public static class BuildPipelineEditor
{
	/// <summary>
	/// 构建资源包并部署到 StreamingAssets（单机离线模式使用）
	/// </summary>
	[MenuItem("Tools/YooAsset/构建 AssetBundle（当前平台 → StreamingAssets）", false, 100)]
	public static bool BuildAssetBundles()
	{
		BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;

		// 构建会清空并覆盖 StreamingAssets 内的资源包，必须二次确认
		if (EditorUtility.DisplayDialog("构建资源包",
			$"将构建 {buildTarget} 平台的资源包，并清空覆盖 StreamingAssets 内已有内容。\n\n是否继续？",
			"开始构建", "取消") == false)
		{
			return false;
		}

		Debug.Log($"[BuildPipelineEditor] 开始构建资源包，平台：{buildTarget}");

		BuiltinBuildParameters buildParameters = new BuiltinBuildParameters();
		buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
		buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
		buildParameters.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString();
		buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
		buildParameters.BuildTarget = buildTarget;
		buildParameters.PackageName = GameLauncher.PackageName;
		// 版本号取 PlayerSettings 的版本（离线模式每次覆盖同一版本即可），
		// 时间戳会让每次构建都变成全新版本，导致版本目录无限增长
		buildParameters.PackageVersion = PlayerSettings.bundleVersion;
		buildParameters.EnableSharePackRule = true;
		buildParameters.VerifyBuildingResult = true;
		buildParameters.FileNameStyle = EFileNameStyle.BundleName_HashName;
		buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
		buildParameters.CompressOption = ECompressOption.LZ4;
		buildParameters.ClearBuildCacheFiles = false;
		buildParameters.UseAssetDependencyDB = true;

		BuiltinBuildPipeline pipeline = new BuiltinBuildPipeline();
		BuildResult buildResult = pipeline.Run(buildParameters, true);
		if (buildResult.Success)
		{
			Debug.Log($"[BuildPipelineEditor] 资源包构建成功，版本：{buildParameters.PackageVersion}，已部署到 StreamingAssets");
			AssetDatabase.Refresh();
			return true;
		}

		Debug.LogError($"[BuildPipelineEditor] 资源包构建失败：{buildResult.ErrorInfo}");
		return false;
	}

	/// <summary>
	/// 打开 YooAsset 的资源收集器窗口
	/// </summary>
	[MenuItem("Tools/YooAsset/打开资源收集器", false, 0)]
	public static void OpenCollectorWindow()
	{
		AssetBundleCollectorWindow.OpenWindow();
	}

	/// <summary>
	/// 打开 YooAsset 的资源构建器窗口
	/// </summary>
	[MenuItem("Tools/YooAsset/打开资源构建器", false, 1)]
	public static void OpenBuilderWindow()
	{
		AssetBundleBuilderWindow.OpenWindow();
	}
}
