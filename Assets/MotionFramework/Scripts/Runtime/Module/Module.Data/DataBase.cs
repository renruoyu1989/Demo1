using System;
using System.Collections;
using System.Collections.Generic;
using MotionFramework.CollectAttribute;
using MotionFramework.Event;
using UnityEngine;

namespace MotionFramework.Data
{
	public abstract class DataBase
	{
		public int ArchiveVersion = 1;
		protected readonly EventGroup _eventGroup = new EventGroup();
		protected bool isDirty = false;
		protected string storageKey;

		public void Create()
		{
			DataAttribute attr = AssemblyManager.Instance.GetDataAttribute(GetType());

			if (attr.IsSave)
			{
				storageKey = attr.LocalDataKey;
			}

			OnCreate();
		}
		public void Destroy()
		{
			_eventGroup.RemoveAllListener();
			OnDestroy();
		}
		public void Update()
		{
			if (isDirty)
			{
				SaveLocal();
				isDirty = false;
			}

			OnUpdate();
		}
		public virtual void OnCreate() { }
		public virtual void OnDestroy() { }
		public virtual void OnUpdate() { }
		public void SetDirty()
		{
			isDirty = true;
		}
		protected virtual void SaveLocal()
		{
			ArchiveVersion++;
			string jsonStr = ConvertToJson();
			PlayerPrefs.SetString(storageKey, jsonStr);
			// PlayerPrefs.Save();
		}
		protected virtual string ConvertToJson()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}
	}
}