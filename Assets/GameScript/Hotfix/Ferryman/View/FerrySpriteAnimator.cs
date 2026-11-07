using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 大图切帧动画播放器（纯 C# 类，由 FerryBoardView 统一 Tick 驱动，不自带 Update）
	/// </summary>
	public class FerrySpriteAnimator
	{
		public SpriteRenderer Renderer;

		private Sprite[] _frames;
		private float _fps = 20f;
		private float _time;
		private int _index;
		private bool _loop = true;

		/// <summary>当前动画名</summary>
		public string AnimName { get; private set; }

		public bool IsPlaying { get { return _frames != null && _frames.Length > 1; } }

		/// <summary>播放动画（frames 为按帧顺序排好的 Sprite）</summary>
		public void Play(string animName, Sprite[] frames, float fps, bool loop)
		{
			if (frames == null || frames.Length == 0)
				return;
			if (AnimName == animName && _frames == frames && loop)
				return;

			AnimName = animName;
			_frames = frames;
			_fps = fps > 0f ? fps : 20f;
			_loop = loop;
			_time = 0f;
			_index = 0;

			if (Renderer != null)
				Renderer.sprite = _frames[0];
		}

		public void Tick(float deltaTime)
		{
			if (_frames == null || _frames.Length <= 1)
				return;

			_time += deltaTime;
			float frameTime = 1f / _fps;
			while (_time >= frameTime)
			{
				_time -= frameTime;
				_index++;

				if (_index >= _frames.Length)
				{
					if (_loop)
					{
						_index = 0;
					}
					else
					{
						_index = _frames.Length - 1;
						break;
					}
				}
			}

			if (Renderer != null && _frames[_index] != null)
				Renderer.sprite = _frames[_index];
		}

		/// <summary>改用静态图</summary>
		public void SetStatic(Sprite sprite)
		{
			_frames = null;
			AnimName = null;
			if (Renderer != null)
				Renderer.sprite = sprite;
		}
	}
}
