using System;
using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>动画帧矩形（大图切帧）</summary>
	[Serializable]
	public class FerryFrameData
	{
		public float x;
		public float y;
		public float w;
		public float h;
	}

	/// <summary>一段帧动画</summary>
	[Serializable]
	public class FerryAnimData
	{
		public string name;
		public int fps;
		public float offsetY;
		public List<FerryFrameData> frames = new List<FerryFrameData>();

		public int Fps { get { return fps < 1 ? 20 : fps; } }
		public int FrameCount { get { return frames == null ? 0 : frames.Count; } }
	}

	/// <summary>场景物件（码头、背景装饰、水面等）</summary>
	[Serializable]
	public class FerrySceneObjData
	{
		public string tpl;
		public string img;
		public float x;
		public float y;
		public int flip;
		public int order;
		public int renderOrder;
		public int animated;
		public int type;
	}

	/// <summary>关卡场景物件集合</summary>
	[Serializable]
	public class FerryLevelSceneData
	{
		public int id;
		public List<FerrySceneObjData> scene = new List<FerrySceneObjData>();
	}

	/// <summary>棋子动画集合</summary>
	[Serializable]
	public class FerryPieceAnimData
	{
		public int id;
		public string sheet;
		public List<FerryAnimData> anims = new List<FerryAnimData>();
	}

	/// <summary>场景循环动画（水面/鱼群/NPC），同一张图按 row 分行</summary>
	[Serializable]
	public class FerrySceneAnimData
	{
		public string image;

		/// <summary>原版 .spt 里的行号：水面按关卡 waterType 选行（1 用 0 行，2 用 2 行）</summary>
		public int row = -1;

		public int fps;
		public List<FerryFrameData> frames = new List<FerryFrameData>();
	}

	/// <summary>船夫动画</summary>
	[Serializable]
	public class FerryHeroData
	{
		public List<FerryAnimData> man = new List<FerryAnimData>();
		public List<FerryAnimData> walk = new List<FerryAnimData>();
	}

	/// <summary>
	/// board.json 的根对象：场景物件 + 动画帧数据（体积大，不进 Excel）
	/// </summary>
	[Serializable]
	public class FerryBoardData
	{
		public int version;
		public List<FerryLevelSceneData> levels = new List<FerryLevelSceneData>();
		public List<FerryPieceAnimData> pieces = new List<FerryPieceAnimData>();
		public List<FerrySceneAnimData> sceneAnims = new List<FerrySceneAnimData>();
		public FerryHeroData hero = new FerryHeroData();
	}
}
