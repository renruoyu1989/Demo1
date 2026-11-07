using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using MotionFramework;
using YooAsset;

/// <summary>
/// SpriteAtlas延迟绑定加载器
/// 注意：图集是Late Binding模式时，Unity会通过atlasRequested回调索要图集，
/// 如果没有监听者就会报"atlasRequested wasn't listened to"警告且sprite显示为空
/// </summary>
public static class SpriteAtlasLoader
{
	/// <summary>
	/// 图集所在目录（相对资源根路径）
	/// </summary>
	public const string AtlasFolder = "UIAtlas";

	/// <summary>
	/// 图集句柄缓存
	/// 图集需要常驻内存，句柄必须持有，否则被GC后引用计数只增不减
	/// </summary>
	private static readonly Dictionary<string, AssetHandle> _handles = new Dictionary<string, AssetHandle>();

	private static bool _isInitialized = false;

	public static void Initialize()
	{
		if (_isInitialized)
			return;

		_isInitialized = true;
		SpriteAtlasManager.atlasRequested += OnAtlasRequested;
	}

	/// <summary>
	/// 释放所有缓存的图集句柄
	/// </summary>
	public static void ReleaseAll()
	{
		foreach (var pair in _handles)
		{
			if (pair.Value != null && pair.Value.IsValid)
				pair.Value.Release();
		}
		_handles.Clear();
	}

	private static void OnAtlasRequested(string tag, Action<SpriteAtlas> callback)
	{
		string location = $"{GameDefine.AssetRootPath}/{AtlasFolder}/{tag}";

		// 命中缓存
		if (_handles.TryGetValue(location, out AssetHandle cached))
		{
			SpriteAtlas cachedAtlas = cached.IsValid ? cached.AssetObject as SpriteAtlas : null;
			if (cachedAtlas != null)
			{
				callback.Invoke(cachedAtlas);
				return;
			}

			// 缓存失效（资源被卸载），释放后重新加载
			if (cached.IsValid)
				cached.Release();
			_handles.Remove(location);
		}

		AssetHandle handle = YooAssets.LoadAssetSync<SpriteAtlas>(location);
		SpriteAtlas atlas = handle.IsValid ? handle.AssetObject as SpriteAtlas : null;
		if (atlas == null)
		{
			Debug.LogError($"Failed to load sprite atlas : {location}");
			if (handle.IsValid)
				handle.Release();
			return;
		}

		// 注意：图集需要一直存活，这里保存句柄且不释放
		_handles[location] = handle;
		callback.Invoke(atlas);
	}
}
