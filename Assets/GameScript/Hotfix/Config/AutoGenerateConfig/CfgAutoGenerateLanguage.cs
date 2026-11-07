//--------------------------------------------------
// 自动生成  请勿修改
// 数据来源：表格/AutoGenerateLanguage.xlsx
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
			AddElement(-1891152370, new CfgAutoGenerateLanguageTab(-1891152370, "蓝奶酪", "", "Blue Cheese"));
			AddElement(-1891152369, new CfgAutoGenerateLanguageTab(-1891152369, "公牛", "", "Bull"));
			AddElement(-1891152368, new CfgAutoGenerateLanguageTab(-1891152368, "猫", "", "Cat"));
			AddElement(-1891152367, new CfgAutoGenerateLanguageTab(-1891152367, "奶酪", "", "Cheese"));
			AddElement(-1891152366, new CfgAutoGenerateLanguageTab(-1891152366, "狐狸", "", "Fox"));
			AddElement(-1891152365, new CfgAutoGenerateLanguageTab(-1891152365, "牧羊犬", "", "Guard Dog"));
			AddElement(-1891152364, new CfgAutoGenerateLanguageTab(-1891152364, "干草堆", "", "Hay"));
			AddElement(-1891152363, new CfgAutoGenerateLanguageTab(-1891152363, "羊", "", "Sheep"));
			AddElement(-1891152362, new CfgAutoGenerateLanguageTab(-1891152362, "臭鼬", "", "Skunk"));
			AddElement(1503818722, new CfgAutoGenerateLanguageTab(1503818722, "蛇", "", "Snake"));
			AddElement(1503818723, new CfgAutoGenerateLanguageTab(1503818723, "老虎", "", "Tiger"));
			AddElement(1503818724, new CfgAutoGenerateLanguageTab(1503818724, "无用狐狸", "", "Worthless Fox"));
			AddElement(1503818725, new CfgAutoGenerateLanguageTab(1503818725, "无用的羊", "", "Worthless Sheep"));
			AddElement(1503818726, new CfgAutoGenerateLanguageTab(1503818726, "无用臭鼬", "", "Worthless Skunk"));
			AddElement(1503818727, new CfgAutoGenerateLanguageTab(1503818727, "无用的蛇", "", "Worthless Snake"));
			AddElement(1244319764, new CfgAutoGenerateLanguageTab(1244319764, "这里有狐狸、羊和干草堆，你需要把它们摆渡到对岸。", "", "There is a fox, a sheep and a haystack you have to ferry across."));
			AddElement(1244319765, new CfgAutoGenerateLanguageTab(1244319765, "你的任务是把它们渡过河，送到这边的另一个码头。", "", "Your job is to carry them across the water to the other dock over here.\n\n(click to continue)"));
			AddElement(1244319766, new CfgAutoGenerateLanguageTab(1244319766, "点击动物或物件，把它们装上渡船。", "", "Click on an animal or object to load them onto your ferry."));
			AddElement(1244319767, new CfgAutoGenerateLanguageTab(1244319767, "到达对岸后再次点击它，把它卸下船。", "", "Click on it again to unload it when you get to the other dock."));
			AddElement(1244319768, new CfgAutoGenerateLanguageTab(1244319768, "点击这个按钮，把船划过河。", "", "Click on this button to row the boat across water."));
			AddElement(1244319769, new CfgAutoGenerateLanguageTab(1244319769, "在「谜题规则」框里可以查看哪种动物会吃哪种动物。", "", "You can find out which animal eats which in the Puzzle Rule box."));
			AddElement(1244319770, new CfgAutoGenerateLanguageTab(1244319770, "现在试着把狐狸、羊和干草堆渡到对岸码头。", "", "Now, try to ferry the fox, sheep and haystack across to the other dock."));
			AddElement(1244319771, new CfgAutoGenerateLanguageTab(1244319771, "可以用奶酪阻止动物互相吞食：把奶酪放下，动物会先去吃奶酪，再去吃别的动物。", "", "You can use cheese to prevent animals from eating each other. Place the cheese down and the animals will try to eat the cheese first before they try to eat the other animals."));
			AddElement(1244319772, new CfgAutoGenerateLanguageTab(1244319772, "把光标放在屏幕边缘，可以左右滚动画面。", "", "In this level, you can scroll left and right by placing your cursor on edge of the screen."));
			AddElement(-80792932, new CfgAutoGenerateLanguageTab(-80792932, "这一关有三个码头。可以用中间码头临时把动物分开，最终要把所有动物送到最远的码头。", "", "You have three docks for this level. You can use the middle dock to temporarily separate animals. You'll need to ferry all of the animals to the far dock."));
			AddElement(-80792931, new CfgAutoGenerateLanguageTab(-80792931, "牧羊犬在场时，其它动物不敢在这座码头上互相吞食。但要小心，牧羊犬讨厌猫！", "", "A guard dog prevents any animal from eating another on that dock. But be careful, the guard dog hates cats!"));
			AddElement(-80792930, new CfgAutoGenerateLanguageTab(-80792930, "这一关的船可以同时载两只动物。每关开始时都会提示船的载客量。", "", "In this puzzle, you can ferry two animals at the same time. At the start of each level, you will be told how many animals you can ferry in your boat."));
			AddElement(-80792929, new CfgAutoGenerateLanguageTab(-80792929, "蓝奶酪会吸引所有爱吃奶酪的动物，但与普通奶酪不同，蓝奶酪被吃掉你就输了。", "", "Blue cheese will attract all animals who like a cheese, but unlike the regular cheese, you will lose if the blue cheese is eaten."));
			AddElement(-80792928, new CfgAutoGenerateLanguageTab(-80792928, "绿色动物不需要运送。它们被吃掉不会判负，但它们会吃掉你需要的动物。", "", "You don't need to ferry green animals. You won't lose if one of them is eaten, but they will eat the animals you do need."));
			AddElement(-80792927, new CfgAutoGenerateLanguageTab(-80792927, "在连击模式中，你需要运送多只动物来累积连击。", "", "In Combo Mode, you will need to ferry multiple animals to score multiple combos."));
			AddElement(-80792926, new CfgAutoGenerateLanguageTab(-80792926, "摆渡人每次过河，「剩余渡河次数」就会减少。", "", "When your ferryman crosses the river, the Ferry Count will decrease."));
			AddElement(-80792925, new CfgAutoGenerateLanguageTab(-80792925, "次数归零时，新的动物会出现在这边的第一个码头。", "", "When it reaches zero, new animals will arrive at the first dock here."));
			AddElement(-80792924, new CfgAutoGenerateLanguageTab(-80792924, "即将到来的动物会显示在屏幕顶部。", "", "The animals about to arrive are shown at the top of the screen."));
			AddElement(-80792923, new CfgAutoGenerateLanguageTab(-80792923, "当运送的动物达到 3 只（视关卡而定）后，可以点击这里把它们放生结算。", "", "When you've ferried 3 or more animals (depending on level), you can dismiss them by clicking here."));
			AddElement(-80792901, new CfgAutoGenerateLanguageTab(-80792901, "需要放生同种动物来叠加连击，从而获得更高分数。开始吧！", "", "You will need to dismiss same animals to pile multiple combos to get more scores. Now let's get started!"));
		}
	}
}
