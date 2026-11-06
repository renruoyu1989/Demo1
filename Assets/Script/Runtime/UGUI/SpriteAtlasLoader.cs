using System;
using UnityEngine;
using UnityEngine.U2D;
using MotionFramework;
using MotionFramework.Resource;

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

	private static bool _isInitialized = false;

	public static void Initialize()
	{
		if (_isInitialized)
			return;

		_isInitialized = true;
		SpriteAtlasManager.atlasRequested += OnAtlasRequested;
	}

	private static void OnAtlasRequested(string tag, Action<SpriteAtlas> callback)
	{
		string location = $"{AtlasFolder}/{tag}";
		SpriteAtlas atlas = ResourceManager.Instance.SyncLoad<SpriteAtlas>(location);
		if (atlas == null)
		{
			MotionLog.Log(ELogLevel.Warning, $"Failed to load sprite atlas : {location}");
			return;
		}

		callback.Invoke(atlas);
	}
}
