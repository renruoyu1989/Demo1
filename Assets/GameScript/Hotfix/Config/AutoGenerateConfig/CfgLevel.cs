//--------------------------------------------------
// 自动生成  请勿修改
// 数据来源：表格/Level.xlsx
//--------------------------------------------------
using System.Collections.Generic;

namespace Hotfix
{
	public class CfgLevelTab : ConfigTab
	{
		public string Code { protected set; get; }
		public int Kind { protected set; get; }
		public string Background { protected set; get; }
		public int Boat { protected set; get; }
		public int BoatSpace { protected set; get; }
		public int WaterType { protected set; get; }
		public int CanvasWidth { protected set; get; }
		public int SpawnCount { protected set; get; }
		public int RespawnTime { protected set; get; }
		public int DismissCount { protected set; get; }
		public List<int> Waiting { protected set; get; }

		public CfgLevelTab(int id, string code, int kind, string background, int boat, int boatSpace, int waterType, int canvasWidth, int spawnCount, int respawnTime, int dismissCount, List<int> waiting)
		{
			Id = id;
			Code = code;
			Kind = kind;
			Background = background;
			Boat = boat;
			BoatSpace = boatSpace;
			WaterType = waterType;
			CanvasWidth = canvasWidth;
			SpawnCount = spawnCount;
			RespawnTime = respawnTime;
			DismissCount = dismissCount;
			Waiting = waiting;
		}
	}

	public partial class CfgLevel : AssetConfig
	{
		private static CfgLevel _instance;
		public static CfgLevel Instance
		{
			get
			{
				if (_instance == null) { _instance = new CfgLevel(); _instance.Create(); }
				return _instance;
			}
		}

		private CfgLevel() { }
		public CfgLevelTab GetCfgTab(int key)
		{
			return GetTab(key) as CfgLevelTab;
		}
		public void Create()
		{
			AddElement(900, new CfgLevelTab(900, "Sheep, Fox and Hay", 2, "doc2_bot1_st1_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(1, new CfgLevelTab(1, "sp_0", 0, "doc2_bot1_st1_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(2, new CfgLevelTab(2, "sp_1", 0, "doc2_bot1_st1_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(3, new CfgLevelTab(3, "sp_10", 0, "doc2_bot2_st2_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(4, new CfgLevelTab(4, "sp_11", 0, "doc3_bot1_st2_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(5, new CfgLevelTab(5, "sp_12", 0, "doc3_bot2_st2_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(6, new CfgLevelTab(6, "sp_13", 0, "doc2_bot1_st2_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(7, new CfgLevelTab(7, "sp_14", 0, "doc3_bot1_st2_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(8, new CfgLevelTab(8, "sp_15", 0, "doc3_bot1_st2_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(9, new CfgLevelTab(9, "sp_16", 0, "doc3_bot2_st3_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(10, new CfgLevelTab(10, "sp_17", 0, "doc2_bot2_st3_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(11, new CfgLevelTab(11, "sp_18", 0, "doc3_bot2_st3_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(12, new CfgLevelTab(12, "sp_19", 0, "doc2_bot2_st3_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(13, new CfgLevelTab(13, "sp_2", 0, "doc3_bot1_st1_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(14, new CfgLevelTab(14, "sp_20", 0, "doc3_bot2_st3_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(15, new CfgLevelTab(15, "sp_21", 0, "doc3_bot2_st3_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(16, new CfgLevelTab(16, "sp_22", 0, "doc3_bot2_st4_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(17, new CfgLevelTab(17, "sp_23", 0, "doc2_bot1_st4_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(18, new CfgLevelTab(18, "sp_24", 0, "doc3_bot2_st4_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(19, new CfgLevelTab(19, "sp_25", 0, "doc3_bot2_st4_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(20, new CfgLevelTab(20, "sp_26", 0, "doc3_bot1_st4_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(21, new CfgLevelTab(21, "sp_27", 0, "doc3_bot1_st4_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(22, new CfgLevelTab(22, "sp_28", 0, "doc3_bot2_st4_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(23, new CfgLevelTab(23, "sp_29", 0, "doc2_bot1_st4_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(24, new CfgLevelTab(24, "sp_3", 0, "doc2_bot2_st1_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(25, new CfgLevelTab(25, "sp_30", 0, "doc3_bot2_st4_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(26, new CfgLevelTab(26, "sp_31", 0, "doc2_bot1_st4_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(27, new CfgLevelTab(27, "sp_32", 0, "doc2_bot1_st4_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(28, new CfgLevelTab(28, "sp_33", 0, "doc3_bot2_st4_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(29, new CfgLevelTab(29, "sp_34", 0, "doc3_bot2_st5_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(30, new CfgLevelTab(30, "sp_35", 0, "doc2_bot1_st5_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(31, new CfgLevelTab(31, "sp_36", 0, "doc3_bot2_st5_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(32, new CfgLevelTab(32, "sp_37", 0, "doc3_bot1_st5_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(33, new CfgLevelTab(33, "sp_38", 0, "doc2_bot2_st5_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(34, new CfgLevelTab(34, "sp_39", 0, "doc3_bot2_st5_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(35, new CfgLevelTab(35, "sp_4", 0, "doc2_bot2_st1_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(36, new CfgLevelTab(36, "sp_40", 0, "doc3_bot2_st5_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(37, new CfgLevelTab(37, "sp_41", 0, "doc3_bot2_st5_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(38, new CfgLevelTab(38, "sp_42", 0, "doc3_bot2_st5_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(39, new CfgLevelTab(39, "sp_43", 0, "doc3_bot1_st6_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(40, new CfgLevelTab(40, "sp_44", 0, "doc3_bot2_st6_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(41, new CfgLevelTab(41, "sp_45", 0, "doc3_bot2_st6_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(42, new CfgLevelTab(42, "sp_46", 0, "doc3_bot2_st6_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(43, new CfgLevelTab(43, "sp_47", 0, "doc3_bot2_st6_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(44, new CfgLevelTab(44, "sp_48", 0, "doc3_bot2_st6_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(45, new CfgLevelTab(45, "sp_49", 0, "doc3_bot2_st7_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(46, new CfgLevelTab(46, "sp_5", 0, "doc3_bot2_st1_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(47, new CfgLevelTab(47, "sp_50", 0, "doc3_bot2_st7_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(48, new CfgLevelTab(48, "sp_51", 0, "doc3_bot1_st7_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(49, new CfgLevelTab(49, "sp_52", 0, "doc3_bot2_st7_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(50, new CfgLevelTab(50, "sp_53", 0, "doc3_bot1_st7_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(51, new CfgLevelTab(51, "sp_54", 0, "doc3_bot1_st7_ty2", 1, 1, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(52, new CfgLevelTab(52, "sp_55", 0, "doc2_bot2_st8_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(53, new CfgLevelTab(53, "sp_56", 0, "doc3_bot2_st8_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(54, new CfgLevelTab(54, "sp_57", 0, "doc3_bot1_st8_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(55, new CfgLevelTab(55, "sp_58", 0, "doc3_bot2_st8_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(56, new CfgLevelTab(56, "sp_59", 0, "doc4_bot2_st8_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(57, new CfgLevelTab(57, "sp_6", 0, "doc2_bot2_st1_ty2", 2, 2, 2, 1000, 0, 0, 0, new List<int>()));
			AddElement(58, new CfgLevelTab(58, "sp_60", 0, "doc4_bot3_st8_ty1", 3, 3, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(59, new CfgLevelTab(59, "sp_7", 0, "doc2_bot1_st2_ty1", 1, 1, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(60, new CfgLevelTab(60, "sp_8", 0, "doc2_bot2_st2_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(61, new CfgLevelTab(61, "sp_9", 0, "doc3_bot2_st2_ty1", 2, 2, 1, 1000, 0, 0, 0, new List<int>()));
			AddElement(101, new CfgLevelTab(101, "tm_0", 1, "doc2_bot1_st6_ty2", 1, 1, 2, 1000, 1, 2, 3, new List<int>() { 8, 5, 10, 5, 8 }));
			AddElement(102, new CfgLevelTab(102, "tm_1", 1, "doc2_bot1_st6_ty2", 1, 1, 2, 1000, 1, 2, 3, new List<int>() { 10, 8, 9, 5, 8, 10 }));
			AddElement(103, new CfgLevelTab(103, "tm_10", 1, "doc2_bot1_st3_ty1", 1, 1, 1, 1000, 2, 4, 3, new List<int>() { 7, 7, 3, 3, 8, 3, 10, 6 }));
			AddElement(104, new CfgLevelTab(104, "tm_11", 1, "doc2_bot2_st3_ty2", 2, 2, 2, 1000, 4, 4, 5, new List<int>() { 7, 8, 5, 8, 8, 5, 10, 11 }));
			AddElement(105, new CfgLevelTab(105, "tm_12", 1, "doc3_bot1_st3_ty1", 1, 1, 1, 1000, 2, 6, 3, new List<int>() { 5, 7, 5, 3, 5, 5 }));
			AddElement(106, new CfgLevelTab(106, "tm_13", 1, "doc3_bot1_st1_ty2", 1, 1, 2, 1000, 2, 6, 3, new List<int>() { 8, 9, 11, 11, 7, 9 }));
			AddElement(107, new CfgLevelTab(107, "tm_14", 1, "doc3_bot1_st1_ty2", 1, 1, 2, 1000, 2, 6, 3, new List<int>() { 10, 5, 5, 7, 5, 10 }));
			AddElement(108, new CfgLevelTab(108, "tm_15", 1, "doc3_bot1_st1_ty2", 1, 1, 2, 1000, 2, 6, 3, new List<int>() { 5, 5, 3, 8, 5, 5 }));
			AddElement(109, new CfgLevelTab(109, "tm_16", 1, "doc3_bot1_st2_ty1", 1, 1, 1, 1000, 2, 6, 3, new List<int>() { 7, 7, 8, 8, 15, 15 }));
			AddElement(110, new CfgLevelTab(110, "tm_17", 1, "doc3_bot1_st2_ty2", 1, 1, 2, 1000, 2, 6, 3, new List<int>() { 10, 3, 10, 5 }));
			AddElement(111, new CfgLevelTab(111, "tm_18", 1, "doc3_bot1_st2_ty2", 1, 1, 2, 1000, 2, 6, 3, new List<int>() { 7, 8, 8, 2, 11, 11 }));
			AddElement(112, new CfgLevelTab(112, "tm_19", 1, "doc3_bot2_st7_ty1", 2, 2, 1, 1000, 3, 4, 5, new List<int>() { 10, 8, 11, 8, 8, 7, 11, 11 }));
			AddElement(113, new CfgLevelTab(113, "tm_2", 1, "doc2_bot1_st6_ty1", 1, 1, 1, 1000, 1, 3, 3, new List<int>() { 10, 11, 7, 8, 7, 11 }));
			AddElement(114, new CfgLevelTab(114, "tm_20", 1, "doc3_bot1_st7_ty2", 1, 1, 2, 1000, 2, 6, 3, new List<int>() { 1, 7, 10, 10, 3, 3 }));
			AddElement(115, new CfgLevelTab(115, "tm_21", 1, "doc3_bot1_st7_ty2", 1, 1, 2, 1000, 2, 6, 3, new List<int>() { 10, 3, 5, 10, 3, 6 }));
			AddElement(116, new CfgLevelTab(116, "tm_22", 1, "doc3_bot1_st8_ty1", 1, 1, 1, 1000, 2, 6, 3, new List<int>() { 8, 10, 10, 8, 1, 1 }));
			AddElement(117, new CfgLevelTab(117, "tm_23", 1, "doc3_bot1_st8_ty1", 1, 1, 1, 1000, 2, 6, 3, new List<int>() { 5, 5, 8, 8, 5, 7 }));
			AddElement(118, new CfgLevelTab(118, "tm_24", 1, "doc3_bot1_st8_ty1", 1, 1, 1, 1000, 2, 6, 3, new List<int>() { 11, 10, 2, 11, 10, 10 }));
			AddElement(119, new CfgLevelTab(119, "tm_25", 1, "doc4_bot1_st1_ty1", 1, 1, 1, 1000, 2, 8, 3, new List<int>() { 5, 10, 5, 10 }));
			AddElement(120, new CfgLevelTab(120, "tm_26", 1, "doc4_bot1_st1_ty1", 1, 1, 1, 1000, 2, 8, 3, new List<int>() { 3, 1, 5, 5 }));
			AddElement(121, new CfgLevelTab(121, "tm_27", 1, "doc2_bot1_st6_ty1", 1, 1, 1, 1000, 1, 3, 3, new List<int>() { 10, 11, 7, 8, 7, 11 }));
			AddElement(122, new CfgLevelTab(122, "tm_3", 1, "doc2_bot1_st6_ty1", 1, 1, 1, 1000, 1, 3, 3, new List<int>() { 10, 8, 5, 8, 5, 10, 8 }));
			AddElement(123, new CfgLevelTab(123, "tm_4", 1, "doc2_bot1_st4_ty2", 1, 1, 2, 1000, 2, 5, 3, new List<int>() { 10, 7, 5, 7, 5, 5, 8, 7 }));
			AddElement(124, new CfgLevelTab(124, "tm_5", 1, "doc2_bot1_st4_ty1", 1, 1, 1, 1000, 2, 5, 3, new List<int>() { 11, 7, 8, 7, 11, 7, 8, 8 }));
			AddElement(125, new CfgLevelTab(125, "tm_6", 1, "doc2_bot1_st4_ty1", 1, 1, 1, 1000, 2, 5, 3, new List<int>() { 9, 10, 5, 7, 10, 10, 8, 7 }));
			AddElement(126, new CfgLevelTab(126, "tm_7", 1, "doc2_bot1_st5_ty2", 1, 1, 2, 1000, 2, 5, 3, new List<int>() { 2, 11, 11, 7, 10, 10, 11, 2, 7, 10 }));
			AddElement(127, new CfgLevelTab(127, "tm_8", 1, "doc2_bot2_st5_ty1", 2, 2, 1, 1000, 4, 5, 4, new List<int>() { 7, 11, 10, 8, 7, 7, 10, 8 }));
			AddElement(128, new CfgLevelTab(128, "tm_9", 1, "doc2_bot2_st5_ty1", 2, 2, 1, 1000, 4, 4, 4, new List<int>() { 10, 5, 10, 8, 8, 5, 10, 8 }));
		}
	}
}
