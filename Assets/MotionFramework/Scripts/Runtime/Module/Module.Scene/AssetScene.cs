//--------------------------------------------------
// Motion Framework
// Copyright©2018-2021 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using UnityEngine.SceneManagement;
using YooAsset;

namespace MotionFramework.Scene
{
	internal class AssetScene
	{
		private SceneHandle _handle;
		private System.Action<SceneHandle> _userCallback;
		private bool _isLoadScene = false;

		/// <summary>
		/// 场景地址
		/// </summary>
		public string Location { private set; get; }

		/// <summary>
		/// 场景加载进度（0-100）
		/// </summary>
		public int Progress
		{
			get
			{
				if (_handle == null)
					return 0;
				return (int)(_handle.Progress * 100f);
			}
		}

		/// <summary>
		/// 场景是否加载失败
		/// </summary>
		public bool IsFailed { private set; get; }

		/// <summary>
		/// 场景是否加载完毕
		/// </summary>
		public bool IsDone
		{
			get
			{
				if (_handle == null)
					return false;
				return _handle.IsDone;
			}
		}


		public AssetScene(string location)
		{
			Location = location;
		}
		public void Load(bool isAdditive, bool activeOnLoad, System.Action<SceneHandle> callback)
		{
			if (_isLoadScene)
				return;

			_isLoadScene = true;
			_userCallback = callback;
			MotionLog.Log($"Begin to load scene : {Location}");

			LoadSceneMode sceneMode = isAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
			// suspendLoad = !activeOnLoad：不自动激活时，由调用方通过 UnSuspend 手动激活
			_handle = YooAssets.LoadSceneAsync(Location, sceneMode, LocalPhysicsMode.None, activeOnLoad == false);
			_handle.Completed += Handle_Completed;
		}

		/// <summary>
		/// 激活被挂起的场景（activeOnLoad 为 false 时需要手动调用）
		/// </summary>
		public void UnSuspend()
		{
			if (_handle != null)
				_handle.UnSuspend();
		}
		public void UnLoad()
		{
			if (_isLoadScene)
			{
				_isLoadScene = false;
				_userCallback = null;
				if (_handle != null)
					_handle.UnloadAsync();
			}
		}

		// 资源回调
		private void Handle_Completed(SceneHandle obj)
		{
			if (obj.Status != EOperationStatus.Succeed)
			{
				IsFailed = true;
				MotionLog.Error($"Failed to load scene : {Location}, status = {obj.Status}, error = {obj.LastError}");
			}

			_userCallback?.Invoke(_handle);
		}
	}
}
