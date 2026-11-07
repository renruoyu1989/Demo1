using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 棋盘坐标换算与相机适配
	/// 设计稿坐标：原点在左下角，1 设计像素 = 1 世界单位，Y 轴向上
	/// </summary>
	public class FerryBoardAnchor
	{
		public Camera Camera { get; private set; }

		/// <summary>本关设计稿宽度</summary>
		public float CanvasWidth { get; private set; }

		/// <summary>设计稿高度</summary>
		public float CanvasHeight { get; private set; }

		/// <summary>
		/// 棋盘高度占屏幕高度的比例。原版相机正交尺寸就是设计稿高度的一半，
		/// 也就是 562 高的舞台正好铺满屏幕竖向，这里保持 1 以对齐原版取景。
		/// </summary>
		private const float BoardFillRatio = 1f;

		private float _cameraX;
		private float _targetX;

		/// <summary>关卡真实内容的 X 范围（贴图包围盒），相机横移在这里面夹</summary>
		private float _contentMinX;
		private float _contentMaxX;

		public FerryBoardAnchor(Camera camera, float canvasWidth)
		{
			Camera = camera;
			CanvasWidth = canvasWidth > 0f ? canvasWidth : FerryDefine.DesignWidth;
			CanvasHeight = FerryDefine.DesignHeight;

			camera.orthographic = true;
			camera.orthographicSize = CanvasHeight * 0.5f / BoardFillRatio;
			camera.nearClipPlane = -50f;
			camera.farClipPlane = 50f;
			camera.transform.rotation = Quaternion.identity;

			_cameraX = CanvasWidth <= ScreenVisibleWidth ? CanvasWidth * 0.5f : ScreenVisibleWidth * 0.5f;
			_targetX = _cameraX;
			Apply();
		}

		/// <summary>相机可见宽度（世界单位）</summary>
		public float ScreenVisibleWidth
		{
			get { return Camera.orthographicSize * 2f * Camera.aspect; }
		}

		/// <summary>
		/// 设计稿坐标 → 世界坐标
		/// 注意：舞台数据的 Y 是「屏幕向下」的原点在上坐标系，这里翻转成 Unity 的 Y 向上
		/// </summary>
		public Vector3 ToWorld(float x, float y, float z = 0f)
		{
			return new Vector3(x, CanvasHeight - y, z);
		}

		/// <summary>把相机横向对准某个设计稿 X</summary>
		public void FollowX(float x)
		{
			_targetX = x;
		}

		/// <summary>鼠标位于屏幕边缘时自动左右滚动</summary>
		public void EdgeScroll(float deltaTime, float edgePixels = 8f, float speed = 900f)
		{
			Vector3 mouse = Input.mousePosition;
			if (mouse.x < 0f || mouse.x > Screen.width || mouse.y < 0f || mouse.y > Screen.height)
				return;

			if (mouse.x <= edgePixels)
				_targetX -= speed * deltaTime;
			else if (mouse.x >= Screen.width - edgePixels)
				_targetX += speed * deltaTime;
		}

		/// <summary>设置关卡真实内容范围（世界 X），用于夹住相机，避免横移时露出空背景</summary>
		public void SetContentBounds(float minX, float maxX)
		{
			if (maxX <= minX)
				return;

			_contentMinX = minX;
			_contentMaxX = maxX;
		}

		public void Tick(float deltaTime)
		{
			float half = ScreenVisibleWidth * 0.5f;
			// 内容范围优先，没设置过就退回设计稿宽度
			float left = _contentMaxX > _contentMinX ? _contentMinX : 0f;
			float right = _contentMaxX > _contentMinX ? _contentMaxX : CanvasWidth;
			float min = left + half;
			float max = right - half;
			if (max < min)
			{
				// 关卡比屏幕窄：居中
				_cameraX = (left + right) * 0.5f;
			}
			else
			{
				float clampTarget = _targetX < min ? min : (_targetX > max ? max : _targetX);
				_targetX = clampTarget;
				_cameraX = Mathf.Lerp(_cameraX, clampTarget, Mathf.Clamp01(deltaTime * 8f));
			}

			Apply();
		}

		private void Apply()
		{
			Camera.transform.position = new Vector3(_cameraX, CanvasHeight * 0.5f, -10f);
		}

		/// <summary>屏幕坐标 → 设计稿坐标</summary>
		public Vector2 ScreenToDesign(Vector3 screen)
		{
			Vector3 world = Camera.ScreenToWorldPoint(screen);
			return new Vector2(world.x, world.y);
		}
	}
}
