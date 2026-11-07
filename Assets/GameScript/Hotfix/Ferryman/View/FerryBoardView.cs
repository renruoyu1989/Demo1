using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 棋盘总控：场景物件、码头、棋子、船、浮字
	/// 由 HotfixStatePlaying 每帧 Tick 驱动，自己不写 Update
	/// </summary>
	public class FerryBoardView
	{
		/// <summary>当前棋盘相机（教程气泡要按它把设计稿坐标投到画布上，并随横移跟随）</summary>
		public static Camera ActiveCamera { get; private set; }

		private Transform _root;
		private Camera _camera;
		private FerryBoardAnchor _anchor;
		private FerryGameSession _session;
		private List<FerryDockLayout> _layouts;

		private readonly FerryAssetCache _cache = new FerryAssetCache();
		private readonly List<GameObject> _sceneObjects = new List<GameObject>();
		private readonly List<FerryPieceView> _pieceViews = new List<FerryPieceView>();
		private readonly List<FerryFloatTextView> _floats = new List<FerryFloatTextView>();
		private readonly List<FerrySpriteAnimator> _sceneAnims = new List<FerrySpriteAnimator>();
		private readonly Dictionary<string, Sprite[]> _animCache = new Dictionary<string, Sprite[]>();
		private FerryBoatView _boat;
		private float _boatHalfWidth = 92f;
		private float _boatHeight = 40f;
		private float _lastBoatX;
		private FerryDockSlots _dockSlots;

		/// <summary>上一次布局时的停靠侧，用于识别「只换侧 = 岸上搬船」</summary>
		private int _lastBoatSide = 1;

		/// <summary>本次位移是不是岸上搬船（河里划船一律保持船夫原有朝向，不改方向）</summary>
		private bool _carryMove;

		/// <summary>水面矩形（多河道关卡有多条，按 X 就近取用）</summary>
		private class WaterRect
		{
			public float X;         // 设计稿左边缘
			public float Width;     // 设计稿宽度
			public float TopY;      // 设计稿顶边（Y 向下）
			public float CenterX { get { return X + Width * 0.5f; } }
		}

		private readonly List<WaterRect> _waters = new List<WaterRect>();

		/// <summary>场景里一个 Water 物件都没有时的兜底水面 Y</summary>
		private float _fallbackWaterTopY;

		private Action _onReady;
		private bool _ready;

		/// <summary>棋盘是否准备完毕（贴图加载完成后才能显示）</summary>
		public bool IsReady { get { return _ready; } }
		public Camera Camera { get { return _camera; } }
		public FerryBoardAnchor Anchor { get { return _anchor; } }

		#region 初始化
		public void Init(Transform parent, Camera camera, FerryGameSession session, List<FerryDockLayout> layouts, Action onReady)
		{
			_root = parent;
			_camera = camera;
			ActiveCamera = camera;
			_session = session;
			_layouts = layouts;
			_onReady = onReady;

			_anchor = new FerryBoardAnchor(_camera, session.Level.Dto.CanvasWidth);
			_dockSlots = new FerryDockSlots(session, layouts);

			_cache.LoadTextures(CollectTextureNames(), Handle_AssetsLoaded);
		}

		private List<string> CollectTextureNames()
		{
			List<string> names = new List<string>();

			// 场景物件
			List<FerrySceneObjData> scene = FerryConfigService.Instance.GetLevelScene(_session.Level.Dto.Id);
			for (int i = 0; i < scene.Count; i++)
				names.Add(ShortName(scene[i].img));

			// 棋子图集（表格里存的是 "Art/xxx" 形式，这里取文件名）
			for (int i = 0; i < _session.Pieces.Count; i++)
				names.Add(ShortName(_session.Pieces[i].Dto.Sheet));

			// 船与船夫
			names.Add("boat" + _session.Level.Dto.Boat);
			names.Add("man");
			names.Add("walk");

			return names;
		}

		private static string ShortName(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			int index = path.LastIndexOf('/');
			return index >= 0 ? path.Substring(index + 1) : path;
		}

		private void Handle_AssetsLoaded()
		{
			BuildScene();
			BuildBoat();
			SyncPieces();

			// ApplyLayout 内部会拦未就绪的调用，这里先置位再摆放，否则棋子/船会停在原点
			_ready = true;
			ApplyLayout(true);

			if (_onReady != null)
				_onReady();
		}

		private void BuildScene()
		{
			List<FerrySceneObjData> scene = FerryConfigService.Instance.GetLevelScene(_session.Level.Dto.Id);

			// 水面矩形：浪花长条要正好铺在这个宽度上，所以先把所有 Water 矩形收集起来。
			// 多河道关卡有两条河，浪花线必须各对各的水面，全铺到第一条上会整体错位。
			CollectWaters(scene);

			// 关卡真实内容范围（贴图包围盒），相机横移要按它来夹，不能只看 canvasWidth
			float contentMinX = float.MaxValue;
			float contentMaxX = float.MinValue;

			for (int i = 0; i < scene.Count; i++)
			{
				FerrySceneObjData data = scene[i];

				// 粒子发射器不铺贴图；云把渐变天空烘进了贴图，铺上去是矩形色块
				if (data.type == 3 || FerryDefine.IsSkipSceneTemplate(data.tpl))
					continue;

				string image = ShortName(data.img);
				if (string.IsNullOrEmpty(image))
					continue;

				bool isWater = data.tpl == FerryDefine.TplWaterUp;
				WaterRect water = isWater ? NearestWater(data.x) : null;

				// 有 anims 记录的物件（水面/鱼群/NPC）按帧矩形切帧，否则整张图都是水面墙
				FerrySceneAnimData animData = data.animated != 0 ? GetSceneAnim(image, SceneAnimRow(data, isWater)) : null;
				Sprite[] frames = animData != null ? BuildSceneFrames(image, animData) : null;
				if (isWater && frames != null && frames.Length > 0 && water != null && water.Width > 0f)
					frames = BuildWaterSlideFrames(image, animData, water.Width);

				Sprite sprite = frames != null && frames.Length > 0 ? frames[0] : _cache.GetFullSprite(image);
				if (sprite == null)
					continue;

				GameObject go = new GameObject("Scene_" + image);
				go.transform.SetParent(_root, false);
				go.transform.position = ScenePosition(data, sprite, isWater, water);

				SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
				renderer.sprite = sprite;
				// 关卡数据里 flip = -1 才是镜像
				renderer.flipX = data.flip < 0;
				// 原版分两遍画：render_order 0 背景 / 1 前景，同遍内按数组下标从后往前叠
				renderer.sortingOrder = FerryDefine.SortingSceneLayer(data.renderOrder) + data.order;

				// 多帧的才有必要做循环播放
				if (frames != null && frames.Length > 1)
				{
					// 原版场景动画统一走 sceneAnimSpeed（0.5），水面/鱼群/NPC 都是半速
					float fps = (animData.fps > 0 ? animData.fps : FerryDefine.DefaultFps) * FerryDefine.SceneAnimSpeed;
					FerrySpriteAnimator animator = new FerrySpriteAnimator();
					animator.Renderer = renderer;
					animator.Play(image, frames, fps, true);
					_sceneAnims.Add(animator);
				}

				_sceneObjects.Add(go);

				Bounds bounds = renderer.bounds;
				if (bounds.size.x > 0f)
				{
					contentMinX = Mathf.Min(contentMinX, bounds.min.x);
					contentMaxX = Mathf.Max(contentMaxX, bounds.max.x);
				}

				if (data.tpl == FerryDefine.TplDock)
					ApplyDockGeometry(data, sprite);
			}

			if (contentMaxX > contentMinX)
				_anchor.SetContentBounds(contentMinX, contentMaxX);
		}

		/// <summary>收集本关所有 Water 矩形的宽度与顶边；一条都没有时记下兜底水面 Y</summary>
		private void CollectWaters(List<FerrySceneObjData> scene)
		{
			_waters.Clear();
			_fallbackWaterTopY = 0f;

			for (int i = 0; i < scene.Count; i++)
			{
				FerrySceneObjData data = scene[i];
				if (data.tpl != FerryDefine.TplWater && data.tpl != FerryDefine.TplWaterUp)
					continue;

				Sprite sprite = _cache.GetFullSprite(ShortName(data.img));
				if (sprite == null)
					continue;

				if (data.tpl == FerryDefine.TplWater)
				{
					WaterRect rect = new WaterRect();
					rect.X = data.x;
					rect.Width = sprite.rect.width;
					rect.TopY = data.y;
					_waters.Add(rect);
				}
				else if (_fallbackWaterTopY <= 0f)
				{
					// 原版没有 Water 时退到「浪花线底边」当水面
					_fallbackWaterTopY = data.y + sprite.rect.height;
				}
			}

			if (_waters.Count == 0 && _fallbackWaterTopY <= 0f)
				_fallbackWaterTopY = 500f;      // 562 画布下部的默认水线
		}

		/// <summary>离给定设计稿 X 最近的水面（多河道关卡各用各的）</summary>
		private WaterRect NearestWater(float imgX)
		{
			if (_waters.Count == 0)
				return null;

			WaterRect best = _waters[0];
			float bestDistance = Mathf.Abs(best.CenterX - imgX);
			for (int i = 1; i < _waters.Count; i++)
			{
				float distance = Mathf.Abs(_waters[i].CenterX - imgX);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = _waters[i];
				}
			}
			return best;
		}

		/// <summary>水面高度（设计稿 Y）：优先 Water 顶边，其次浪花线底边，最后默认水线</summary>
		private float WaterSurfaceY(float imgX)
		{
			WaterRect water = NearestWater(imgX);
			return water != null ? water.TopY : _fallbackWaterTopY;
		}

		/// <summary>水面样式行号：ty1 用 default 行（0），其余按 ty 号取（原版 .spt 的行号）</summary>
		private int SceneAnimRow(FerrySceneObjData data, bool isWater)
		{
			if (isWater == false)
				return 0;

			int waterType = _session != null ? _session.Level.Dto.WaterType : 1;
			return waterType <= 1 ? 0 : waterType;
		}

		/// <summary>
		/// 场景物件的落点。关卡数据以贴图左上角为锚点（Y 向下），而精灵 pivot 在底边中点，
		/// 这里换算成「左上角对齐」的位置。
		/// 水面浪花线按原版：帧的**顶边**贴住水面上方 2px，横向对齐该水面的左边缘，
		/// 用底边 pivot 时要把帧高补回来，否则整条浪花会抬高一整个帧高。
		/// </summary>
		private Vector3 ScenePosition(FerrySceneObjData data, Sprite sprite, bool isWater, WaterRect water)
		{
			float width = sprite.rect.width;
			float height = sprite.rect.height;

			if (isWater && water != null)
				return _anchor.ToWorld(water.X + width * 0.5f, water.TopY - FerryDefine.WaterSurfaceLift + height);

			return _anchor.ToWorld(data.x + width * 0.5f, data.y + height);
		}

		private void BuildBoat()
		{
			_boat = new FerryBoatView(_root, FerryDefine.SortingBoat, FerryDefine.SortingHero);

			Sprite boat = _cache.GetFullSprite("boat" + _session.Level.Dto.Boat);
			_boat.SetBoatSprite(boat);
			// 停靠时要让出岸沿，不能一半压在码头上
			_boatHalfWidth = boat != null ? boat.rect.width * 0.5f * 0.95f : 92f;
			_boatHeight = boat != null ? boat.rect.height : 40f;
		}

		/// <summary>
		/// 船底所在的设计稿 Y（Y 向下 = 数值越大越靠屏幕下方）。
		/// 原版船用的是中心 pivot：船中心 = 水面 + boatH*BoatSinkRatio，
		/// 因此船底 = 水面 - boatH*(0.5 - BoatSinkRatio)。
		/// 本工程所有 Sprite 统一用底边 pivot，必须补回这半个船高，
		/// 否则整条船会相对船夫/乘客抬高 boatH*0.5（boatH=34 时约 17~20px）。
		/// </summary>
		private float BoatBottomY(float imgX)
		{
			return WaterSurfaceY(imgX) + _boatHeight * (0.5f - FerryDefine.BoatSinkRatio);
		}

		/// <summary>
		/// 甲板线的设计稿 Y（船上乘客/船夫的落脚点）。
		/// 原版 _boatDeckY = 船中心 + boatH*BoardDeckRatio = 水面 + boatH*(BoatSinkRatio+BoardDeckRatio)，
		/// 与船用哪种 pivot 无关：修船的位置时不能让角色跟着挪。
		/// </summary>
		private float BoatDeckY(float imgX)
		{
			return WaterSurfaceY(imgX) - _boatHeight * (FerryDefine.BoatSinkRatio + FerryDefine.BoardDeckRatio);
		}

		/// <summary>
		/// 用 Dock 贴图的真实尺寸回填码头布局：布局里的 CenterX 存的是 Dock 物件左上角 X，
		/// 表现层要的是贴图中心与半宽（船停在岸沿外侧、相机跟随都要用）
		/// </summary>
		private void ApplyDockGeometry(FerrySceneObjData data, Sprite sprite)
		{
			if (_layouts == null)
				return;

			float half = sprite.rect.width * 0.5f;
			for (int i = 0; i < _layouts.Count; i++)
			{
				if (Mathf.Abs(_layouts[i].CenterX - data.x) > 0.5f)
					continue;

				_layouts[i].CenterX = data.x + half;
				_layouts[i].HalfWidth = half;
				return;
			}
		}
		#endregion

		#region 棋子同步与布局
		private void SyncPieces()
		{
			// 新增（连击模式会往队列里补员）
			for (int i = 0; i < _session.Pieces.Count; i++)
			{
				FerryPiece piece = _session.Pieces[i];
				if (FindView(piece) != null)
					continue;

				GameObject go = new GameObject("Piece_" + piece.Dto.Code);
				// 原版按棋子下标叠层：后加入的压在上面，避免同 y 的棋子闪烁
				FerryPieceView view = new FerryPieceView(piece, go, _root, FerryDefine.SortingPiece + i);
				_pieceViews.Add(view);

				// 进关时分配固定槽位号，之后被吃掉的棋子不会让其他人挪位置
				_dockSlots.AssignSlot(piece);
			}
		}

		private FerryPieceView FindView(FerryPiece piece)
		{
			for (int i = 0; i < _pieceViews.Count; i++)
			{
				if (_pieceViews[i].Piece == piece)
					return _pieceViews[i];
			}
			return null;
		}

		/// <summary>重新计算所有棋子和船的目标位置</summary>
		public void ApplyLayout(bool instant = false)
		{
			if (_ready == false)
				return;

			// 连击模式会中途补员：先把新棋子的视图与槽位补齐，再统一摆放
			SyncPieces();

			// 船：停在码头的河面一侧，让出岸沿；高度贴当前这条河的水面
			FerryDockLayout dock = GetLayout(_session.BoatDock);
			float shore = dock.HalfWidth > 0f ? dock.HalfWidth : 96f;
			float boatX = dock.CenterX + _session.BoatSide * (shore + _boatHalfWidth + FerryDefine.BoatShoreGap);
			float boatBottom = BoatBottomY(boatX);
			float deckY = BoatDeckY(boatX);
			Vector3 boatPosition = _anchor.ToWorld(boatX, boatBottom);

			// 只识别「岸上搬船」这一步：停靠侧翻转 + LastMoveShore。
			// 河里划船完全不处理方向：原版 _hero 从不 flipX，default/right/left 三行帧朝向也一致，
			// 船夫朝向始终不变；只有岸上 Carrier 才按行进方向翻转。
			// LastMoveShore 会残留到后续上船/下船，所以必须靠 sideChanged 限定，且其余情况不清标记，
			// 否则一次 Board/Unload 就会打断正在进行的搬船动画。
			bool sideChanged = _lastBoatSide != _session.BoatSide;
			if (instant)
				_carryMove = false;
			else if (sideChanged && _session.LastMoveShore)
			{
				_carryMove = true;
				SetupCarryPath(dock);
			}
			else if (sideChanged)
				_carryMove = false;

			_lastBoatSide = _session.BoatSide;

			_boat.MoveTo(boatPosition, instant);

			// 船夫：固定在船尾侧（原版 heroOffsetX = -26），脚沉到甲板线下方 18px
			_boat.MoveHeroTo(_anchor.ToWorld(boatX + FerryDefine.HeroOffsetX, deckY + FerryDefine.HeroSinkY));
			_lastBoatX = _boat.Go.transform.position.x;

			// 码头上的棋子：站位表只算一次，离开的动物不会让其他人挪位置。
			// 原版首次 Refresh 会连空码头一起建表（中间码头的左右半区要在船出发前就定下来），
			// 这里不能因为码头没人就跳过 Ensure。
			for (int d = 0; d < _session.Docks.Count; d++)
			{
				List<FerryPiece> onDock = _session.PiecesOn(d);
				_dockSlots.Ensure(d, onDock.Count);
				if (onDock.Count == 0)
					continue;

				FerryDockLayout layout = GetLayout(d);
				bool twoRiver = _dockSlots.IsTwoRiverDock(d);

				for (int i = 0; i < onDock.Count; i++)
				{
					FerryPiece piece = onDock[i];
					FerryPieceView view = FindView(piece);
					if (view == null)
						continue;

					// 中间码头的站位表按「本码头第几只」排，其余码头用进关时分配的固定槽位
					int slot = twoRiver ? i : _dockSlots.SlotOf(piece, i);
					// 原版 PieceView.Offset = (renderOffsetX, -renderOffsetY)，码头与船上都要加横向偏移
					float x = _dockSlots.SlotX(d, slot) + piece.Dto.OffsetX;
					float y = layout.DeckY + piece.Dto.OffsetY;
					view.MoveTo(_anchor.ToWorld(x, y), instant);
				}
			}

			// 船上的棋子
			for (int i = 0; i < _session.Boat.Count; i++)
			{
				FerryPieceView view = FindView(_session.Boat[i]);
				if (view == null)
					continue;

				float x = BoatRiderX(boatX, i, _session.Boat.Count) + _session.Boat[i].Dto.OffsetX;
				float y = deckY + _session.Boat[i].Dto.OffsetY;
				view.MoveTo(_anchor.ToWorld(x, y), instant);
			}

			// 已吃掉/遣散/隐藏
			for (int i = 0; i < _pieceViews.Count; i++)
			{
				FerryPieceView view = _pieceViews[i];
				bool visible = view.Piece.Alive;
				if (view.Go.activeSelf != visible)
					view.Go.SetActive(visible);
			}

			_anchor.FollowX(boatX);
			UpdateAnims();
		}

		private FerryDockLayout GetLayout(int index)
		{
			if (_layouts == null || _layouts.Count == 0)
			{
				FerryDockLayout fallback = new FerryDockLayout();
				fallback.CenterX = FerryDefine.DesignWidth * 0.5f;
				fallback.DeckY = 200f;
				fallback.SlotXs.Add(fallback.CenterX - 96f);
				fallback.SlotXs.Add(fallback.CenterX);
				fallback.SlotXs.Add(fallback.CenterX + 96f);
				return fallback;
			}

			if (index < 0) index = 0;
			if (index > _layouts.Count - 1) index = _layouts.Count - 1;
			return _layouts[index];
		}

		/// <summary>船上第 index 位乘客（共 count 位）的落脚 X（含单人船整体右移）</summary>
		private float BoatRiderX(float boatX, int index, int count)
		{
			float shift = _session.BoatCapacity <= 1 ? FerryDefine.BoatRiderSingleShiftX : 0f;
			return boatX + (index - (count - 1) * 0.5f) * FerryDefine.BoatRiderStep + shift;
		}

		/// <summary>
		/// 岸上搬船：船夫沿码头甲板从旧侧岸沿走到新侧岸沿（原版 DockEdgeWorldX 取 0.88 半宽）。
		/// 本工程 Sprite 用底边 pivot，直接把脚放在甲板线上即可；原版中心 pivot 才需要另做补偿。
		/// </summary>
		private void SetupCarryPath(FerryDockLayout dock)
		{
			float half = dock.HalfWidth > 0f ? dock.HalfWidth : 96f;
			float edge = half * FerryDefine.DockEdgeRatio;
			float fromX = dock.CenterX + _lastBoatSide * edge;
			float toX = dock.CenterX + _session.BoatSide * edge;
			_boat.BeginCarry(_anchor.ToWorld(fromX, dock.DeckY), _anchor.ToWorld(toX, dock.DeckY));
		}

		private void UpdateAnims()
		{
			for (int i = 0; i < _pieceViews.Count; i++)
			{
				FerryPieceView view = _pieceViews[i];
				if (view.Piece.Alive == false)
					continue;

				string animName = view.IsMoving ? FerryDefine.AnimWalk : FerryDefine.AnimIdle;
				FerryPieceAnimData animData = FerryConfigService.Instance.GetPieceAnim(view.Piece.Dto.Id);
				if (animData == null)
					continue;

				FerryAnimData anim = FerryConfigService.FindAnim(animData.anims, animName);
				Sprite[] frames = BuildFrames(animData.sheet, anim);
				// 动物动画走 pieceAnimSpeed（原版 2 倍速）
				float fps = (anim != null ? anim.Fps : FerryDefine.DefaultFps) * FerryDefine.PieceAnimSpeed;
				view.Play(animName, frames, fps);
			}

			UpdateBoatAnims();
		}

		/// <summary>
		/// 船夫动作切换：只有岸上搬船才换成 hero.walk.default 的 Carrier（帧里自带船），
		/// 河里移动和停稳都播 hero.man.default 的划桨循环，且不改动船夫朝向。
		/// 本工程切帧 Sprite 都是底边 pivot，不同帧高的动作脚底天然对齐，
		/// 因此不需要原版 SetHeroAnim 里那套 offsetY 基线补偿。
		/// </summary>
		private void UpdateBoatAnims()
		{
			FerryHeroData hero = FerryConfigService.Instance.Board != null ? FerryConfigService.Instance.Board.hero : null;
			if (hero == null || _boat == null)
				return;

			bool moving = _boat.IsMoving;
			if (moving == false)
				_carryMove = false;

			bool carrying = _carryMove && moving;
			_boat.SetCarrying(carrying);

			if (carrying)
			{
				FerryAnimData walk = FerryConfigService.FindAnim(hero.walk, FerryDefine.AnimIdle);
				if (walk == null && hero.walk.Count > 0)
					walk = hero.walk[0];
				_boat.PlayCarrier(walk != null ? walk.name : "walk", BuildFrames("walk", walk),
					(walk != null ? walk.Fps : FerryDefine.DefaultFps) * FerryDefine.HeroAnimSpeed);
				return;
			}

			FerryAnimData anim = FerryConfigService.FindAnim(hero.man, FerryDefine.AnimIdle);
			if (anim == null && hero.man.Count > 0)
				anim = hero.man[0];

			_boat.PlayHero(anim != null ? anim.name : "man", BuildFrames("man", anim),
				(anim != null ? anim.Fps : FerryDefine.DefaultFps) * FerryDefine.HeroAnimSpeed);
		}

		/// <summary>
		/// 按贴图名 + 行号查场景循环动画（水面/鱼群/Boss）。
		/// 同一张图可能有多行（水面按 waterType 分行），行号对不上时退回该图第一条，与原版一致。
		/// </summary>
		private static FerrySceneAnimData GetSceneAnim(string image, int row)
		{
			FerryBoardData board = FerryConfigService.Instance.Board;
			if (board == null || board.sceneAnims == null || string.IsNullOrEmpty(image))
				return null;

			FerrySceneAnimData other = null;
			for (int i = 0; i < board.sceneAnims.Count; i++)
			{
				FerrySceneAnimData anim = board.sceneAnims[i];
				if (anim == null || ShortName(anim.image) != image)
					continue;
				if (anim.frames == null || anim.frames.Count == 0)
					continue;

				if (anim.row == row)
					return anim;
				if (other == null)
					other = anim;
			}
			return other;
		}

		/// <summary>场景动画的 Sprite 帧序列（同贴图只切一次）</summary>
		private Sprite[] BuildSceneFrames(string image, FerrySceneAnimData anim)
		{
			if (anim == null || string.IsNullOrEmpty(image))
				return null;

			// 同一张图可能分红（水面按 waterType 分行），缓存键必须带行号
			string key = image + "|scene|" + anim.row;
			Sprite[] frames;
			if (_animCache.TryGetValue(key, out frames))
				return frames;

			frames = new Sprite[anim.frames.Count];
			for (int i = 0; i < frames.Length; i++)
				frames[i] = _cache.GetSprite(image, anim.frames[i]);

			_animCache[key] = frames;
			return frames;
		}

		private Sprite[] BuildFrames(string sheetName, FerryAnimData anim)
		{
			if (anim == null || string.IsNullOrEmpty(sheetName))
				return null;

			string sheet = ShortName(sheetName);
			string key = sheet + "|" + anim.name;
			Sprite[] frames;
			if (_animCache.TryGetValue(key, out frames))
				return frames;

			frames = new Sprite[anim.FrameCount];
			for (int i = 0; i < frames.Length; i++)
				frames[i] = _cache.GetSprite(sheet, anim.frames[i]);

			_animCache[key] = frames;
			return frames;
		}

		/// <summary>
		/// 水面波纹：原图是一条 3950 宽的连续长条，这里按窗口宽 = 水面宽度切成若干等宽窗口，
		/// 窗口逐帧右移就是「水在流动」。每帧宽度一致，浪花线不会跟着跳。
		/// </summary>
		private Sprite[] BuildWaterSlideFrames(string image, FerrySceneAnimData anim, float windowWidth)
		{
			string key = image + "|slide|" + Mathf.RoundToInt(windowWidth) + "|" + anim.row;
			Sprite[] cached;
			if (_animCache.TryGetValue(key, out cached))
				return cached;

			FerryFrameData source = anim.frames[0];
			float width = Mathf.Min(windowWidth, source.w);
			// 一个循环刚好滚过一屏宽
			float span = Mathf.Min(Mathf.Max(0f, source.w - width), width);
			if (width <= 0f || span <= 0f)
				return null;

			Sprite[] frames = new Sprite[FerryDefine.WaterSlideFrameCount];
			for (int i = 0; i < frames.Length; i++)
			{
				FerryFrameData frame = new FerryFrameData();
				frame.x = source.x + span * i / frames.Length;
				frame.y = source.y;
				frame.w = width;
				frame.h = source.h;
				frames[i] = _cache.GetSprite(image, frame);
			}

			_animCache[key] = frames;
			return frames;
		}
		#endregion

		#region 每帧驱动
		public void Tick(float deltaTime)
		{
			if (_ready == false)
				return;

			_anchor.EdgeScroll(deltaTime);
			_anchor.Tick(deltaTime);

			for (int i = 0; i < _pieceViews.Count; i++)
				_pieceViews[i].Tick(deltaTime);

			_boat.Tick(deltaTime);
			FollowBoatRiders();

			for (int i = 0; i < _sceneAnims.Count; i++)
				_sceneAnims[i].Tick(deltaTime);

			for (int i = _floats.Count - 1; i >= 0; i--)
			{
				if (_floats[i].Tick(deltaTime) == false)
					_floats.RemoveAt(i);
			}

			UpdateAnims();
			HandleInput();
		}

		/// <summary>
		/// 船横移时，船上乘客与船夫要跟着船一起动（原版 LayoutBoatRiders 每帧按船当前 X 重排）。
		/// 用本帧船的位移去平移，避免插值期间人先瞬移到终点、船才慢慢划过去。
		/// </summary>
		private void FollowBoatRiders()
		{
			if (_boat == null || _session == null)
				return;

			float x = _boat.Go.transform.position.x;
			float dx = x - _lastBoatX;
			_lastBoatX = x;
			if (Mathf.Abs(dx) < 0.001f)
				return;

			int count = _session.Boat.Count;
			for (int i = 0; i < count; i++)
			{
				FerryPieceView view = FindView(_session.Boat[i]);
				if (view == null)
					continue;

				float riderX = BoatRiderX(x, i, count) + _session.Boat[i].Dto.OffsetX;
				view.SetBoatRiderX(riderX);
			}
			_boat.SetHeroX(x + FerryDefine.HeroOffsetX);
		}

		private void HandleInput()
		{
			if (_session.IsOver)
				return;
			if (Input.GetMouseButtonDown(0) == false)
				return;

			// 点击 UI 时由 EventSystem 拦截，这里只处理棋盘
			Vector3 screen = Input.mousePosition;
			if (IsPointerOverUI())
				return;

			Vector3 world = _camera.ScreenToWorldPoint(screen);
			Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(world.x, world.y));
			for (int i = 0; i < hits.Length; i++)
			{
				for (int j = 0; j < _pieceViews.Count; j++)
				{
					FerryPieceView view = _pieceViews[j];
					if (view.Collider == null || view.Collider != hits[i])
						continue;

					OnPieceClicked(view);
					return;
				}
			}
		}

		private static bool IsPointerOverUI()
		{
			if (UnityEngine.EventSystems.EventSystem.current == null)
				return false;
			return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
		}

		private void OnPieceClicked(FerryPieceView view)
		{
			FerryPiece piece = view.Piece;
			if (piece.Alive == false)
				return;

			if (piece.OnBoat)
				FerryGameCommands.Unload(piece);
			else if (piece.Dock == _session.BoatDock)
				FerryGameCommands.Board(piece);
		}

		/// <summary>在某个码头上方冒一个浮字</summary>
		public void AddFloatAtDock(int dockIndex, string content, Color color)
		{
			FerryDockLayout layout = GetLayout(dockIndex);
			// Y 是屏幕向下坐标系，减掉 110 才是码头上方
			AddFloat(layout.CenterX, layout.DeckY - 110f, content, color);
		}

		/// <summary>在指定设计稿坐标处冒一个浮字</summary>
		public void AddFloat(float x, float y, string content, Color color, int fontSize = 48)
		{
			if (_ready == false)
				return;

			FerryFloatTextView view = FerryFloatTextView.Spawn(_root, _anchor.ToWorld(x, y), content, fontSize, color);
			_floats.Add(view);
		}
		#endregion

		public void Destroy()
		{
			for (int i = 0; i < _pieceViews.Count; i++)
				_pieceViews[i].Destroy();
			_pieceViews.Clear();

			for (int i = 0; i < _floats.Count; i++)
				_floats[i].Destroy();
			_floats.Clear();

			if (_boat != null)
			{
				_boat.Destroy();
				_boat = null;
			}

			for (int i = 0; i < _sceneObjects.Count; i++)
			{
				if (_sceneObjects[i] != null)
					UnityEngine.Object.Destroy(_sceneObjects[i]);
			}
			_sceneObjects.Clear();
			_sceneAnims.Clear();

			if (ActiveCamera == _camera)
				ActiveCamera = null;

			_animCache.Clear();
			if (_dockSlots != null)
				_dockSlots.Clear();
			_waters.Clear();
			_cache.ReleaseAll();
			_ready = false;
		}
	}
}
