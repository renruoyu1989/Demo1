using System;
using System.Security;
using UnityEngine;
using Mono.Xml;
using YooAsset;
using MotionFramework;

public abstract class AssetXml
{
	private AssetHandle _handle;
	private System.Action _userCallback;
	private bool _isLoading;
	protected SecurityElement _xml;

	/// <summary>
	/// 是否已加载完成（无论成功失败）
	/// </summary>
	public bool IsDone { get; private set; }

	/// <summary>
	/// 是否加载失败
	/// </summary>
	public bool IsFailed { get; private set; }

	/// <summary>
	/// 资源地址
	/// </summary>
	public string Location { private set; get; }


	public void Init(string location)
	{
		Location = location;
	}
	public void Load(System.Action callback)
	{
		// 已经加载完成，直接回调（避免调用方永久等待）
		if (IsDone)
		{
			callback?.Invoke();
			return;
		}

		// 加载中，追加回调而不是丢弃
		if (_isLoading)
		{
			_userCallback += callback;
			return;
		}

		_isLoading = true;
		_userCallback = callback;
		_handle = YooAssets.LoadAssetAsync<TextAsset>(Location);
		_handle.Completed += Handle_Completed;
	}

	private void Handle_Completed(AssetHandle obj)
	{
		if (_handle != null)
			_handle.Completed -= Handle_Completed;

		TextAsset temp = (_handle != null && obj.Status == EOperationStatus.Succeed) ? _handle.AssetObject as TextAsset : null;
		if (temp == null)
		{
			IsFailed = true;
			Debug.LogError($"Failed to load xml {Location}. Status : {obj.Status}");
		}
		else
		{
			try
			{
				SecurityParser sp = new SecurityParser();
				sp.LoadXml(temp.text);
				_xml = sp.ToXml();

				// 解析数据
				if (_xml != null)
					ParseData();
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed to parse xml {Location}. Exception : {ex.ToString()}");
			}
		}

		// 注意：为了节省内存这里立即释放了资源
		if (_handle != null)
		{
			_handle.Release();
			_handle = null;
		}

		IsDone = true;
		_isLoading = false;

		// 取出并清空回调，避免回调内再次 Load 时被复用
		System.Action callback = _userCallback;
		_userCallback = null;
		callback?.Invoke();
	}

	/// <summary>
	/// 解析数据
	/// </summary>
	protected abstract void ParseData();
}
