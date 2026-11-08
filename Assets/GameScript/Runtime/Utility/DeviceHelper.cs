// 来源：D:\rxgj_unity\Client\Assets\GameScript\Runtime\Utility\DeviceHelper.cs
// 移植到本项目时把 SafeArea 的高度换算基准由竖屏 1920 改为横屏 1080
using UnityEngine;

public class DeviceHelper
{
    private static Vector2 _safeAreaTopOffset;
    private static Vector2 _safeAreaBottomOffset;

    /// <summary>
    /// 获得 safeArea 的偏移位置
    /// 返回距离顶部或者底部的距离
    /// 1 是距离顶部 其他是距离底部
    /// </summary>
    public static Vector2 GetSafeAreaOffset(int direction)
    {
        Rect safeArea = Screen.safeArea;
        float ratio = Screen.height / 1080f;

        float offset = safeArea.y + safeArea.height - Screen.height;
        offset /= ratio;
        _safeAreaTopOffset = new Vector2(0, offset);

        offset = safeArea.y;
        offset /= ratio;
        _safeAreaBottomOffset = new Vector2(0, offset);

        if (direction != 1)
        {
            return _safeAreaBottomOffset;
        }

        return _safeAreaTopOffset;
    }

    private static bool IsIpad()
    {
        string type = SystemInfo.deviceModel.ToLower().Trim();
        if (type.Length >= 3 && type.Substring(0, 3) == "ipa")
        {
            return true;
        }

        return false;
    }

    private const float STANDARD_PAD_WIDTH_HEIGHT_RATIO = 1.6f;

    private static int _isPadState = -1;

    public static bool IsPad()
    {
#if UNITY_EDITOR
        _isPadState = -1;
#endif
        if (_isPadState < 0)
        {
            var ratio = (float)Screen.height / Screen.width;
            if (Application.platform == RuntimePlatform.IPhonePlayer && IsIpad())
            {
                _isPadState = 1;
            }
            else
            {
                _isPadState = ratio <= STANDARD_PAD_WIDTH_HEIGHT_RATIO ? 1 : 0;
            }
        }

        return _isPadState == 1;
    }
}
