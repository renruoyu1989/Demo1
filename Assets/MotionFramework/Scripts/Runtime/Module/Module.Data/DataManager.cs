using System;
using System.Collections.Generic;
using System.Reflection;

namespace MotionFramework.Data
{
	/// <summary>
	/// 单条业务数据的存档片段
	/// </summary>
	[Serializable]
	public class DataSaveEntry
	{
		public string Key;
		public string Json;
	}

	/// <summary>
	/// 游戏存档数据结构
	/// </summary>
	[Serializable]
	public class GameSaveData
	{
		public string Account;
		public long LastSaveTicks;
		public List<DataSaveEntry> Entries = new List<DataSaveEntry>();
	}

	/// <summary>
	/// 数据管理器
	/// 通过 [Data] 特性自动收集所有业务数据类，统一管理生命周期
	/// 存档的落盘方式由业务层注入（框架层不依赖具体存档实现）
	/// </summary>
	public class DataManager : ModuleSingleton<DataManager>, IModule
	{
		private readonly List<DataBase> _allDatas = new List<DataBase>();
		private string _currentSlot;

		/// <summary>
		/// 当前使用的存档槽
		/// </summary>
		public string CurrentSlot
		{
			get
			{
				if (string.IsNullOrEmpty(_currentSlot))
					_currentSlot = DefaultSlotGetter != null ? DefaultSlotGetter() : "player";
				return _currentSlot;
			}
			private set
			{
				_currentSlot = value;
			}
		}

		/// <summary>
		/// 当前账号（单机版用来区分存档）
		/// </summary>
		public string Account { private set; get; }

		#region 存档后端（由业务层注入）
		public Func<string, GameSaveData, bool> SaveHandler { get; set; }
		public Func<string, GameSaveData> LoadHandler { get; set; }
		public Func<string, bool> ExistsHandler { get; set; }
		public Func<string, bool> DeleteHandler { get; set; }
		public Func<string> DefaultSlotGetter { get; set; }
		#endregion

		void IModule.OnCreate(System.Object createParam)
		{
			// 收集并创建所有数据类
			CreateAllData();

			for (int i = 0; i < _allDatas.Count; i++)
			{
				_allDatas[i].Create();
			}
		}

		void IModule.OnUpdate()
		{
			for (int i = 0; i < _allDatas.Count; i++)
			{
				_allDatas[i].Update();
			}
		}

		void IModule.OnGUI()
		{
		}

		void IModule.OnDestroy()
		{
			ClearAll();
		}

		/// <summary>
		/// 通过 [Data] 特性收集所有业务数据类
		/// </summary>
		private void CreateAllData()
		{
			Type[] types = CollectDataTypes();
			for (int i = 0; i < types.Length; i++)
			{
				DataBase data = Activator.CreateInstance(types[i]) as DataBase;
				if (data == null)
				{
					MotionLog.Error($"Data {types[i].FullName} create instance failed.");
					continue;
				}
				_allDatas.Add(data);
			}
		}

		/// <summary>
		/// 扫描所有已加载程序集里的 [Data] 类型
		/// </summary>
		private static Type[] CollectDataTypes()
		{
			List<Type> result = new List<Type>();
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				Type[] types;
				try
				{
					types = assemblies[i].GetTypes();
				}
				catch (ReflectionTypeLoadException e)
				{
					types = e.Types;
				}

				for (int j = 0; j < types.Length; j++)
				{
					Type type = types[j];
					if (type == null || type.IsAbstract || type.IsInterface)
						continue;
					if (typeof(DataBase).IsAssignableFrom(type) == false)
						continue;
					if (type.GetCustomAttribute<DataAttribute>() == null)
						continue;

					result.Add(type);
				}
			}
			return result.ToArray();
		}

		/// <summary>
		/// 销毁所有数据
		/// </summary>
		public void ClearAll()
		{
			for (int i = 0; i < _allDatas.Count; i++)
			{
				_allDatas[i].Destroy();
			}
			_allDatas.Clear();
		}

		/// <summary>
		/// 获取数据对象
		/// </summary>
		public T GetData<T>() where T : DataBase
		{
			for (int i = 0; i < _allDatas.Count; i++)
			{
				if (_allDatas[i] is T)
					return _allDatas[i] as T;
			}
			return null;
		}

		#region 存档
		/// <summary>
		/// 指定当前账号（用于区分存档文件）
		/// </summary>
		public void SetAccount(string account)
		{
			Account = account;
			if (string.IsNullOrEmpty(account) == false)
				CurrentSlot = account;
		}

		/// <summary>
		/// 是否存在存档
		/// </summary>
		public bool HasSave(string slot = null)
		{
			if (ExistsHandler == null)
				return false;
			return ExistsHandler(string.IsNullOrEmpty(slot) ? CurrentSlot : slot);
		}

		/// <summary>
		/// 保存所有数据
		/// </summary>
		public bool SaveAll(string slot = null)
		{
			if (SaveHandler == null)
			{
				MotionLog.Error("DataManager.SaveHandler is not set.");
				return false;
			}

			string saveSlot = string.IsNullOrEmpty(slot) ? CurrentSlot : slot;

			GameSaveData saveData = new GameSaveData();
			saveData.Account = Account;
			saveData.LastSaveTicks = DateTime.UtcNow.Ticks;

			for (int i = 0; i < _allDatas.Count; i++)
			{
				DataBase data = _allDatas[i];
				string json = data.CollectSaveData();
				if (string.IsNullOrEmpty(json))
					continue;

				DataSaveEntry entry = new DataSaveEntry();
				entry.Key = data.SaveKey;
				entry.Json = json;
				saveData.Entries.Add(entry);
			}

			bool result = SaveHandler(saveSlot, saveData);
			if (result)
				MotionLog.Log($"存档成功：{saveSlot}");
			else
				MotionLog.Error($"存档失败：{saveSlot}");
			return result;
		}

		/// <summary>
		/// 载入所有数据
		/// </summary>
		public bool LoadAll(string slot = null)
		{
			string saveSlot = string.IsNullOrEmpty(slot) ? CurrentSlot : slot;

			GameSaveData saveData = LoadHandler != null ? LoadHandler(saveSlot) : null;
			if (saveData == null)
			{
				// 没有存档，全部使用默认数据
				ResetAll();
				return false;
			}

			CurrentSlot = saveSlot;
			Account = saveData.Account;

			// 先全部重置，避免残留旧数据
			ResetAll();

			for (int i = 0; i < saveData.Entries.Count; i++)
			{
				DataSaveEntry entry = saveData.Entries[i];
				DataBase data = GetDataByKey(entry.Key);
				if (data == null)
				{
					MotionLog.Warning($"存档里存在未知数据：{entry.Key}");
					continue;
				}
				data.ApplySaveData(entry.Json);
			}

			MotionLog.Log($"载入存档成功：{saveSlot}");
			return true;
		}

		/// <summary>
		/// 删除存档
		/// </summary>
		public bool DeleteSave(string slot = null)
		{
			if (DeleteHandler == null)
				return false;
			return DeleteHandler(string.IsNullOrEmpty(slot) ? CurrentSlot : slot);
		}

		/// <summary>
		/// 重置所有数据为默认值
		/// </summary>
		public void ResetAll()
		{
			for (int i = 0; i < _allDatas.Count; i++)
			{
				_allDatas[i].ResetData();
			}
		}

		private DataBase GetDataByKey(string key)
		{
			for (int i = 0; i < _allDatas.Count; i++)
			{
				if (_allDatas[i].SaveKey == key)
					return _allDatas[i];
			}
			return null;
		}
		#endregion
	}
}
