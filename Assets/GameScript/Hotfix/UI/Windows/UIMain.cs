using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hotfix
{
	[Window(EWindowType.UIMain, EWindowLayer.Panel)]
	public class UIMain : UIWindow
	{
		// 玩家信息（代码动态创建，用于验证存档数据）
		private Text _playerInfoText;

		public override void OnCreate()
		{
			AddButtonListener("UIMain/Meum/Button (Settings)", OnClickSetting);
			AddButtonListener("UIMain/Spell/Button (Attack)", OnClickSkill1);
			AddButtonListener("UIMain/Spell/Button (Shield)", OnClickSkill2);
			AddButtonListener("UIMain/Spell/Button (Swords)", OnClickSkill3);

			CreatePlayerInfoText();
		}
		public override void OnDestroy()
		{
		}
		public override void OnRefresh()
		{
			RefreshPlayerInfo();
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

			// 调试：F5存档，F9模拟获得奖励（用于验证存档是否正确落盘）
			if (Input.GetKeyDown(KeyCode.F5))
			{
				DataManager.Instance.SaveAll();
				HotfixLogger.Log("手动存档完成");
			}
			if (Input.GetKeyDown(KeyCode.F9))
			{
				DataPlayer data = DataManager.Instance.GetData<DataPlayer>();
				if (data != null)
				{
					data.AddGold(100);
					data.AddExp(50);
					HotfixLogger.Log($"获得奖励：Gold={data.Gold}, Lv={data.Level}, Exp={data.Exp}");
				}
			}
#endif
			RefreshPlayerInfo();
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

		#region 玩家信息
		private void CreatePlayerInfoText()
		{
			Font font = GetDefaultFont();
			if (font == null)
			{
				HotfixLogger.Warning("UIMain : not found font, skip player info.");
				return;
			}

			GameObject go = new GameObject("Text (PlayerInfo)");
			go.transform.SetParent(Go.transform, false);

			RectTransform rect = go.AddComponent<RectTransform>();
			rect.anchorMin = new Vector2(0, 1);
			rect.anchorMax = new Vector2(0, 1);
			rect.pivot = new Vector2(0, 1);
			rect.anchoredPosition = new Vector2(20, -20);
			rect.sizeDelta = new Vector2(700, 40);

			_playerInfoText = go.AddComponent<Text>();
			_playerInfoText.font = font;
			_playerInfoText.fontSize = 22;
			_playerInfoText.color = Color.white;
			_playerInfoText.alignment = TextAnchor.UpperLeft;

			RefreshPlayerInfo();
		}

		private void RefreshPlayerInfo()
		{
			if (_playerInfoText == null)
				return;

			DataPlayer data = DataManager.Instance.GetData<DataPlayer>();
			if (data == null)
				return;

			string text = $"{data.NickName}  Lv.{data.Level}  Exp:{data.Exp}  Gold:{data.Gold}  Slot:{DataManager.Instance.CurrentSlot}";
			if (_playerInfoText.text != text)
				_playerInfoText.text = text;
		}
		#endregion
	}
}
