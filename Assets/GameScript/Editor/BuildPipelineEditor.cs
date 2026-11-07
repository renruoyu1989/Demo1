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
		Debug.Log($"[BuildPipelineEditor] 开始构建资源包，平台：{buildTarget}");

		BuiltinBuildParameters buildParameters = new BuiltinBuildParameters();
		buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
		buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
		buildParameters.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString();
		buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
		buildParameters.BuildTarget = buildTarget;
		buildParameters.PackageName = GameLauncher.PackageName;
		buildParameters.PackageVersion = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
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
