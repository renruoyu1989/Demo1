using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace Hotfix
{
	/// <summary>
	/// 过河玩法的配置查询入口
	/// 配表走 AssetConfig（Cfg*），体积大的场景/动画数据走 board.json
	/// </summary>
	public class FerryConfigService
	{
		private static FerryConfigService _instance;
		public static FerryConfigService Instance
		{
			get
			{
				if (_instance == null)
					_instance = new FerryConfigService();
				return _instance;
			}
		}

		private AssetHandle _handle;
		private bool _loading;
		private readonly List<Action> _pending = new List<Action>();
		private FerryBoardData _board;

		private FerryConfigService() { }

		/// <summary>场景/动画数据是否已就绪</summary>
		public bool IsBoardDataReady { get { return _board != null; } }

		public FerryBoardData Board { get { return _board; } }

		#region 数据加载
		/// <summary>异步加载 board.json，已加载则直接回调</summary>
		public void EnsureBoardData(Action callback)
		{
			if (_board != null)
			{
				if (callback != null) callback();
				return;
			}

			if (callback != null)
				_pending.Add(callback);

			if (_loading)
				return;

			_loading = true;
			_handle = YooAssets.LoadAssetAsync<TextAsset>(FerryDefine.BoardDataLocation);
			_handle.Completed += Handle_Completed;
		}

		private void Handle_Completed(AssetHandle handle)
		{
			_loading = false;

			TextAsset asset = handle.AssetObject as TextAsset;
			if (asset == null)
			{
				HotfixLogger.Error($"FerryConfigService load board data failed : {FerryDefine.BoardDataLocation}");
			}
			else
			{
				_board = JsonUtility.FromJson<FerryBoardData>(asset.text);
				if (_board == null)
					HotfixLogger.Error("FerryConfigService parse board data failed");
			}

			List<Action> callbacks = new List<Action>(_pending);
			_pending.Clear();
			for (int i = 0; i < callbacks.Count; i++)
			{
				if (callbacks[i] != null)
					callbacks[i]();
			}
		}

		/// <summary>释放 board.json（离开玩法时调用，避免 TextAsset 常驻）</summary>
		public void ReleaseBoardData()
		{
			if (_handle != null)
			{
				_handle.Release();
				_handle = null;
			}
			_board = null;
		}
		#endregion

		#region 配表查询
		public CfgLevelTab GetLevel(int levelId)
		{
			return CfgLevel.Instance.GetCfgTab(levelId);
		}

		/// <summary>按模式取关卡（按 id 升序）</summary>
		public List<CfgLevelTab> GetLevels(int kind)
		{
			List<CfgLevelTab> list = new List<CfgLevelTab>();
			List<int> keys = CfgLevel.Instance.GetKeys();
			keys.Sort();
			for (int i = 0; i < keys.Count; i++)
			{
				CfgLevelTab tab = CfgLevel.Instance.GetCfgTab(keys[i]);
				if (tab != null && (kind < 0 || tab.Kind == kind))
					list.Add(tab);
			}
			return list;
		}

		public CfgPieceTab GetPiece(int pieceId)
		{
			return CfgPiece.Instance.GetCfgTab(pieceId);
		}

		/// <summary>按种族取一个代表棋子（用于规则说明里的名字）</summary>
		public CfgPieceTab GetPieceBySpecies(int species)
		{
			List<int> keys = CfgPiece.Instance.GetKeys();
			for (int i = 0; i < keys.Count; i++)
			{
				CfgPieceTab tab = CfgPiece.Instance.GetCfgTab(keys[i]);
				if (tab != null && tab.Species == species)
					return tab;
			}
			return null;
		}

		/// <summary>取某关的初始布阵（按 order 升序）</summary>
		public List<CfgPieceTab> GetLevelPieces(int levelId)
		{
			List<CfgLevelPieceTab> placed = new List<CfgLevelPieceTab>();
			List<int> keys = CfgLevelPiece.Instance.GetKeys();
			for (int i = 0; i < keys.Count; i++)
			{
				CfgLevelPieceTab tab = CfgLevelPiece.Instance.GetCfgTab(keys[i]);
				if (tab != null && tab.LevelID == levelId)
					placed.Add(tab);
			}
			placed.Sort((a, b) => a.Order.CompareTo(b.Order));

			List<CfgPieceTab> list = new List<CfgPieceTab>();
			for (int i = 0; i < placed.Count; i++)
			{
				CfgPieceTab piece = GetPiece(placed[i].PieceID);
				if (piece != null)
					list.Add(piece);
			}
			return list;
		}

		/// <summary>取地图节点（0=经典 1=连击）</summary>
		public List<CfgLevelMapTab> GetMapNodes(int mode)
		{
			List<CfgLevelMapTab> list = new List<CfgLevelMapTab>();
			List<int> keys = CfgLevelMap.Instance.GetKeys();
			for (int i = 0; i < keys.Count; i++)
			{
				CfgLevelMapTab tab = CfgLevelMap.Instance.GetCfgTab(keys[i]);
				if (tab != null && tab.Mode == mode)
					list.Add(tab);
			}
			list.Sort((a, b) => a.Order.CompareTo(b.Order));
			return list;
		}

		/// <summary>取某关的教程步骤</summary>
		public List<CfgTutorialStepTab> GetTutorialSteps(int levelId)
		{
			List<CfgTutorialStepTab> list = new List<CfgTutorialStepTab>();
			List<int> keys = CfgTutorialStep.Instance.GetKeys();
			for (int i = 0; i < keys.Count; i++)
			{
				CfgTutorialStepTab tab = CfgTutorialStep.Instance.GetCfgTab(keys[i]);
				if (tab != null && tab.LevelID == levelId)
					list.Add(tab);
			}
			list.Sort((a, b) => a.Step.CompareTo(b.Step));
			return list;
		}
		#endregion

		#region 场景与动画
		/// <summary>取某关的场景物件</summary>
		public List<FerrySceneObjData> GetLevelScene(int levelId)
		{
			if (_board == null || _board.levels == null)
				return new List<FerrySceneObjData>();

			for (int i = 0; i < _board.levels.Count; i++)
			{
				if (_board.levels[i].id == levelId)
					return _board.levels[i].scene;
			}
			return new List<FerrySceneObjData>();
		}

		/// <summary>推导码头布局</summary>
		public List<FerryDockLayout> BuildDockLayouts(CfgLevelTab level)
		{
			List<FerrySceneObjData> scene = GetLevelScene(level.Id);
			return FerryDockLayoutBuilder.Build(level.CanvasWidth, scene);
		}

		/// <summary>取棋子的动画集合</summary>
		public FerryPieceAnimData GetPieceAnim(int pieceId)
		{
			if (_board == null || _board.pieces == null)
				return null;

			for (int i = 0; i < _board.pieces.Count; i++)
			{
				if (_board.pieces[i].id == pieceId)
					return _board.pieces[i];
			}
			return null;
		}

		/// <summary>按名字取一段动画</summary>
		public static FerryAnimData FindAnim(List<FerryAnimData> anims, string name)
		{
			if (anims == null)
				return null;

			for (int i = 0; i < anims.Count; i++)
			{
				if (anims[i].name == name)
					return anims[i];
			}
			return anims.Count > 0 ? anims[0] : null;
		}
		#endregion
	}
}
