using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace Hotfix
{
	/// <summary>
	/// 棋盘用到的贴图缓存：按需加载 png，按帧矩形切成 Sprite 并缓存
	/// 注意：只保留当前关卡用到的图，切关时必须 ReleaseAll
	/// </summary>
	public class FerryAssetCache
	{
		private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
		private readonly Dictionary<string, AssetHandle> _handles = new Dictionary<string, AssetHandle>();
		private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

		private int _pending;
		private Action _onAllDone;

		/// <summary>批量加载贴图（name 为不带扩展名的文件名），全部完成后回调</summary>
		public void LoadTextures(List<string> names, Action onAllDone)
		{
			_onAllDone = onAllDone;

			List<string> todo = new List<string>();
			for (int i = 0; i < names.Count; i++)
			{
				string name = names[i];
				if (string.IsNullOrEmpty(name)) continue;
				if (_textures.ContainsKey(name)) continue;
				if (todo.Contains(name)) continue;
				todo.Add(name);
			}

			if (todo.Count == 0)
			{
				if (_onAllDone != null) _onAllDone();
				return;
			}

			_pending = todo.Count;
			for (int i = 0; i < todo.Count; i++)
			{
				string name = todo[i];
				AssetHandle handle = YooAssets.LoadAssetAsync<Texture2D>(FerryDefine.EntityLocationRoot + name);
				_handles[name] = handle;
				handle.Completed += h => Handle_Completed(name, h);
			}
		}

		private void Handle_Completed(string name, AssetHandle handle)
		{
			Texture2D texture = handle.AssetObject as Texture2D;
			if (texture == null)
			{
				// 少量原版占位图（例如 Water）没有对应贴图，跳过即可
				HotfixLogger.Warning($"FerryAssetCache skip missing texture : {name}");
			}
			else
			{
				_textures[name] = texture;
			}

			_pending--;
			if (_pending <= 0 && _onAllDone != null)
			{
				Action callback = _onAllDone;
				_onAllDone = null;
				callback();
			}
		}

		public Texture2D GetTexture(string name)
		{
			Texture2D texture;
			_textures.TryGetValue(name, out texture);
			return texture;
		}

		/// <summary>按帧矩形取 Sprite（pivot 在底边中点，便于站在甲板线上）</summary>
		public Sprite GetSprite(string name, FerryFrameData frame)
		{
			if (frame == null)
				return null;

			string key = string.Format("{0}|{1}|{2}|{3}|{4}", name, frame.x, frame.y, frame.w, frame.h);
			Sprite sprite;
			if (_sprites.TryGetValue(key, out sprite))
				return sprite;

			Texture2D texture = GetTexture(name);
			if (texture == null)
				return null;

			Rect rect = new Rect(frame.x, frame.y, frame.w, frame.h);
			sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 1f);
			sprite.name = key;
			_sprites.Add(key, sprite);
			return sprite;
		}

		/// <summary>按整张贴图取 Sprite（用于船这类单图资源）</summary>
		public Sprite GetFullSprite(string name)
		{
			Texture2D texture = GetTexture(name);
			if (texture == null)
				return null;

			string key = name + "|full";
			Sprite sprite;
			if (_sprites.TryGetValue(key, out sprite))
				return sprite;

			sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 1f);
			sprite.name = key;
			_sprites.Add(key, sprite);
			return sprite;
		}

		public void ReleaseAll()
		{
			foreach (KeyValuePair<string, Sprite> pair in _sprites)
			{
				if (pair.Value != null)
					UnityEngine.Object.Destroy(pair.Value);
			}
			_sprites.Clear();

			foreach (KeyValuePair<string, AssetHandle> pair in _handles)
			{
				if (pair.Value != null)
					pair.Value.Release();
			}
			_handles.Clear();
			_textures.Clear();
			_pending = 0;
			_onAllDone = null;
		}
	}
}
