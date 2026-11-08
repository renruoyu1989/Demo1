// 来源：D:\rxgj_unity\Client\Assets\GameScript\Runtime\UGUI\UITools\UIImageFullScreen.cs
// 移植到本项目时按横屏基准把 StandardWidth/StandardHeight 由 1080x1920 改为 1920x1080
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背景图全屏拉伸
/// 配合 UIRoot.prefab 上的 CanvasScaler(referenceResolution=1920x1080, ScreenMatchMode=Expand, Match=0) 使用
/// </summary>
[RequireComponent(typeof(Image))]
public class UIImageFullScreen : MonoBehaviour
{
	public enum FullType
	{
		ExpandFull = 0,
		MatchWidth = 1,
		MatchHeight = 2,
	}

	private const float StandardWidth = 1920;
	private const float StandardHeight = 1080;

	public FullType fullType = FullType.ExpandFull;
	public bool ResizeByScale = false;

	void Start()
	{
		RectTransform thisTrans = this.transform as RectTransform;
		FullScreen(thisTrans, fullType, ResizeByScale);
	}

	/// <summary>
	/// 设置全屏尺寸
	/// </summary>
	public static void FullScreen(RectTransform rectTran, FullType fullType, bool resizeByScale = false)
	{
		float screenWidth = Screen.width;
		float screenHeight = Screen.height;
		float standardRate = StandardWidth / StandardHeight;
		float screenRate = screenWidth / screenHeight;

		float rectWidth = rectTran.sizeDelta.x;
		float rectHeight = rectTran.sizeDelta.y;

		float width = rectWidth;
		float height = rectHeight;
		float rectRate = width / height;

		if (fullType == FullType.MatchWidth || (fullType == FullType.ExpandFull && screenRate >= rectRate))
		{
			float referWidth = StandardWidth;
			if (screenRate >= standardRate) referWidth = StandardWidth * screenRate / standardRate;

			width = Mathf.FloorToInt(referWidth);
			height = Mathf.FloorToInt(height * width / rectWidth);
		}
		else if (fullType == FullType.MatchHeight || (fullType == FullType.ExpandFull && screenRate < rectRate))
		{
			float referHeight = StandardHeight;
			if (screenRate <= standardRate) referHeight = StandardHeight * standardRate / screenRate;

			height = Mathf.FloorToInt(referHeight);
			width = Mathf.FloorToInt(width * height / rectHeight);
		}

		if (resizeByScale)
		{
			rectTran.localScale = new Vector3(Mathf.FloorToInt(width) / rectWidth, Mathf.FloorToInt(height) / rectHeight, 1);
		}
		else
		{
			rectTran.sizeDelta = new Vector2(Mathf.FloorToInt(width), Mathf.FloorToInt(height));
		}
	}
}
