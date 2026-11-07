using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

#if UNITY_2020_1_OR_NEWER
using UnityEditor.SceneManagement;
#elif UNITY_2018_4_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

public class UIPanelMonitor : Editor
{
	[InitializeOnLoadMethod]
	static void StartInitializeOnLoadMethod()
	{
		// 注意：必须先解绑再绑定，避免关闭 Domain Reload 时重复订阅，也不能用 = 覆盖别人的监听
		PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
		PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;

#if UNITY_2018_4_OR_NEWER
		// 监听新的Prefab系统
		PrefabStage.prefabSaving -= OnPrefabSaving;
		PrefabStage.prefabSaving += OnPrefabSaving;
#endif
	}

	static void OnPrefabInstanceUpdated(GameObject go)
	{
		if (go == null)
			return;

		UIManifest manifest = go.GetComponent<UIManifest>();
		if (manifest != null)
			manifest.Refresh();
	}

#if UNITY_2018_4_OR_NEWER
	static void OnPrefabSaving(GameObject go)
	{
		PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
		if (stage != null && go != null)
		{
			UIManifest manifest = go.GetComponent<UIManifest>();
			if (manifest != null)
				manifest.Refresh();
		}
	}
#endif
}
