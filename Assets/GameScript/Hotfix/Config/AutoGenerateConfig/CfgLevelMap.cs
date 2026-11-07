//--------------------------------------------------
// 自动生成  请勿修改
// 数据来源：表格/LevelMap.xlsx
//--------------------------------------------------
using System.Collections.Generic;

namespace Hotfix
{
	public class CfgLevelMapTab : ConfigTab
	{
		public int LevelID { protected set; get; }
		public int Mode { protected set; get; }
		public float X { protected set; get; }
		public float Y { protected set; get; }
		public int Order { protected set; get; }

		public CfgLevelMapTab(int id, int levelID, int mode, float x, float y, int order)
		{
			Id = id;
			LevelID = levelID;
			Mode = mode;
			X = x;
			Y = y;
			Order = order;
		}
	}

	public partial class CfgLevelMap : AssetConfig
	{
		private static CfgLevelMap _instance;
		public static CfgLevelMap Instance
		{
			get
			{
				if (_instance == null) { _instance = new CfgLevelMap(); _instance.Create(); }
				return _instance;
			}
		}

		private CfgLevelMap() { }
		public CfgLevelMapTab GetCfgTab(int key)
		{
			return GetTab(key) as CfgLevelMapTab;
		}
		public void Create()
		{
			AddElement(1, new CfgLevelMapTab(1, 1, 0, 37f, 177f, 0));
			AddElement(2, new CfgLevelMapTab(2, 2, 0, 112f, 205f, 1));
			AddElement(3, new CfgLevelMapTab(3, 13, 0, 148f, 248f, 2));
			AddElement(4, new CfgLevelMapTab(4, 24, 0, 180f, 201f, 3));
			AddElement(5, new CfgLevelMapTab(5, 35, 0, 250f, 255f, 4));
			AddElement(6, new CfgLevelMapTab(6, 46, 0, 286f, 190f, 5));
			AddElement(7, new CfgLevelMapTab(7, 57, 0, 331f, 253f, 6));
			AddElement(8, new CfgLevelMapTab(8, 59, 0, 433f, 248f, 7));
			AddElement(9, new CfgLevelMapTab(9, 60, 0, 474f, 293f, 8));
			AddElement(10, new CfgLevelMapTab(10, 61, 0, 510f, 250f, 9));
			AddElement(11, new CfgLevelMapTab(11, 3, 0, 607f, 227f, 10));
			AddElement(12, new CfgLevelMapTab(12, 4, 0, 653f, 262f, 11));
			AddElement(13, new CfgLevelMapTab(13, 5, 0, 658f, 190f, 12));
			AddElement(14, new CfgLevelMapTab(14, 6, 0, 742f, 219f, 13));
			AddElement(15, new CfgLevelMapTab(15, 7, 0, 760f, 168f, 14));
			AddElement(16, new CfgLevelMapTab(16, 8, 0, 797f, 212f, 15));
			AddElement(17, new CfgLevelMapTab(17, 9, 0, 882f, 159f, 16));
			AddElement(18, new CfgLevelMapTab(18, 10, 0, 917f, 207f, 17));
			AddElement(19, new CfgLevelMapTab(19, 11, 0, 958f, 164f, 18));
			AddElement(20, new CfgLevelMapTab(20, 12, 0, 973f, 263f, 19));
			AddElement(21, new CfgLevelMapTab(21, 14, 0, 984f, 317f, 20));
			AddElement(22, new CfgLevelMapTab(22, 15, 0, 959f, 368f, 21));
			AddElement(23, new CfgLevelMapTab(23, 16, 0, 808f, 383f, 22));
			AddElement(24, new CfgLevelMapTab(24, 17, 0, 842f, 437f, 23));
			AddElement(25, new CfgLevelMapTab(25, 18, 0, 875f, 383f, 24));
			AddElement(26, new CfgLevelMapTab(26, 19, 0, 612f, 332f, 25));
			AddElement(27, new CfgLevelMapTab(27, 20, 0, 652f, 366f, 26));
			AddElement(28, new CfgLevelMapTab(28, 21, 0, 701f, 391f, 27));
			AddElement(29, new CfgLevelMapTab(29, 22, 0, 503f, 387f, 28));
			AddElement(30, new CfgLevelMapTab(30, 23, 0, 531f, 341f, 29));
			AddElement(31, new CfgLevelMapTab(31, 25, 0, 565f, 403f, 30));
			AddElement(32, new CfgLevelMapTab(32, 26, 0, 385f, 391f, 31));
			AddElement(33, new CfgLevelMapTab(33, 27, 0, 430f, 424f, 32));
			AddElement(34, new CfgLevelMapTab(34, 28, 0, 438f, 350f, 33));
			AddElement(35, new CfgLevelMapTab(35, 29, 0, 261f, 446f, 34));
			AddElement(36, new CfgLevelMapTab(36, 30, 0, 295f, 392f, 35));
			AddElement(37, new CfgLevelMapTab(37, 31, 0, 328f, 440f, 36));
			AddElement(38, new CfgLevelMapTab(38, 32, 0, 104f, 395f, 37));
			AddElement(39, new CfgLevelMapTab(39, 33, 0, 152f, 444f, 38));
			AddElement(40, new CfgLevelMapTab(40, 34, 0, 182f, 395f, 39));
			AddElement(41, new CfgLevelMapTab(41, 36, 0, 56f, 447f, 40));
			AddElement(42, new CfgLevelMapTab(42, 37, 0, 53f, 499f, 41));
			AddElement(43, new CfgLevelMapTab(43, 38, 0, 94f, 528f, 42));
			AddElement(44, new CfgLevelMapTab(44, 39, 0, 169f, 511f, 43));
			AddElement(45, new CfgLevelMapTab(45, 40, 0, 201f, 568f, 44));
			AddElement(46, new CfgLevelMapTab(46, 41, 0, 230f, 509f, 45));
			AddElement(47, new CfgLevelMapTab(47, 42, 0, 301f, 515f, 46));
			AddElement(48, new CfgLevelMapTab(48, 43, 0, 335f, 568f, 47));
			AddElement(49, new CfgLevelMapTab(49, 44, 0, 362f, 515f, 48));
			AddElement(50, new CfgLevelMapTab(50, 45, 0, 433f, 542f, 49));
			AddElement(51, new CfgLevelMapTab(51, 47, 0, 481f, 575f, 50));
			AddElement(52, new CfgLevelMapTab(52, 48, 0, 527f, 616f, 51));
			AddElement(53, new CfgLevelMapTab(53, 49, 0, 611f, 611f, 52));
			AddElement(54, new CfgLevelMapTab(54, 50, 0, 650f, 652f, 53));
			AddElement(55, new CfgLevelMapTab(55, 51, 0, 697f, 621f, 54));
			AddElement(56, new CfgLevelMapTab(56, 52, 0, 761f, 671f, 55));
			AddElement(57, new CfgLevelMapTab(57, 53, 0, 785f, 619f, 56));
			AddElement(58, new CfgLevelMapTab(58, 54, 0, 823f, 675f, 57));
			AddElement(59, new CfgLevelMapTab(59, 55, 0, 888f, 581f, 58));
			AddElement(60, new CfgLevelMapTab(60, 56, 0, 921f, 630f, 59));
			AddElement(61, new CfgLevelMapTab(61, 58, 0, 955f, 577f, 60));
			AddElement(62, new CfgLevelMapTab(62, 101, 1, 49f, 135f, 0));
			AddElement(63, new CfgLevelMapTab(63, 102, 1, 153f, 171f, 1));
			AddElement(64, new CfgLevelMapTab(64, 113, 1, 265f, 162f, 2));
			AddElement(65, new CfgLevelMapTab(65, 122, 1, 363f, 117f, 3));
			AddElement(66, new CfgLevelMapTab(66, 123, 1, 480f, 77f, 4));
			AddElement(67, new CfgLevelMapTab(67, 124, 1, 586f, 85f, 5));
			AddElement(68, new CfgLevelMapTab(68, 125, 1, 688f, 110f, 6));
			AddElement(69, new CfgLevelMapTab(69, 126, 1, 790f, 124f, 7));
			AddElement(70, new CfgLevelMapTab(70, 127, 1, 950f, 127f, 8));
			AddElement(71, new CfgLevelMapTab(71, 128, 1, 866f, 178f, 9));
			AddElement(72, new CfgLevelMapTab(72, 103, 1, 718f, 190f, 10));
			AddElement(73, new CfgLevelMapTab(73, 104, 1, 638f, 234f, 11));
			AddElement(74, new CfgLevelMapTab(74, 105, 1, 524f, 239f, 12));
			AddElement(75, new CfgLevelMapTab(75, 106, 1, 429f, 295f, 13));
			AddElement(76, new CfgLevelMapTab(76, 107, 1, 506f, 388f, 14));
			AddElement(77, new CfgLevelMapTab(77, 108, 1, 633f, 373f, 15));
			AddElement(78, new CfgLevelMapTab(78, 109, 1, 748f, 355f, 16));
			AddElement(79, new CfgLevelMapTab(79, 110, 1, 853f, 314f, 17));
			AddElement(80, new CfgLevelMapTab(80, 111, 1, 947f, 351f, 18));
			AddElement(81, new CfgLevelMapTab(81, 112, 1, 967f, 457f, 19));
			AddElement(82, new CfgLevelMapTab(82, 114, 1, 951f, 563f, 20));
			AddElement(83, new CfgLevelMapTab(83, 115, 1, 854f, 628f, 21));
			AddElement(84, new CfgLevelMapTab(84, 116, 1, 728f, 646f, 22));
			AddElement(85, new CfgLevelMapTab(85, 117, 1, 585f, 613f, 23));
			AddElement(86, new CfgLevelMapTab(86, 118, 1, 461f, 557f, 24));
			AddElement(87, new CfgLevelMapTab(87, 119, 1, 348f, 514f, 25));
			AddElement(88, new CfgLevelMapTab(88, 120, 1, 218f, 500f, 26));
			AddElement(89, new CfgLevelMapTab(89, 121, 1, 87f, 525f, 27));
		}
	}
}
