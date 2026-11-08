using System;
using System.Collections;
using System.Collections.Generic;
using MotionFramework.Event;

namespace MotionFramework.Data
{
	/// <summary>
	/// 业务数据基类
	/// 生命周期：Create -> Update ... -> Destroy
	/// 存档由 DataManager 统一收集（CollectSaveData / ApplySaveData）
	/// </summary>
	public abstract class DataBase
	{
		/// <summary>
		/// 存档版本号
		/// </summary>
		public int ArchiveVersion = 1;

		/// <summary>
		/// 事件组：在数据销毁时自动移除注册的事件
		/// </summary>
		protected readonly EventGroup _eventGroup = new EventGroup();

		protected bool isDirty = false;

		/// <summary>
		/// 存档键（默认使用类名，业务可重写）
		/// </summary>
		public virtual string SaveKey
		{
			get { return GetType().Name; }
		}

		/// <summary>
		/// 创建数据（由 DataManager 调用）
		/// </summary>
		public void Create()
		{
			OnCreate();
		}

		/// <summary>
		/// 销毁数据（由 DataManager 调用）
		/// </summary>
		public void Destroy()
		{
			_eventGroup.RemoveAllListener();
			OnDestroy();
		}

		/// <summary>
		/// 轮询数据（由 DataManager 调用）
		/// </summary>
		public void Update()
		{
			if (isDirty)
			{
				isDirty = false;
				OnDirty();
			}

			OnUpdate();
		}

		public virtual void OnCreate() { }
		public virtual void OnDestroy() { }
		public virtual void OnUpdate() { }

		/// <summary>
		/// 数据被标记为脏时的回调
		/// </summary>
		protected virtual void OnDirty() { }

		/// <summary>
		/// 标记数据已变更
		/// </summary>
		public void SetDirty()
		{
			isDirty = true;
		}

		#region 存档相关
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
	}
}
