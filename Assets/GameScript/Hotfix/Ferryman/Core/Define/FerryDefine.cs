
namespace Hotfix
{
	/// <summary>
	/// 过河玩法常量
	/// 注意：本目录（Core）不允许引用 UnityEngine，坐标换算一律放在 View 层
	/// </summary>
	public static class FerryDefine
	{
		/// <summary>每个抵达终点码头的单位得分 = 50 × 该码头同名数量</summary>
		public const int ScorePerPiece = 50;

		/// <summary>棋子所在位置：在船上</summary>
		public const int DockOnBoat = -1;

		/// <summary>种族ID（与表格 Piece.species 对应）</summary>
		public const int SpeciesCheese = 1;
		public const int SpeciesBull = 2;
		public const int SpeciesCat = 3;
		public const int SpeciesFox = 4;
		public const int SpeciesGuard = 5;
		public const int SpeciesHay = 6;
		public const int SpeciesSheep = 7;
		public const int SpeciesSkunk = 8;
		public const int SpeciesSnake = 9;
		public const int SpeciesTiger = 10;

		/// <summary>关卡模式（与表格 Level.kind 对应）</summary>
		public const int KindClassic = 0;
		public const int KindTutorial = 1;
		public const int KindCustom = 2;

		/// <summary>设计稿尺寸，用于表现层等比换算</summary>
		public const float DesignWidth = 1000f;
		public const float DesignHeight = 562f;

		/// <summary>默认帧率（表里 fps 缺失时使用）</summary>
		public const float DefaultFps = 20f;

		/// <summary>动画名</summary>
		public const string AnimIdle = "default";
		public const string AnimWalk = "walk";
		public const string AnimEat = "eat";
		public const string AnimSelect = "select";

		/// <summary>资源路径（YooAsset 地址 = 资源全路径，不带扩展名）</summary>
		public const string BoardDataLocation = "Assets/GameRes/Ferryman/Data/board";
		public const string EntityLocationRoot = "Assets/GameRes/Ferryman/Entity/";
		public const string AudioLocationRoot = "Assets/GameRes/Ferryman/Audio/";

		/// <summary>
		/// 棋盘排序层。原版以 1000 为一遍的跨度：render_order 0 → 0 段，1 → 1000 段，
		/// 船 1500、船夫 1501、棋子 2000 起。这里保持同样的量级，避免背景层物件多时串到前景。
		/// </summary>
		public const int SortingBackground = 0;
		public const int SortingBoard = 10;
		public const int SortingSceneStride = 1000;
		public const int SortingBoat = 3000;
		public const int SortingHero = 3001;
		public const int SortingPiece = 4000;
		public const int SortingFloatText = 5000;

		/// <summary>动画倍速（1 = 表里写的 fps），与原版 GameTuning 默认值一致</summary>
		public const float PieceAnimSpeed = 2f;
		public const float SceneAnimSpeed = 0.5f;
		public const float HeroAnimSpeed = 1f;

		/// <summary>浪花线顶边相对水面顶边的偏移（设计像素，向上为正）</summary>
		public const float WaterSurfaceLift = 2f;

		/// <summary>场景物件排序：先按 render_order 分遍，再按数组下标叠放</summary>
		public static int SortingSceneLayer(int renderOrder)
		{
			return SortingBackground + (renderOrder > 0 ? renderOrder : 0) * SortingSceneStride;
		}

		/// <summary>场景物件的模板名（与 LevelScene.tpl 对应）</summary>
		public const string TplWater = "Water";
		public const string TplWaterUp = "water up";

		/// <summary>水面波纹滑动窗口的帧数</summary>
		public const int WaterSlideFrameCount = 12;

		/// <summary>船停靠时与岸沿之间留的空隙（设计像素）</summary>
		public const float BoatShoreGap = 8f;

		/// <summary>船身上浮比例（吃水线大约落在船体下方这个比例处）</summary>
		public const float BoatSinkRatio = 0.18f;

		/// <summary>甲板线在船身高度上的位置</summary>
		public const float BoardDeckRatio = 0.35f;

		/// <summary>船上乘客的横向间距</summary>
		public const float BoatRiderStep = 46f;

		/// <summary>船上乘客：船容量为 1 时整体右移，避开船夫</summary>
		public const float BoatRiderSingleShiftX = 40f;

		/// <summary>船夫相对船中心的横向偏移（负 = 船尾）与甲板下沉量（设计像素，向下为正）</summary>
		public const float HeroOffsetX = -26f;
		public const float HeroSinkY = 18f;

		/// <summary>码头站位：最大列距、对岸首个站位右移、对岸列距</summary>
		public const float DockSlotMaxStep = 96f;
		public const float FarDockFirstShiftX = 24f;
		public const float FarDockStep = 56f;

		/// <summary>中间码头：整组动物贴向停靠侧岸边时的偏移比例（1 = 码头边缘）</summary>
		public const float DockSideSlotOffset = 0.7f;

		/// <summary>划一次船的时长（秒）</summary>
		public const float BoatMoveDuration = 0.75f;

		/// <summary>棋子走路：每「1 格/秒」折算的像素速度</summary>
		public const float WalkPixelsPerGrid = 30f;

		/// <summary>棋子走路时长的上下限（秒）</summary>
		public const float WalkMinDuration = 0.3f;
		public const float WalkMaxDuration = 2f;

		/// <summary>码头模板名</summary>
		public const string TplDock = "Dock";

		/// <summary>这些模板的原图把渐变天空烘进了贴图，铺上去会出现矩形色块，直接略过</summary>
		private static readonly string[] SkipSceneTemplates = { "Cloud 1", "Cloud 2", "Cloud 3" };

		public static bool IsSkipSceneTemplate(string template)
		{
			if (string.IsNullOrEmpty(template))
				return false;

			for (int i = 0; i < SkipSceneTemplates.Length; i++)
			{
				if (SkipSceneTemplates[i] == template)
					return true;
			}
			return false;
		}
	}
}
