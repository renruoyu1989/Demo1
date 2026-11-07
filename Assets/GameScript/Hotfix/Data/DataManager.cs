using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hotfix
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

	public class DataManager
	{
		public static readonly DataManager Instance = new DataManager();

		// 数据集合
		private readonly List<DataBase> _allDatas = new List<DataBase>();

		/// <summary>
		/// 当前使用的存档槽
		/// </summary>
		public string CurrentSlot { private set; get; }

		/// <summary>
		/// 当前账号（单机版用来区分存档）
		/// </summary>
		public string Account { private set; get; }

		private DataManager()
		{
			CurrentSlot = SaveManager.DefaultSlot;

			// 监听自动存档请求（切后台、失去焦点、退出游戏）
			SaveManager.OnRequestSave += HandleRequestSave;
		}
		public void Start()
		{
			// 显式注册所有数据类
			// 注意：不要用枚举名反射创建，那样类名/命名空间任何一处改动都会变成运行时异常
			_allDatas.Add(new DataLogin());
			_allDatas.Add(new DataPlayer());

			// 先载入存档，再初始化数据类
			LoadAll();

			for (int i = 0; i < _allDatas.Count; i++)
			{
				_allDatas[i].Start();
			}
		}
		public void Update()
		{
			for (int i = 0; i < _allDatas.Count; i++)
			{
				_allDatas[i].Update();
			}
		}
		public void ClearAll()
		{
			for (int i = 0; i < _allDatas.Count; i++)
			{
				_allDatas[i].Destroy();
				_allDatas[i].RemoveAllListener();
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
			return SaveManager.Exists(string.IsNullOrEmpty(slot) ? CurrentSlot : slot);
		}

		/// <summary>
		/// 保存所有数据
		/// </summary>
		public bool SaveAll(string slot = null)
		{
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

			bool result = SaveManager.Save<GameSaveData>(saveSlot, saveData);
			if (result)
				HotfixLogger.Log($"存档成功：{saveSlot}");
			else
				HotfixLogger.Error($"存档失败：{saveSlot}");
			return result;
		}

		/// <summary>
		/// 载入所有数据
		/// </summary>
		public bool LoadAll(string slot = null)
		{
			string saveSlot = string.IsNullOrEmpty(slot) ? CurrentSlot : slot;

			GameSaveData saveData;
			if (SaveManager.Load<GameSaveData>(saveSlot, out saveData) == false)
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
					HotfixLogger.Warning($"存档里存在未知数据：{entry.Key}");
					continue;
				}
				data.ApplySaveData(entry.Json);
			}

			HotfixLogger.Log($"载入存档成功：{saveSlot}");
			return true;
		}

		/// <summary>
		/// 删除存档
		/// </summary>
		public bool DeleteSave(string slot = null)
		{
			string saveSlot = string.IsNullOrEmpty(slot) ? CurrentSlot : slot;
			return SaveManager.Delete(saveSlot);
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

		private void HandleRequestSave()
		{
			if (_allDatas.Count == 0)
				return;

			SaveAll();
		}
		#endregion
	}
}
