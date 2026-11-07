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
	protected SecurityElement _xml;

	/// <summary>
	/// 是否完成
	/// </summary>
	public bool IsDone
	{
		get
		{
			return _handle != null && _handle.IsDone;
		}
	}

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
		if (_userCallback != null)
			return;

		_userCallback = callback;
		_handle = YooAssets.LoadAssetAsync<TextAsset>(Location);
		_handle.Completed += Handle_Completed;
	}
	private void Handle_Completed(AssetHandle obj)
	{
		TextAsset temp = _handle.AssetObject as TextAsset;
		if (temp != null)
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

		_userCallback?.Invoke();
	}

	/// <summary>
	/// 解析数据
	/// </summary>
	protected abstract void ParseData();
}
