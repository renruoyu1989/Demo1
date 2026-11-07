using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 渡船表现：船体贴图 + 船夫（站立/行走两套帧动画）
	/// </summary>
	public class FerryBoatView
	{
		public GameObject Go;
		public SpriteRenderer Renderer;
		public readonly FerrySpriteAnimator Animator = new FerrySpriteAnimator();

		public GameObject HeroGo;
		public SpriteRenderer HeroRenderer;
		public readonly FerrySpriteAnimator HeroAnimator = new FerrySpriteAnimator();

		private Vector3 _target;
		private Vector3 _from;
		private float _duration;
		private float _elapsed;

		/// <summary>船是否还在向目标位置插值（用于切换船夫动作）</summary>
		public bool IsMoving
		{
			get { return _duration > 0f || (Go.transform.position - _target).sqrMagnitude > 0.25f; }
		}

		public FerryBoatView(Transform parent, int boatSortingOrder, int heroSortingOrder)
		{
			Go = new GameObject("Boat");
			Go.transform.SetParent(parent, false);
			Renderer = Go.AddComponent<SpriteRenderer>();
			Renderer.sortingOrder = boatSortingOrder;
			Animator.Renderer = Renderer;

			HeroGo = new GameObject("Ferryman");
			HeroGo.transform.SetParent(parent, false);
			HeroRenderer = HeroGo.AddComponent<SpriteRenderer>();
			HeroRenderer.sortingOrder = heroSortingOrder;
			HeroAnimator.Renderer = HeroRenderer;
		}

		public void SetBoatSprite(Sprite sprite)
		{
			Animator.SetStatic(sprite);
		}

		public void PlayHero(string animName, Sprite[] frames, float fps)
		{
			if (frames == null || frames.Length == 0)
				return;
			HeroAnimator.Play(animName, frames, fps, true);
		}

		public void MoveTo(Vector3 position, bool instant = false)
		{
			_target = position;
			if (instant)
			{
				Go.transform.position = position;
				_duration = 0f;
				_elapsed = 0f;
				return;
			}

			if ((Go.transform.position - position).sqrMagnitude < 0.25f)
			{
				_duration = 0f;
				return;
			}

			// 原版划一次船固定 0.75 秒，慢一点才有「摆渡」的感觉
			_from = Go.transform.position;
			_duration = FerryDefine.BoatMoveDuration;
			_elapsed = 0f;
		}

		public void MoveHeroTo(Vector3 position)
		{
			HeroGo.transform.position = position;
		}

		public void Tick(float deltaTime)
		{
			if (_duration > 0f)
			{
				_elapsed += deltaTime;
				float t = Mathf.Clamp01(_elapsed / _duration);
				if (t >= 1f)
				{
					Go.transform.position = _target;
					_duration = 0f;
				}
				else
				{
					Go.transform.position = Vector3.Lerp(_from, _target, t);
				}
			}

			Animator.Tick(deltaTime);
			HeroAnimator.Tick(deltaTime);
		}

		public void Destroy()
		{
			if (Go != null)
				Object.Destroy(Go);
			if (HeroGo != null)
				Object.Destroy(HeroGo);
			Go = null;
			HeroGo = null;
		}
	}
}
