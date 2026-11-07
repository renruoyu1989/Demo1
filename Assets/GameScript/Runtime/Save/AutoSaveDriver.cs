using UnityEngine;

/// <summary>
/// 自动存档驱动
/// 注意：切后台、失去焦点、退出游戏时触发一次存档
/// </summary>
public class AutoSaveDriver : MonoBehaviour
{
	/// <summary>
	/// 两次自动存档的最小间隔（秒）
	/// 切后台时 Pause 与 Focus 往往连续触发，需要节流
	/// </summary>
	private const float MinSaveInterval = 2f;

	private static AutoSaveDriver _instance;
	private float _lastSaveTime = -999f;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void CreateInstance()
	{
		if (_instance != null)
			return;

		GameObject go = new GameObject("[AutoSaveDriver]");
		DontDestroyOnLoad(go);
		_instance = go.AddComponent<AutoSaveDriver>();
	}

	private void RequestSave()
	{
		// 节流：避免同一时刻重复做全量序列化与磁盘替换
		if (Time.unscaledTime - _lastSaveTime < MinSaveInterval)
			return;

		_lastSaveTime = Time.unscaledTime;
		SaveManager.RequestSave();
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
			RequestSave();
	}
	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus == false)
			RequestSave();
	}
	private void OnApplicationQuit()
	{
		// 退出属于最后机会，不做节流
		SaveManager.RequestSave();
	}
}
