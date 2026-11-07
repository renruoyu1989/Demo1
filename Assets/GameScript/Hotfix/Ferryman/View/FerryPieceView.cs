using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 单个棋子的表现：SpriteRenderer + 切帧动画 + 位置插值
	/// </summary>
	public class FerryPieceView
	{
		public FerryPiece Piece;
		public GameObject Go;
		public SpriteRenderer Renderer;
		public BoxCollider2D Collider;
		public readonly FerrySpriteAnimator Animator = new FerrySpriteAnimator();

		private Vector3 _target;
		private Vector3 _from;
		private float _speed = 400f;
		private float _duration;
		private float _elapsed;
		private Sprite _colliderSprite;

		public bool IsMoving
		{
			get { return _duration > 0f || (Go.transform.position - _target).sqrMagnitude > 0.25f; }
		}

		public FerryPieceView(FerryPiece piece, GameObject go, Transform parent, int sortingOrder)
		{
			Piece = piece;
			Go = go;
			Go.transform.SetParent(parent, false);

			Renderer = Go.AddComponent<SpriteRenderer>();
			Renderer.sortingOrder = sortingOrder;

			Collider = Go.AddComponent<BoxCollider2D>();
			Collider.size = new Vector2(60f, 70f);
			Collider.offset = new Vector2(0f, 35f);

			Animator.Renderer = Renderer;

			float r = piece.Dto.ColorR / 255f;
			float g = piece.Dto.ColorG / 255f;
			float b = piece.Dto.ColorB / 255f;
			Renderer.color = new Color(r, g, b, 1f);

			// 表里的 speed 是「格/秒」，乘上每格像素数才是走路速度
			float grids = piece.Dto.Speed > 0.1f ? piece.Dto.Speed : 2.5f;
			_speed = grids * FerryDefine.WalkPixelsPerGrid;
		}

		public void Play(string animName, Sprite[] frames, float fps)
		{
			Animator.Play(animName, frames, fps, true);
			ApplyCollider(frames);
		}

		/// <summary>点击区域跟随当前帧的贴图尺寸（不同动物体型差很多，固定尺寸会点不到/误点）</summary>
		private void ApplyCollider(Sprite[] frames)
		{
			if (Collider == null || frames == null || frames.Length == 0)
				return;

			Sprite sprite = frames[0];
			if (sprite == null || sprite == _colliderSprite)
				return;

			Vector2 size = sprite.bounds.size;
			if (size.x < 1f || size.y < 1f)
				return;

			_colliderSprite = sprite;
			Collider.size = size;
			Collider.offset = new Vector2(0f, size.y * 0.5f);
		}

		public void SetPosition(Vector3 position)
		{
			_target = position;
			Go.transform.position = position;
		}

		/// <summary>
		/// 渡河途中按船的当前 X 把乘客钉在船上的槽位（原版每帧 LayoutBoatRiders）。
		/// 同时取消尚未走完的上船插值，避免「自身走路」和「船横移」叠加成双倍位移。
		/// </summary>
		public void SetBoatRiderX(float x)
		{
			Vector3 p = Go.transform.position;
			p.x = x;
			Go.transform.position = p;
			_from.x = x;
			_target.x = x;
			_duration = 0f;
			_elapsed = 0f;
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

			float distance = Vector3.Distance(Go.transform.position, position);
			if (distance < 0.5f)
			{
				_duration = 0f;
				return;
			}

			// 原版：速度按「格/秒」换算，时长再夹在 0.3~2 秒之间，不然近距离会瞬移、远距离会走太久
			_from = Go.transform.position;
			_duration = Mathf.Clamp(distance / _speed, FerryDefine.WalkMinDuration, FerryDefine.WalkMaxDuration);
			_elapsed = 0f;
		}

		public void Tick(float deltaTime)
		{
			Animator.Tick(deltaTime);

			Vector3 current = Go.transform.position;
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
			else if ((current - _target).sqrMagnitude > 0.25f)
			{
				Go.transform.position = Vector3.MoveTowards(current, _target, _speed * deltaTime);
			}

			// 朝向：向左移动时翻转（原版每段行走只在起步时定朝向）
			float dx = _target.x - current.x;
			if (Mathf.Abs(dx) > 0.5f)
				Renderer.flipX = dx < 0f;
		}

		public void Destroy()
		{
			if (Go != null)
				Object.Destroy(Go);
			Go = null;
		}
	}
}
