﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hotfix
{
	[Window(EWindowType.UIMain, EWindowLayer.Panel)]
	public class UIMain : UIWindow
	{

		public override void OnCreate()
		{
			AddButtonListener("UIMain/Meum/Button (Settings)", OnClickSetting);
			AddButtonListener("UIMain/Spell/Button (Attack)", OnClickSkill1);
			AddButtonListener("UIMain/Spell/Button (Shield)", OnClickSkill2);
			AddButtonListener("UIMain/Spell/Button (Swords)", OnClickSkill3);
		}
		public override void OnDestroy()
		{
		}
		public override void OnRefresh()
		{
		}
		public override void OnUpdate()
		{
#if UNITY_EDITOR
			if (Input.GetKeyDown(KeyCode.Alpha1))
				OnClickSkill1();
			if (Input.GetKeyDown(KeyCode.Alpha2))
				OnClickSkill2();
			if (Input.GetKeyDown(KeyCode.Alpha3))
				OnClickSkill3();
#endif
		}

		private void OnClickSetting()
		{
			UIManager.Instance.OpenWindow(EWindowType.UISetting);
		}
		private void OnClickSkill1()
		{
			BattleEvent.PlayerSpell msg = new BattleEvent.PlayerSpell();
			msg.SkillID = 1001;
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.BattleEvent, msg);
		}
		private void OnClickSkill2()
		{
			BattleEvent.PlayerSpell msg = new BattleEvent.PlayerSpell();
			msg.SkillID = 1003;
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.BattleEvent, msg);
		}
		private void OnClickSkill3()
		{
			BattleEvent.PlayerSpell msg = new BattleEvent.PlayerSpell();
			msg.SkillID = 1002;
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.BattleEvent, msg);
		}
	}
}