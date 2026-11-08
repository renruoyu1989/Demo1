using System;
using System.Collections.Generic;
using MotionFramework.Data;
using UnityEngine;

namespace Hotfix
{
	/// <summary>单关记录</summary>
	[Serializable]
	public class FerryLevelRecord
	{
		public int levelId;
		public int bestScore;
		public float bestTimeSeconds;
		public bool cleared;
	}

	/// <summary>过河游戏的存档数据</summary>
	[Serializable]
	public class FerrySaveData
	{
		public List<FerryLevelRecord> records = new List<FerryLevelRecord>();
		public int totalScore;
		public int lastLevelId;
	}

	/// <summary>
	/// 过河游戏的进度存档：解锁、通关、最高分、总分
	/// </summary>
	[Data(typeof(DataFerryProfile), true)]
	public class DataFerryProfile : DataBase
	{
		private FerrySaveData _data = new FerrySaveData();

		public override void OnCreate()
		{
		}

		public override void OnUpdate()
		{
		}

		public override void OnDestroy()
		{
		}

		#region 存档
		public override string CollectSaveData()
		{
			return JsonUtility.ToJson(_data);
		}

		public override void ApplySaveData(string json)
		{
			if (string.IsNullOrEmpty(json))
				return;

			FerrySaveData data = JsonUtility.FromJson<FerrySaveData>(json);
			if (data != null)
				_data = data;
		}

		public override void ResetData()
		{
			_data = new FerrySaveData();
		}
		#endregion

		#region 进度查询
		public int TotalScore { get { return _data.totalScore; } }
		public int LastLevelId { get { return _data.lastLevelId; } }

		private FerryLevelRecord GetRecord(int levelId)
		{
			for (int i = 0; i < _data.records.Count; i++)
			{
				if (_data.records[i].levelId == levelId)
					return _data.records[i];
			}
			return null;
		}

		public bool IsCleared(int levelId)
		{
			FerryLevelRecord record = GetRecord(levelId);
			return record != null && record.cleared;
		}

		public int GetBestScore(int levelId)
		{
			FerryLevelRecord record = GetRecord(levelId);
			return record == null ? 0 : record.bestScore;
		}

		public float GetBestTime(int levelId)
		{
			FerryLevelRecord record = GetRecord(levelId);
			return record == null ? 0f : record.bestTimeSeconds;
		}

		/// <summary>
		/// 关卡是否解锁：经典/连击各自按地图顺序逐关解锁
		/// </summary>
		public bool IsUnlocked(int levelId)
		{
			CfgLevelTab level = FerryConfigService.Instance.GetLevel(levelId);
			if (level == null)
				return false;

			List<int> order = GetOrderedLevelIds(level.Kind);
			if (order.Count == 0)
				return true;

			if (order[0] == levelId)
				return true;

			for (int i = 0; i < order.Count; i++)
			{
				if (order[i] != levelId)
					continue;

				return i > 0 && IsCleared(order[i - 1]);
			}

			return true;
		}

		/// <summary>
		/// 取某模式下的关卡顺序（按地图节点，没有节点时按 id）
		/// </summary>
		public List<int> GetOrderedLevelIds(int kind)
		{
			List<int> list = new List<int>();
			int mode = kind == FerryDefine.KindTutorial ? 1 : 0;
			List<CfgLevelMapTab> nodes = FerryConfigService.Instance.GetMapNodes(mode);
			for (int i = 0; i < nodes.Count; i++)
			{
				CfgLevelTab level = FerryConfigService.Instance.GetLevel(nodes[i].LevelID);
				if (level != null && level.Kind == kind && list.Contains(level.Id) == false)
					list.Add(level.Id);
			}

			if (list.Count == 0)
			{
				List<CfgLevelTab> levels = FerryConfigService.Instance.GetLevels(kind);
				for (int i = 0; i < levels.Count; i++)
				{
					list.Add(levels[i].Id);
				}
			}

			return list;
		}

		/// <summary>下一个尚未通关的关卡，全部通关则返回 0</summary>
		public int GetNextPlayableLevelId(int kind)
		{
			List<int> order = GetOrderedLevelIds(kind);
			for (int i = 0; i < order.Count; i++)
			{
				if (IsCleared(order[i]) == false)
					return order[i];
			}
			return order.Count > 0 ? order[0] : 0;
		}
		#endregion

		#region 记录
		/// <summary>记录一次结算结果</summary>
		public void RecordResult(int levelId, bool isWin, int score)
		{
			if (levelId <= 0)
				return;

			FerryLevelRecord record = GetRecord(levelId);
			if (record == null)
			{
				record = new FerryLevelRecord();
				record.levelId = levelId;
				_data.records.Add(record);
			}

			if (score > record.bestScore)
			{
				_data.totalScore += score - record.bestScore;
				record.bestScore = score;
			}

			if (isWin)
			{
				record.cleared = true;

				float elapsed = FerryGameFlow.Instance.Session != null
					? FerryGameFlow.Instance.Session.ElapsedSeconds
					: 0f;
				if (record.bestTimeSeconds <= 0f || elapsed < record.bestTimeSeconds)
					record.bestTimeSeconds = elapsed;
			}

			_data.lastLevelId = levelId;
		}
		#endregion
	}
}
