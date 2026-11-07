using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>
	/// UI 唯一的玩法入口：界面只发意图，不直接改对局状态
	/// 状态切换采用「挂起请求 + 主循环消费」的方式，避免在 UI 回调里同步 Exit/Enter
	/// </summary>
	public static class FerryGameCommands
	{
		/// <summary>待进入的关卡</summary>
		public static int PendingLevelId { get; private set; }

		/// <summary>挂起的状态切换请求</summary>
		private static bool _hasPendingState;
		private static EHotfixStateType _pendingState;

		/// <summary>当前对局</summary>
		public static FerryGameSession Session
		{
			get { return FerryGameFlow.Instance.Session; }
		}

		#region 流程
		/// <summary>请求切到主菜单</summary>
		public static void GoMenu()
		{
			RequestState(EHotfixStateType.Menu);
		}

		/// <summary>关卡选择要显示的页签（0 经典 / 1 连击），由状态机读取</summary>
		public static int PendingSelectMode { get; private set; }

		/// <summary>请求切到关卡选择：默认沿用当前关卡的模式</summary>
		public static void GoSelect()
		{
			FerryGameSession session = FerryGameFlow.Instance.Session;
			if (session != null)
				PendingSelectMode = session.Level.Dto.Kind == FerryDefine.KindTutorial ? 1 : 0;
			RequestState(EHotfixStateType.Select);
		}

		/// <summary>主菜单直接指定模式进入关卡选择</summary>
		public static void GoSelectMode(int mode)
		{
			PendingSelectMode = mode == 1 ? 1 : 0;
			RequestState(EHotfixStateType.Select);
		}

		/// <summary>选择关卡并开始</summary>
		public static void SelectLevel(int levelId)
		{
			PendingLevelId = levelId;

			// 已经在玩法内（下一关）：同状态不会触发 Enter，直接换关，由 SessionChanged 驱动表现重建
			if (HotfixFsmManager.Instance.RunStateType == EHotfixStateType.Playing)
			{
				if (FerryGameFlow.Instance.StartLevel(levelId) == false)
					GoSelect();
				return;
			}

			RequestState(EHotfixStateType.Playing);
		}

		/// <summary>重玩当前关卡</summary>
		public static void Restart()
		{
			int levelId = FerryGameFlow.Instance.LevelId;
			if (levelId <= 0)
				levelId = PendingLevelId;

			PendingLevelId = levelId;
			FerryGameFlow.Instance.StartLevel(levelId);
		}

		/// <summary>进入同模式的下一关，没有下一关时回到关卡选择</summary>
		public static void NextLevel()
		{
			int levelId = FerryGameFlow.Instance.LevelId;
			CfgLevelTab current = FerryConfigService.Instance.GetLevel(levelId);
			if (current == null)
			{
				GoSelect();
				return;
			}

			List<CfgLevelTab> levels = FerryConfigService.Instance.GetLevels(current.Kind);
			for (int i = 0; i < levels.Count; i++)
			{
				if (levels[i].Id != levelId)
					continue;

				if (i + 1 < levels.Count)
				{
					SelectLevel(levels[i + 1].Id);
				}
				else
				{
					GoSelect();
				}
				return;
			}

			GoSelect();
		}

		/// <summary>主循环里消费挂起的状态请求</summary>
		public static void ProcessPendingState()
		{
			if (_hasPendingState == false)
				return;

			_hasPendingState = false;
			HotfixFsmManager.Instance.ChangeState(_pendingState);
		}

		private static void RequestState(EHotfixStateType state)
		{
			_hasPendingState = true;
			_pendingState = state;
		}
		#endregion

		#region 玩法操作
		public static bool Board(FerryPiece piece)
		{
			FerryGameSession session = Session;
			if (session == null || session.Board(piece) == false)
				return false;

			SendCue(EFerryAudioCue.Board, piece != null && piece.Dto != null ? piece.Dto.Sound : null);
			return true;
		}

		public static bool Unload(FerryPiece piece)
		{
			FerryGameSession session = Session;
			if (session == null || session.Unload(piece) == false)
				return false;

			SendCue(EFerryAudioCue.Unload, piece != null && piece.Dto != null ? piece.Dto.Sound : null);
			return true;
		}

		public static bool Row(int direction)
		{
			FerryGameSession session = Session;
			if (session == null)
				return false;

			// 吃子记录由表现层在 Changed 事件里消费，这里用存活数/棋子数的变化来判是否要播对应音效
			int aliveBefore = CountAlive(session.Pieces);
			int piecesBefore = session.Pieces.Count;
			if (session.Row(direction) == false)
				return false;

			SendCue(EFerryAudioCue.Row, null);
			if (CountAlive(session.Pieces) < aliveBefore)
				SendCue(EFerryAudioCue.Eat, null);
			if (session.Pieces.Count > piecesBefore)
				SendCue(EFerryAudioCue.Spawn, null);
			return true;
		}

		private static int CountAlive(List<FerryPiece> pieces)
		{
			int count = 0;
			for (int i = 0; i < pieces.Count; i++)
			{
				if (pieces[i].Alive)
					count++;
			}
			return count;
		}

		public static bool Dismiss()
		{
			FerryGameSession session = Session;
			if (session == null || session.Dismiss() == false)
				return false;

			SendCue(EFerryAudioCue.Dismiss, null);
			return true;
		}

		private static void SendCue(EFerryAudioCue cue, string pieceSound)
		{
			FerryEvent.Action msg = new FerryEvent.Action();
			msg.Cue = cue;
			msg.PieceSound = pieceSound;
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.FerryEvent, msg);
		}
		#endregion
	}
}
