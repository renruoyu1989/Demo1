//--------------------------------------------------
// 自动生成  请勿修改
// 研发人员实现LANG多语言接口
//--------------------------------------------------
using System.Collections.Generic;

namespace Hotfix
{
	public class CfgAutoGenerateLanguageTab : ConfigTab
	{
		public string Lang { protected set; get; }
		public string Lang_ct { protected set; get; }
		public string Lang_en { protected set; get; }

		public CfgAutoGenerateLanguageTab(int id, string lang, string lang_ct, string lang_en)
		{
			Id = id;
			Lang = lang;
			Lang_ct = lang_ct;
			Lang_en = lang_en;
		}
	}

	public partial class CfgAutoGenerateLanguage : AssetConfig
	{
		private static CfgAutoGenerateLanguage _instance;
		public static CfgAutoGenerateLanguage Instance
		{
			get
			{
				if (_instance == null) { _instance = new CfgAutoGenerateLanguage(); _instance.Create(); }
				return _instance;
			}
		}

		private CfgAutoGenerateLanguage() { }
		public CfgAutoGenerateLanguageTab GetCfgTab(int key)
		{
			return GetTab(key) as CfgAutoGenerateLanguageTab;
		}
		public void Create()
		{
			AddElement(178940420, new CfgAutoGenerateLanguageTab(178940420, "蓝色步兵", "藍色步兵", "Blue Footman"));
			AddElement(178979727, new CfgAutoGenerateLanguageTab(178979727, "红色步兵", "紅色步兵", "Red Footman"));
			AddElement(1421729370, new CfgAutoGenerateLanguageTab(1421729370, "步兵队长", "步兵隊長", "Footman Captain"));
			AddElement(91216312, new CfgAutoGenerateLanguageTab(91216312, "战士", "戰士", "Warrior"));
			AddElement(88134997, new CfgAutoGenerateLanguageTab(88134997, "普通攻击", "普通攻擊", "Basic Attack"));
			AddElement(1351039565, new CfgAutoGenerateLanguageTab(1351039565, "跳跃", "跳躍", "Jump"));
			AddElement(-1257327582, new CfgAutoGenerateLanguageTab(-1257327582, "盾牌防守", "盾牌防守", "Shield Defense"));
			AddElement(-347589321, new CfgAutoGenerateLanguageTab(-347589321, "进入游戏", "進入遊戲", "Enter Game"));
			AddElement(-487833582, new CfgAutoGenerateLanguageTab(-487833582, "设置", "設定", "Settings"));
			AddElement(1015751757, new CfgAutoGenerateLanguageTab(1015751757, "音量", "音量", "Volume"));
			AddElement(-50860896, new CfgAutoGenerateLanguageTab(-50860896, "开关", "開關", "Toggle"));
			AddElement(337257549, new CfgAutoGenerateLanguageTab(337257549, "音乐", "音樂", "Music"));
			AddElement(-384687027, new CfgAutoGenerateLanguageTab(-384687027, "音效", "音效", "Sound Effect"));
			AddElement(107007938, new CfgAutoGenerateLanguageTab(107007938, "确定", "確定", "OK"));
		}
	}
}
