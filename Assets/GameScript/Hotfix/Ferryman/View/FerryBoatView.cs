using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 渡船表现：船体贴图 + 船夫（hero.man 划桨）+ 挟船行走的船夫（hero.walk，帧里自带船）。
	/// 原版把「渡船 + 主角」当成一组动画：水里一律划桨，只有岸上搬船才换成挟船行走。
	/// </summary>
	public class FerryBoatView
	{
		public GameObject Go;
		public SpriteRenderer Renderer;
		public readonly FerrySpriteAnimator Animator = new FerrySpriteAnimator();

		public GameObject HeroGo;
		public SpriteRenderer HeroRenderer;
		public readonly FerrySpriteAnimator HeroAnimator = new FerrySpriteAnimator();

		/// <summary>岸上挟船行走：整个 GameObject 默认隐藏，只有搬船那一段显示</summary>
		public GameObject CarrierGo;
		public SpriteRenderer CarrierRenderer;
		public readonly FerrySpriteAnimator CarrierAnimator = new FerrySpriteAnimator();

		private Vector3 _target;
		private Vector3 _from;
		private float _duration;
		private float _elapsed;
		private float _heroOffsetX;
		private Vector3 _carrierFrom;
		private Vector3 _carrierTo;
		private bool _carrying;

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

			// 原版 Carrier 与船同层：搬船时普通船体/船夫隐藏，只留这一组自带船的行走帧
			CarrierGo = new GameObject("Carrier");
			CarrierGo.transform.SetParent(parent, false);
			CarrierRenderer = CarrierGo.AddComponent<SpriteRenderer>();
			CarrierRenderer.sortingOrder = boatSortingOrder;
			CarrierRenderer.enabled = false;
			CarrierAnimator.Renderer = CarrierRenderer;
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

		/// <summary>搬船时只显示 Carrier（帧里自带船），普通船体与船上的船夫都隐藏</summary>
		public void SetCarrying(bool carrying)
		{
			if (_carrying == carrying)
				return;

			_carrying = carrying;
			if (Renderer != null)
				Renderer.enabled = !carrying;
			if (HeroRenderer != null)
				HeroRenderer.enabled = !carrying;
			if (CarrierRenderer != null)
				CarrierRenderer.enabled = carrying;
			// 原版搬船结束会把 localScale 复位（_carrier.transform.localScale = Vector3.one）
			if (carrying == false && CarrierGo != null)
				CarrierGo.transform.localScale = Vector3.one;
		}

		public bool IsCarrying { get { return _carrying; } }

		public void PlayCarrier(string animName, Sprite[] frames, float fps)
		{
			if (frames == null || frames.Length == 0)
				return;
			CarrierAnimator.Play(animName, frames, fps, true);
		}

		/// <summary>
		/// 岸上搬船的轨迹：船夫沿码头甲板从旧侧岸沿走到新侧岸沿，与船体用同一段插值进度。
		/// 方向严格照原版 BoardView.MoveBoat：walk 帧默认朝左，向右走把整组镜像
		/// （transform.localScale.x = -1），向左走保持原样；结束时复位为 Vector3.one。
		/// 用 localScale 而不是 SpriteRenderer.flipX，避免与原版语义出现偏差。
		/// </summary>
		public void BeginCarry(Vector3 from, Vector3 to)
		{
			_carrierFrom = from;
			_carrierTo = to;
			if (CarrierGo != null)
			{
				CarrierGo.transform.position = from;
				CarrierGo.transform.localScale = new Vector3(to.x >= from.x ? -1f : 1f, 1f, 1f);
			}
			if (CarrierRenderer != null)
				CarrierRenderer.flipX = false;
		}

		public void MoveTo(Vector3 position, bool instant = false)
		{
			// 同一次 Row 会连着 Notify 两次（改状态 / 结算吃子），后续 Board、Unload 也会重排布局。
			// 目标没变时不能重启插值，否则渡河/搬船动作会被反复拉长甚至一直停不下来。
			if (instant == false && _duration > 0f && (_target - position).sqrMagnitude < 0.25f)
				return;

			_target = position;
			if (instant)
			{
				Go.transform.position = position;
				_duration = 0f;
				_elapsed = 0f;
				SetCarrying(false);
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

		/// <summary>
		/// 船夫相对船身的固定站位。传入的是「船停稳后的世界位置」，
		/// 但船可能还在插值，所以用船的当前 X 定位，之后由 ShiftHero 跟着船走。
		/// </summary>
		public void MoveHeroTo(Vector3 position)
		{
			_heroOffsetX = position.x - _target.x;
			HeroGo.transform.position = new Vector3(Go.transform.position.x + _heroOffsetX, position.y, position.z);
		}

		/// <summary>渡河途中把船夫按船的当前 X 摆回船尾固定偏移处</summary>
		public void SetHeroX(float x)
		{
			Vector3 p = HeroGo.transform.position;
			p.x = x;
			HeroGo.transform.position = p;
		}

		public void Tick(float deltaTime)
		{
			if (_duration > 0f)
			{
				_elapsed += deltaTime;
				float t = Mathf.Clamp01(_elapsed / _duration);
				Go.transform.position = Vector3.Lerp(_from, _target, t);

				if (_carrying && CarrierGo != null)
					CarrierGo.transform.position = Vector3.Lerp(_carrierFrom, _carrierTo, t);

				if (t >= 1f)
				{
					Go.transform.position = _target;
					_duration = 0f;
				}
			}

			Animator.Tick(deltaTime);
			HeroAnimator.Tick(deltaTime);
			CarrierAnimator.Tick(deltaTime);
		}

		public void Destroy()
		{
			if (Go != null)
				Object.Destroy(Go);
			if (HeroGo != null)
				Object.Destroy(HeroGo);
			if (CarrierGo != null)
				Object.Destroy(CarrierGo);
			Go = null;
			HeroGo = null;
			CarrierGo = null;
		}
	}
}
