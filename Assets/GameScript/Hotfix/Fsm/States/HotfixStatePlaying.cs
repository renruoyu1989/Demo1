using System.Collections.Generic;
using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 关卡内玩法状态
	/// 负责：加载 board.json → 建立对局 → 生成 2D 棋盘相机与 FerryBoardView → 打开 HUD/教程
	/// 表现层由本状态的 Execute 驱动，保持单一 Update 根
	/// </summary>
	public class HotfixStatePlaying : HotfixFsmState
	{
		private GameObject _root;
		private Camera _boardCamera;
		private Camera _cachedMainCamera;
		private FerryBoardView _boardView;
		private FerryGameSession _session;

		public HotfixStatePlaying(EHotfixStateType type) : base(type)
		{
		}

		public override void Enter()
		{
			// 先监听，保证 StartLevel 发出的 SessionChanged 不会丢
			HotfixEventManager.Instance.AddListener(HotfixEventMessageTag.FerryEvent, Handle_FerryEvent);
			FerryConfigService.Instance.EnsureBoardData(Handle_BoardDataReady);
		}

		private void Handle_BoardDataReady()
		{
			// 加载期间玩家可能已经切走
			if (HotfixFsmManager.Instance.RunStateType != EHotfixStateType.Playing)
				return;

			int levelId = FerryGameCommands.PendingLevelId;
			if (levelId <= 0)
			{
				HotfixLogger.Error("Ferry playing state without a pending level id");
				FerryGameCommands.GoSelect();
				return;
			}

			if (FerryGameFlow.Instance.StartLevel(levelId) == false)
			{
				FerryGameCommands.GoSelect();
				return;
			}

			_session = FerryGameFlow.Instance.Session;
			CreateBoard();

			UIManager.Instance.OpenWindow(EWindowType.UIHud);

			List<CfgTutorialStepTab> steps = FerryConfigService.Instance.GetTutorialSteps(levelId);
			if (steps.Count > 0)
				UIManager.Instance.OpenWindow(EWindowType.UITutorial);
		}

		private void CreateBoard()
		{
			// 换关时整块表现重建：先拆干净，否则会残留棋盘相机与旧贴图
			DestroyBoard();

			_root = new GameObject("FerryBoardRoot");
			_boardCamera = CreateBoardCamera();

			List<FerryDockLayout> layouts = FerryConfigService.Instance.BuildDockLayouts(_session.Level.Dto);
			_boardView = new FerryBoardView();
			_boardView.Init(_root.transform, _boardCamera, _session, layouts, null);
		}

		private Camera CreateBoardCamera()
		{
			// 暂时关掉场景主相机，避免两套渲染叠在一起
			if (_cachedMainCamera == null)
			{
				_cachedMainCamera = Camera.main;
				if (_cachedMainCamera != null)
					_cachedMainCamera.enabled = false;
			}

			GameObject go = new GameObject("FerryBoardCamera");
			Camera camera = go.AddComponent<Camera>();
			// 必须排在 UI 相机之前渲染，否则整屏棋盘会把 uGUI 盖掉
			camera.depth = -1;
			camera.clearFlags = CameraClearFlags.SolidColor;
			// 原版整屏是天空蓝底色，棋盘浮在中间
			camera.backgroundColor = new Color(0.42f, 0.66f, 0.86f, 1f);
			camera.orthographic = true;
			camera.transform.position = new Vector3(500f, 281f, -10f);
			return camera;
		}

		private void DestroyBoard()
		{
			if (_boardView != null)
			{
				_boardView.Destroy();
				_boardView = null;
			}

			if (_root != null)
			{
				Object.Destroy(_root);
				_root = null;
			}

			if (_boardCamera != null)
			{
				Object.Destroy(_boardCamera.gameObject);
				_boardCamera = null;
			}

			if (_cachedMainCamera != null)
			{
				_cachedMainCamera.enabled = true;
				_cachedMainCamera = null;
			}
		}

		private void Handle_FerryEvent(IHotfixEventMessage message)
		{
			FerryEvent.Action action = message as FerryEvent.Action;
			if (action != null)
			{
				FerryAudio.PlayCue(action.Cue, action.PieceSound);
				return;
			}

			FerryEvent.SessionChanged changed = message as FerryEvent.SessionChanged;
			if (changed != null)
			{
				// StartLevel 的广播早于本状态拿到会话，忽略
				if (_session == null)
					return;

				if (FerryGameFlow.Instance.Session != _session)
				{
					// 重开一关：整套表现重建
					_session = FerryGameFlow.Instance.Session;
					CreateBoard();
				}
				else if (_boardView != null)
				{
					_boardView.ApplyLayout();
				}
				return;
			}

			FerryEvent.Scored scored = message as FerryEvent.Scored;
			if (scored != null && _boardView != null)
			{
				string content = scored.SameCount > 1
					? $"{scored.SameCount}x  +{scored.Value}"
					: $"+{scored.Value}";
				Color color = scored.SameCount > 1 ? FerryUIBuilder.ColorAccent : FerryUIBuilder.ColorText;
				_boardView.AddFloatAtDock(_session.LastDockIndex, content, color);
				return;
			}

			FerryEvent.LevelEnd end = message as FerryEvent.LevelEnd;
			if (end != null)
			{
				UIManager.Instance.CloseWindow(EWindowType.UITutorial);
				UIManager.Instance.OpenWindow(EWindowType.UIResult);
			}
		}

		public override void Execute()
		{
			float deltaTime = Time.deltaTime;

			FerryGameFlow.Instance.Tick(deltaTime);

			if (_boardView != null)
				_boardView.Tick(deltaTime);
		}

		public override void Exit()
		{
			HotfixEventManager.Instance.RemoveListener(HotfixEventMessageTag.FerryEvent, Handle_FerryEvent);

			DestroyBoard();
			FerryGameFlow.Instance.ExitLevel();
			_session = null;

			UIManager.Instance.CloseWindow(EWindowType.UITutorial);
			UIManager.Instance.CloseWindow(EWindowType.UIResult);
			UIManager.Instance.CloseWindow(EWindowType.UIRules);
			UIManager.Instance.CloseWindow(EWindowType.UIHud);

			FerryConfigService.Instance.ReleaseBoardData();
		}
	}
}
