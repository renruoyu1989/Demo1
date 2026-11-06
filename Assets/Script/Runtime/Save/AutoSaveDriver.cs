using UnityEngine;

/// <summary>
/// 自动存档驱动
/// 注意：切后台、失去焦点、退出游戏时触发一次存档
/// </summary>
public class AutoSaveDriver : MonoBehaviour
{
	private static AutoSaveDriver _instance;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void CreateInstance()
	{
		if (_instance != null)
			return;

		GameObject go = new GameObject("[AutoSaveDriver]");
		DontDestroyOnLoad(go);
		_instance = go.AddComponent<AutoSaveDriver>();
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
			SaveManager.RequestSave();
	}
	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus == false)
			SaveManager.RequestSave();
	}
	private void OnApplicationQuit()
	{
		SaveManager.RequestSave();
	}
}
