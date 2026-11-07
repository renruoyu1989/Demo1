using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using MotionFramework.Event;

namespace MotionFramework.Window
{
	public abstract class UIRoot : IEnumerator
	{
		private AssetHandle _handle;
		private bool _isLoadAsset = false;

		/// <summary>
		/// 实例化对象
		/// </summary>
		public GameObject Go { private set; get; }

		/// <summary>
		/// 是否加载完毕
		/// </summary>
		public bool IsDone { get { return _handle != null && _handle.IsDone; } }

		/// <summary>
		/// 是否准备完毕
		/// </summary>
		public bool IsPrepare { get { return Go != null; } }

		/// <summary>
		/// UI桌面
		/// </summary>
		public abstract GameObject UIDesktop { protected set; get; }

		/// <summary>
		/// UI相机
		/// </summary>
		public abstract Camera UICamera { protected set; get; }


		internal void InternalLoad(string location)
		{
			if (_isLoadAsset)
				return;

			_isLoadAsset = true;
			_handle = YooAssets.LoadAssetAsync<GameObject>(location);
			_handle.Completed += Handle_Completed;
		}
		internal void InternalDestroy()
		{
			if (_handle != null)
			{
				_handle.Completed -= Handle_Completed;
				_handle.Release();
				_handle = null;
			}

			if (Go != null)
			{
				GameObject.Destroy(Go);
				Go = null;
			}
		}
		private void Handle_Completed(AssetHandle obj)
		{
			if (obj.Status != EOperationStatus.Succeed || _handle == null || _handle.AssetObject == null)
			{
				MotionLog.Error($"Failed to load UIRoot asset, status = {obj.Status}");
				_isLoadAsset = false;
				if (_handle != null)
				{
					_handle.Completed -= Handle_Completed;
					_handle.Release();
					_handle = null;
				}
				return;
			}

			// 实例化对象
			Go = _handle.InstantiateSync();
			Go.transform.position = new Vector3(1000, 0, 1);
			GameObject.DontDestroyOnLoad(Go);

			// 调用重载函数
			OnAssetLoad(Go);
		}
		protected abstract void OnAssetLoad(GameObject go);

		#region 异步相关
		bool IEnumerator.MoveNext()
		{
			return !IsDone;
		}
		void IEnumerator.Reset()
		{
		}
		object IEnumerator.Current
		{
			get { return null; }
		}
		#endregion
	}
}
