using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 玩家数据（单机版，直接参与存档）
	/// </summary>
	[Serializable]
	public class DataPlayer : DataBase
	{
		/// <summary>
		/// 账号（单机版用来区分存档）
		/// </summary>
		public string Account;

		/// <summary>
		/// 昵称
		/// </summary>
		public string NickName;

		/// <summary>
		/// 等级
		/// </summary>
		public int Level;

		/// <summary>
		/// 经验
		/// </summary>
		public int Exp;

		/// <summary>
		/// 金币
		/// </summary>
		public long Gold;

		/// <summary>
		/// 创建时间（UTC Ticks）
		/// </summary>
		public long CreateTicks;

		/// <summary>
		/// 最近登录时间（UTC Ticks）
		/// </summary>
		public long LastLoginTicks;

		public override void Start()
		{
		}
		public override void Update()
		{
		}
		public override void Destroy()
		{
		}

		/// <summary>
		/// 设置账号（创建角色时调用）
		/// </summary>
		public void SetAccount(string account)
		{
			if (string.IsNullOrEmpty(account))
				account = "Player";

			Account = account;
			if (string.IsNullOrEmpty(NickName))
				NickName = account;
			if (CreateTicks == 0)
				CreateTicks = DateTime.UtcNow.Ticks;
			LastLoginTicks = DateTime.UtcNow.Ticks;
		}

		public void AddGold(long value)
		{
			Gold += value;
			if (Gold < 0)
				Gold = 0;
		}

		public void AddExp(int value)
		{
			Exp += value;
			if (Exp < 0)
				Exp = 0;

			// 简单的升级规则：每级需要 100 * 当前等级经验
			while (Level > 0 && Exp >= Level * 100)
			{
				Exp -= Level * 100;
				Level++;
			}
			if (Level <= 0)
				Level = 1;
		}

		#region 存档
		public override string CollectSaveData()
		{
			return JsonUtility.ToJson(this);
		}
		public override void ApplySaveData(string json)
		{
			JsonUtility.FromJsonOverwrite(json, this);
		}
		public override void ResetData()
		{
			Account = string.Empty;
			NickName = string.Empty;
			Level = 1;
			Exp = 0;
			Gold = 0;
			CreateTicks = 0;
			LastLoginTicks = 0;
		}
		#endregion
	}
}
