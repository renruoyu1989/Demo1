using System;
using System.Collections;
using System.Collections.Generic;

namespace Hotfix
{
	public abstract class DataBase
	{
		public abstract void Start();
		public abstract void Update();
		public abstract void Destroy();

		#region 存档相关
		/// <summary>
		/// 存档键（默认使用类名）
		/// </summary>
		public virtual string SaveKey
		{
			get { return GetType().Name; }
		}

		/// <summary>
		/// 收集存档数据（返回null代表不参与存档）
		/// </summary>
		public virtual string CollectSaveData()
		{
			return null;
		}

		/// <summary>
		/// 应用存档数据
		/// </summary>
		public virtual void ApplySaveData(string json)
		{
		}

		/// <summary>
		/// 重置为默认数据（没有存档时调用）
		/// </summary>
		public virtual void ResetData()
		{
		}
		#endregion

		#region 事件相关
		private readonly Dictionary<HotfixEventMessageTag, List<Action<IHotfixEventMessage>>> _cachedListener = new Dictionary<HotfixEventMessageTag, List<Action<IHotfixEventMessage>>>();

		/// <summary>
		/// 添加一个事件监听
		/// </summary>
		protected void AddEventListener(HotfixEventMessageTag eventTag, System.Action<IHotfixEventMessage> listener)
		{
			if (_cachedListener.ContainsKey(eventTag) == false)
				_cachedListener.Add(eventTag, new List<Action<IHotfixEventMessage>>());

			if (_cachedListener[eventTag].Contains(listener) == false)
			{
				_cachedListener[eventTag].Add(listener);
				HotfixEventManager.Instance.AddListener(eventTag, listener);
			}
			else
			{
				HotfixLogger.Warning($"Event listener is exist : {eventTag}");
			}
		}

		/// <summary>
		/// 移除所有缓存的事件监听
		/// </summary>
		public void RemoveAllListener()
		{
			foreach (var pair in _cachedListener)
			{
				HotfixEventMessageTag eventTag = pair.Key;
				for (int i = 0; i < pair.Value.Count; i++)
				{
					HotfixEventManager.Instance.RemoveListener(eventTag, pair.Value[i]);
				}
				pair.Value.Clear();
			}
			_cachedListener.Clear();
		}
		#endregion
	}
}
