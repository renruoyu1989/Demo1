﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hotfix
{
	public class EntityMonster : EntityCharacter
	{
		private readonly int _monsterID;

		public EntityMonster(int entityID, int monsterID) : base(entityID)
		{
			_monsterID = monsterID;
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			// 初始化角色数据
			CfgMonsterTab table = CfgMonster.Instance.GetCfgTab(_monsterID);
			CharData.InitData(table.BodyRadius, table.MoveSpeed, table.Hp, table.Mp, table.Damage, table.Armor);
		}
	}
}