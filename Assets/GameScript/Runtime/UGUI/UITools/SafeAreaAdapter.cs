// 来源：D:\rxgj_unity\Client\Assets\GameScript\Runtime\UGUI\UITools\SafeAreaAdapter.cs
using UnityEngine;

/// <summary>
/// 异形屏安全区适配：挂在顶部/底部 UI 节点上，按 SafeArea 偏移位置或高度
/// </summary>
public class SafeAreaAdapter : MonoBehaviour
{
    [SerializeField] int _direction = 1;//1是在顶部，需要向下移动，-1是底部，需要向上移动
    private Vector2 _srcPos;
    private float _srcHeight;
    private RectTransform rectTran;

    void Awake()
    {
        rectTran = GetComponent<RectTransform>();
        _srcPos = rectTran.anchoredPosition;
        _srcHeight = rectTran.rect.height;
        Adapt();
    }

    void Adapt()
    {
        Vector2 offset = DeviceHelper.GetSafeAreaOffset(_direction);

        // 存在上下跟随适配的问题
        if (rectTran.anchorMin.y == 0 && rectTran.anchorMax.y == 1)
        {
            rectTran.SetSizeDeltaHeight(_srcHeight + offset.y);
            rectTran.anchoredPosition = _srcPos + new Vector2(0, offset.y * rectTran.pivot.y);
        }
        else
        {
            rectTran.anchoredPosition = new Vector2(_srcPos.x, _srcPos.y + offset.y);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        //某些安卓设备，刚启动时非安全区显示黑边，获取到的offset为0，但是切后台再回来，非安全区又正常显示了，此时应重新获取一下offset，重新适配
        if (hasFocus)
        {
            Adapt();
        }
    }
}
