using System;
using System.Collections.Generic;
using MotionFramework.Window;
using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 业务窗口基类
	/// 在框架 CanvasWindow 之上补充业务层能力：常驻标记、默认字体、业务事件监听
	/// </summary>
	public abstract class WindowBase : CanvasWindow
	{
		/// <summary>
		/// 获取界面默认字体（用于代码动态创建的文本）
		/// </summary>
		protected Font GetDefaultFont()
		{
			return UIFontProvider.DefaultFont;
		}

		#region 事件相关
		private readonly Dictionary<HotfixEventMessageTag, List<Action<IHotfixEventMessage>>> _cachedListener = new Dictionary<HotfixEventMessageTag, List<Action<IHotfixEventMessage>>>();

		/// <summary>
		/// 添加一个事件监听（窗口销毁时自动移除）
		/// </summary>
		protected void AddEventListener(HotfixEventMessageTag eventTag, Action<IHotfixEventMessage> listener)
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
