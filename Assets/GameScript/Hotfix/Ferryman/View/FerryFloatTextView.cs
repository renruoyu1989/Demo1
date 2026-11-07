using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 棋盘上的浮字（"3xCombo +150"），向上飘并淡出
	/// 用世界空间 TextMesh 实现，避免和 UGUI Canvas 耦合
	/// </summary>
	public class FerryFloatTextView
	{
		private const float Duration = 1.2f;
		private const float RiseSpeed = 55f;

		private GameObject _go;
		private TextMesh _text;
		private MeshRenderer _renderer;
		private float _life;
		private Color _color;

		public static FerryFloatTextView Spawn(Transform parent, Vector3 position, string content, int fontSize, Color color)
		{
			FerryFloatTextView view = new FerryFloatTextView();

			view._go = new GameObject("FloatText");
			view._go.transform.SetParent(parent, false);
			view._go.transform.position = position;

			view._text = view._go.AddComponent<TextMesh>();
			Font font = UIFontProvider.DefaultFont;
			if (font != null)
				view._text.font = font;
			view._text.text = content;
			view._text.fontSize = fontSize;
			view._text.characterSize = 0.12f;
			view._text.anchor = TextAnchor.LowerCenter;
			view._text.alignment = TextAlignment.Center;
			view._text.color = color;

			view._renderer = view._go.GetComponent<MeshRenderer>();
			if (view._renderer != null)
				view._renderer.sortingOrder = FerryDefine.SortingFloatText;

			view._color = color;
			view._life = 0f;
			return view;
		}

		/// <summary>返回 false 表示已播完，可以回收</summary>
		public bool Tick(float deltaTime)
		{
			if (_go == null)
				return false;

			_life += deltaTime;
			if (_life >= Duration)
			{
				Destroy();
				return false;
			}

			Vector3 position = _go.transform.position;
			position.y += RiseSpeed * deltaTime;
			_go.transform.position = position;

			float alpha = 1f - _life / Duration;
			Color color = _color;
			color.a = Mathf.Clamp01(alpha);
			if (_text != null)
				_text.color = color;

			return true;
		}

		public void Destroy()
		{
			if (_go != null)
				Object.Destroy(_go);
			_go = null;
		}
	}
}
