//--------------------------------------------------
// 自动生成  请勿修改
// 数据来源：表格/PieceRule.xlsx
//--------------------------------------------------
using System.Collections.Generic;

namespace Hotfix
{
	public class CfgPieceRuleTab : ConfigTab
	{
		public int PieceID { protected set; get; }
		public int Eat { protected set; get; }
		public int TargetSpecies { protected set; get; }
		public int MinCount { protected set; get; }
		public float Priority { protected set; get; }
		public string Desc { protected set; get; }

		public CfgPieceRuleTab(int id, int pieceID, int eat, int targetSpecies, int minCount, float priority, string desc)
		{
			Id = id;
			PieceID = pieceID;
			Eat = eat;
			TargetSpecies = targetSpecies;
			MinCount = minCount;
			Priority = priority;
			Desc = desc;
		}
	}

	public partial class CfgPieceRule : AssetConfig
	{
		private static CfgPieceRule _instance;
		public static CfgPieceRule Instance
		{
			get
			{
				if (_instance == null) { _instance = new CfgPieceRule(); _instance.Create(); }
				return _instance;
			}
		}

		private CfgPieceRule() { }
		public CfgPieceRuleTab GetCfgTab(int key)
		{
			return GetTab(key) as CfgPieceRuleTab;
		}
		public void Create()
		{
			AddElement(1, new CfgPieceRuleTab(1, 2, 1, 6, 0, 0f, "Bull eats Haystake"));
			AddElement(2, new CfgPieceRuleTab(2, 5, 1, 3, 0, 0f, "Fox attacks Cat"));
			AddElement(3, new CfgPieceRuleTab(3, 5, 0, 7, -1, 0f, "Fox does not eat Sheep if Fox is outnumbered by Sheep"));
			AddElement(4, new CfgPieceRuleTab(4, 5, 1, 7, 0, 0f, "Fox attacks Sheep"));
			AddElement(5, new CfgPieceRuleTab(5, 6, 1, 3, 0, 0f, "Guard Dog attacks Cat"));
			AddElement(6, new CfgPieceRuleTab(6, 8, 1, 6, 0, 0f, "Sheep eats Haystack"));
			AddElement(7, new CfgPieceRuleTab(7, 9, 1, 6, 0, 0f, "Skunk eats Haystack"));
			AddElement(8, new CfgPieceRuleTab(8, 9, 1, 9, 0, 0f, "Skunk attacks Snake"));
			AddElement(9, new CfgPieceRuleTab(9, 10, 1, 2, 0, 0f, "Snake attacks Bull"));
			AddElement(10, new CfgPieceRuleTab(10, 10, 1, 4, 0, 0f, "Snake attacks Fox"));
			AddElement(11, new CfgPieceRuleTab(11, 10, 1, 7, 0, 0f, "Snake attacks Sheep"));
			AddElement(12, new CfgPieceRuleTab(12, 10, 1, 10, 0, 0f, "Snake attacks Tiger"));
			AddElement(13, new CfgPieceRuleTab(13, 10, 1, 3, 0, 0f, "Snake attacks Cat"));
			AddElement(14, new CfgPieceRuleTab(14, 11, 1, 7, 0, 0f, "Tiger eats sheep"));
			AddElement(15, new CfgPieceRuleTab(15, 11, 1, 2, 1, 0f, "Tiger eat bull if there is more tigers than bulls"));
			AddElement(16, new CfgPieceRuleTab(16, 12, 1, 3, 0, 0f, "Fox attacks Cat"));
			AddElement(17, new CfgPieceRuleTab(17, 12, 0, 7, -1, 0f, "Fox does not eat sheep if there is more sheeps than foxes"));
			AddElement(18, new CfgPieceRuleTab(18, 12, 1, 7, 0, 0f, "Fox eat sheep"));
			AddElement(19, new CfgPieceRuleTab(19, 13, 1, 6, 0, 0f, "Sheep eats Haystake"));
			AddElement(20, new CfgPieceRuleTab(20, 14, 1, 6, 0, 0f, "Skunk eats Haystack"));
			AddElement(21, new CfgPieceRuleTab(21, 14, 1, 9, 0, 0f, "Skunk attacks Snake"));
			AddElement(22, new CfgPieceRuleTab(22, 15, 1, 2, 0, 0f, "Snake attacks Bull"));
			AddElement(23, new CfgPieceRuleTab(23, 15, 1, 4, 0, 0f, "Snake attacks Fox"));
			AddElement(24, new CfgPieceRuleTab(24, 15, 1, 7, 0, 0f, "Snake attacks Sheep"));
			AddElement(25, new CfgPieceRuleTab(25, 15, 1, 10, 0, 0f, "Snake attacks Tiger"));
			AddElement(26, new CfgPieceRuleTab(26, 15, 1, 3, 0, 0f, "Snake attacks Cat"));
		}
	}
}
