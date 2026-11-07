//--------------------------------------------------
// 自动生成  请勿修改
// 数据来源：表格/Piece.xlsx
//--------------------------------------------------
using System.Collections.Generic;

namespace Hotfix
{
	public class CfgPieceTab : ConfigTab
	{
		public string Code { protected set; get; }
		public int NameID { protected set; get; }
		public string Desc { protected set; get; }
		public string Sheet { protected set; get; }
		public int Species { protected set; get; }
		public float Speed { protected set; get; }
		public int BoatSpace { protected set; get; }
		public int Essential { protected set; get; }
		public int Moveable { protected set; get; }
		public int CanEat { protected set; get; }
		public float OffsetX { protected set; get; }
		public float OffsetY { protected set; get; }
		public int ColorR { protected set; get; }
		public int ColorG { protected set; get; }
		public int ColorB { protected set; get; }
		public string Sound { protected set; get; }
		public string Name { get { return LANG.Convert(NameID); } }

		public CfgPieceTab(int id, string code, int nameID, string desc, string sheet, int species, float speed, int boatSpace, int essential, int moveable, int canEat, float offsetX, float offsetY, int colorR, int colorG, int colorB, string sound)
		{
			Id = id;
			Code = code;
			NameID = nameID;
			Desc = desc;
			Sheet = sheet;
			Species = species;
			Speed = speed;
			BoatSpace = boatSpace;
			Essential = essential;
			Moveable = moveable;
			CanEat = canEat;
			OffsetX = offsetX;
			OffsetY = offsetY;
			ColorR = colorR;
			ColorG = colorG;
			ColorB = colorB;
			Sound = sound;
		}
	}

	public partial class CfgPiece : AssetConfig
	{
		private static CfgPiece _instance;
		public static CfgPiece Instance
		{
			get
			{
				if (_instance == null) { _instance = new CfgPiece(); _instance.Create(); }
				return _instance;
			}
		}

		private CfgPiece() { }
		public CfgPieceTab GetCfgTab(int key)
		{
			return GetTab(key) as CfgPieceTab;
		}
		public void Create()
		{
			AddElement(1, new CfgPieceTab(1, "Blue Cheese", -1891152370, "If eaten, Jiang fails", "Art/cheese", 1, 2.5f, 1, 1, 1, 0, 3f, 3f, 28, 87, 255, ""));
			AddElement(2, new CfgPieceTab(2, "Bull", -1891152369, "Bull eats Hay", "Art/Bull", 2, 2f, 1, 1, 1, 1, 24f, 8f, 255, 255, 255, "Bull"));
			AddElement(3, new CfgPieceTab(3, "Cat", -1891152368, "Guard Dog attacks Cat", "Art/cat", 3, 2.3f, 1, 1, 1, 1, 3f, 5f, 255, 255, 255, "Cat"));
			AddElement(4, new CfgPieceTab(4, "Cheese", -1891152367, "Animals eat it, not each other", "Art/cheese", 1, 2.5f, 1, 0, 1, 0, 3f, 3f, 255, 255, 255, ""));
			AddElement(5, new CfgPieceTab(5, "Fox", -1891152366, "Fox eats Sheep and Cat", "Art/fox", 4, 2.8f, 1, 1, 1, 1, 10f, 7f, 255, 255, 255, "Fox"));
			AddElement(6, new CfgPieceTab(6, "Guard Dog", -1891152365, "Wont let animals attack", "Art/dog", 5, 2.8f, 1, 1, 1, 0, 18f, 8f, 255, 255, 255, ""));
			AddElement(7, new CfgPieceTab(7, "Hay Stack", -1891152364, "Sheep and Bull eat Hay", "Art/hay", 6, 2.5f, 1, 1, 1, 0, 0f, 10f, 255, 255, 255, ""));
			AddElement(8, new CfgPieceTab(8, "Sheep", -1891152363, "Sheep eats Hay", "Art/sheep", 7, 3f, 1, 1, 1, 1, 15f, 6f, 255, 255, 255, "Sheep"));
			AddElement(9, new CfgPieceTab(9, "Skunk", -1891152362, "No one wants to eat Skunk", "Art/Skunk", 8, 2.5f, 1, 1, 1, 1, -6f, 6f, 255, 255, 255, "Skunk"));
			AddElement(10, new CfgPieceTab(10, "Snake", 1503818722, "Snake attacks any animal", "Art/Snake", 9, 2.5f, 1, 1, 1, 1, 0f, 5f, 255, 255, 255, "Snake"));
			AddElement(11, new CfgPieceTab(11, "Tiger", 1503818723, "Tiger eats Sheep and Bull", "Art/tiger", 10, 3f, 1, 1, 1, 1, 11f, 7f, 255, 255, 255, "Tiger"));
			AddElement(12, new CfgPieceTab(12, "Worthless Fox", 1503818724, "This Fox is disposable", "Art/fox", 4, 2.8f, 1, 0, 1, 1, 10f, 7f, 162, 255, 147, ""));
			AddElement(13, new CfgPieceTab(13, "Worthless Sheep", 1503818725, "This Sheep is disposable", "Art/sheep", 7, 3f, 1, 0, 1, 1, 15f, 6f, 162, 255, 147, ""));
			AddElement(14, new CfgPieceTab(14, "Worthless Skunk", 1503818726, "This Skunk is disposable", "Art/Skunk", 8, 2.5f, 1, 0, 1, 1, -6f, 6f, 162, 255, 147, ""));
			AddElement(15, new CfgPieceTab(15, "Worthless Snake", 1503818727, "This Snake is disposable", "Art/Snake", 9, 2.5f, 1, 0, 1, 1, 0f, 5f, 162, 255, 147, ""));
		}
	}
}
