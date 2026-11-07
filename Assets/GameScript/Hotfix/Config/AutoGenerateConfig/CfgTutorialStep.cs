//--------------------------------------------------
// 自动生成  请勿修改
// 数据来源：表格/TutorialStep.xlsx
//--------------------------------------------------
using System.Collections.Generic;

namespace Hotfix
{
	public class CfgTutorialStepTab : ConfigTab
	{
		public int LevelID { protected set; get; }
		public int Step { protected set; get; }
		public int TextID { protected set; get; }
		public float Width { protected set; get; }
		public float X { protected set; get; }
		public float Y { protected set; get; }
		public float Ax1 { protected set; get; }
		public float Ay1 { protected set; get; }
		public float Ax2 { protected set; get; }
		public float Ay2 { protected set; get; }
		public int Center { protected set; get; }
		public string Text { get { return LANG.Convert(TextID); } }

		public CfgTutorialStepTab(int id, int levelID, int step, int textID, float width, float x, float y, float ax1, float ay1, float ax2, float ay2, int center)
		{
			Id = id;
			LevelID = levelID;
			Step = step;
			TextID = textID;
			Width = width;
			X = x;
			Y = y;
			Ax1 = ax1;
			Ay1 = ay1;
			Ax2 = ax2;
			Ay2 = ay2;
			Center = center;
		}
	}

	public partial class CfgTutorialStep : AssetConfig
	{
		private static CfgTutorialStep _instance;
		public static CfgTutorialStep Instance
		{
			get
			{
				if (_instance == null) { _instance = new CfgTutorialStep(); _instance.Create(); }
				return _instance;
			}
		}

		private CfgTutorialStep() { }
		public CfgTutorialStepTab GetCfgTab(int key)
		{
			return GetTab(key) as CfgTutorialStepTab;
		}
		public void Create()
		{
			AddElement(1, new CfgTutorialStepTab(1, 1, 0, 1244319764, 300f, 30f, 90f, 200f, 180f, 250f, 360f, 1));
			AddElement(2, new CfgTutorialStepTab(2, 1, 0, 1244319765, 350f, 645f, 180f, 820f, 300f, 820f, 420f, 1));
			AddElement(3, new CfgTutorialStepTab(3, 1, 1, 1244319766, 300f, 100f, 140f, 265f, 260f, 265f, 350f, 1));
			AddElement(4, new CfgTutorialStepTab(4, 1, 2, 1244319767, 300f, 100f, 140f, 265f, 260f, 265f, 350f, 1));
			AddElement(5, new CfgTutorialStepTab(5, 1, 3, 1244319768, 300f, 165f, 160f, 320f, 280f, 415f, 475f, 1));
			AddElement(6, new CfgTutorialStepTab(6, 1, 4, 1244319769, 300f, 120f, 295f, 275f, 410f, 95f, 615f, 1));
			AddElement(7, new CfgTutorialStepTab(7, 1, 5, 1244319770, 350f, 330f, 170f, 500f, 230f, 500f, 240f, 1));
			AddElement(8, new CfgTutorialStepTab(8, 12, 0, 1244319771, 350f, 480f, 160f, 655f, 305f, 720f, 390f, 1));
			AddElement(9, new CfgTutorialStepTab(9, 13, 0, 1244319772, 350f, 330f, 170f, 500f, 230f, 500f, 240f, 1));
			AddElement(10, new CfgTutorialStepTab(10, 13, 1, -80792932, 300f, 770f, 90f, 900f, 200f, 940f, 430f, 1));
			AddElement(11, new CfgTutorialStepTab(11, 23, 0, -80792931, 300f, 110f, 165f, 240f, 295f, 275f, 370f, 1));
			AddElement(12, new CfgTutorialStepTab(12, 24, 0, -80792930, 300f, 215f, 150f, 345f, 265f, 410f, 405f, 1));
			AddElement(13, new CfgTutorialStepTab(13, 32, 0, -80792929, 300f, 55f, 170f, 210f, 295f, 250f, 390f, 1));
			AddElement(14, new CfgTutorialStepTab(14, 40, 0, -80792928, 300f, 205f, 135f, 340f, 265f, 358f, 360f, 1));
			AddElement(15, new CfgTutorialStepTab(15, 101, 0, -80792927, 300f, 285f, 165f, 445f, 225f, 445f, 225f, 1));
			AddElement(16, new CfgTutorialStepTab(16, 101, 1, -80792926, 300f, 80f, 195f, 235f, 225f, 98f, 117f, 1));
			AddElement(17, new CfgTutorialStepTab(17, 101, 2, -80792925, 300f, 40f, 165f, 190f, 290f, 190f, 390f, 1));
			AddElement(18, new CfgTutorialStepTab(18, 101, 3, -80792924, 300f, 155f, 190f, 310f, 220f, 185f, 135f, 1));
			AddElement(19, new CfgTutorialStepTab(19, 101, 4, -80792923, 300f, 550f, 205f, 835f, 280f, 970f, 281f, 1));
			AddElement(20, new CfgTutorialStepTab(20, 101, 5, -80792901, 300f, 550f, 205f, 715f, 280f, 715f, 280f, 1));
		}
	}
}
