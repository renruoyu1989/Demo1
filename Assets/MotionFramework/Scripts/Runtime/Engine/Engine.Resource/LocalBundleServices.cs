//--------------------------------------------------
// Motion Framework
// Copyright©2018-2020 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using System.IO;
using UnityEngine;
using MotionFramework.Patch;

namespace MotionFramework.Resource
{
	/// <summary>
	/// 单机版AssetBundle服务
	/// 注意：只从APP内的StreamingAssets加载资源，没有CDN服务器、没有沙盒更新流程
	/// </summary>
	public sealed class LocalBundleServices : IBundleServices
	{
		public static readonly LocalBundleServices Instance = new LocalBundleServices();

		private string _cachedLocationRoot;
		private AssetBundleManifest _unityManifest;

		private LocalBundleServices()
		{
		}

		string IBundleServices.ConvertLocationToManifestPath(string location)
		{
			if (_cachedLocationRoot == null)
			{
				if (string.IsNullOrEmpty(AssetSystem.LocationRoot))
					throw new System.Exception($"{nameof(AssetSystem.LocationRoot)} is null or empty.");
				_cachedLocationRoot = AssetSystem.LocationRoot.ToLower();
			}

			// 转换为小写形式
			location = location.ToLower();

			// 如果是变体资源
			if (Path.HasExtension(location))
				return $"{_cachedLocationRoot}/{location}";
			else
				return $"{_cachedLocationRoot}/{location}.{PatchDefine.AssetBundleDefaultVariant}";
		}
		string IBundleServices.GetAssetBundleLoadPath(string manifestPath)
		{
			return AssetPathHelper.MakeStreamingLoadPath(manifestPath);
		}
		string[] IBundleServices.GetDirectDependencies(string assetBundleName)
		{
			AssetBundleManifest manifest = LoadUnityManifest();
			if (manifest == null)
				return new string[0];
			return manifest.GetDirectDependencies(assetBundleName);
		}
		string[] IBundleServices.GetAllDependencies(string assetBundleName)
		{
			AssetBundleManifest manifest = LoadUnityManifest();
			if (manifest == null)
				return new string[0];
			return manifest.GetAllDependencies(assetBundleName);
		}

		private AssetBundleManifest LoadUnityManifest()
		{
			if (_unityManifest != null)
				return _unityManifest;

			string loadPath = AssetPathHelper.MakeStreamingLoadPath(PatchDefine.UnityManifestFileName);
			AssetBundle bundle = AssetBundle.LoadFromFile(loadPath);
			if (bundle == null)
			{
				MotionLog.Log(ELogLevel.Error, $"Failed to load unity manifest : {loadPath}");
				return null;
			}

			_unityManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
			bundle.Unload(false);
			return _unityManifest;
		}
	}
}
