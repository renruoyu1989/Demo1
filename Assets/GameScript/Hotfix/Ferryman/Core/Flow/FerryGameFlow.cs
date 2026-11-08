
using MotionFramework.Data;

namespace Hotfix
{
	/// <summary>
	/// 对局流程：持有当前 FerryGameSession，把逻辑层的事件翻译成框架事件
	/// 表现层与 UI 都只通过这里/ FerryGameCommands 交互
	/// </summary>
	public class FerryGameFlow
	{
		private static FerryGameFlow _instance;
		public static FerryGameFlow Instance
		{
			get
			{
				if (_instance == null)
					_instance = new FerryGameFlow();
				return _instance;
			}
		}

		/// <summary>当前对局</summary>
		public FerryGameSession Session { get; private set; }

		/// <summary>当前关卡ID</summary>
		public int LevelId { get; private set; }

		/// <summary>是否正在对局中</summary>
		public bool IsPlaying { get { return Session != null; } }

		private readonly FerryRuleEvaluator _evaluator = new FerryRuleEvaluator();
		private bool _levelEndSent;

		private FerryGameFlow() { }

		/// <summary>开始一关</summary>
		public bool StartLevel(int levelId)
		{
			ExitLevel();

			FerryLevelRuntime runtime = FerryLevelLoader.Build(levelId);
			if (runtime == null)
				return false;

			LevelId = levelId;
			_levelEndSent = false;
			Session = new FerryGameSession(runtime, _evaluator);
			Session.Toast += Handle_SessionToast;
			Session.Changed += Handle_SessionChanged;
			Session.Scored += Handle_SessionScored;

			HotfixLogger.Log($"Ferry start level {levelId} : {runtime.Dto.Code} pieces={runtime.Pieces.Count} docks={runtime.Docks.Count}");
			SendChanged();
			CheckLevelEnd();
			return true;
		}

		/// <summary>退出当前对局</summary>
		public void ExitLevel()
		{
			if (Session != null)
			{
				Session.Toast -= Handle_SessionToast;
				Session.Changed -= Handle_SessionChanged;
				Session.Scored -= Handle_SessionScored;
				Session = null;
			}
			LevelId = 0;
			_levelEndSent = false;
		}

		/// <summary>每帧驱动（由状态机调用，保证单一 Update 根）</summary>
		public void Tick(float deltaTime)
		{
			if (Session == null)
				return;

			Session.Tick(deltaTime);
			CheckLevelEnd();
		}

		private void CheckLevelEnd()
		{
			if (Session == null || _levelEndSent)
				return;
			if (Session.IsOver == false)
				return;

			_levelEndSent = true;

			FerryEvent.LevelEnd msg = new FerryEvent.LevelEnd();
			msg.IsWin = Session.IsWin;
			msg.LevelId = LevelId;
			msg.Score = Session.Score;
			msg.ElapsedSeconds = Session.ElapsedSeconds;
			msg.ElapsedText = Session.ElapsedText();
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.FerryEvent, msg);
			SendAction(Session.IsWin ? EFerryAudioCue.Win : EFerryAudioCue.Lose, null);

			// 记录进度与最高分
			DataFerryProfile data = DataManager.Instance.GetData<DataFerryProfile>();
			if (data != null)
			{
				data.RecordResult(LevelId, Session.IsWin, Session.Score);
				DataManager.Instance.SaveAll();
			}

			HotfixLogger.Log($"Ferry level end : level={LevelId} win={Session.IsWin} score={Session.Score} time={msg.ElapsedText}");
		}

		private void SendChanged()
		{
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.FerryEvent, new FerryEvent.SessionChanged());
		}

		private static void SendAction(EFerryAudioCue cue, string pieceSound)
		{
			FerryEvent.Action msg = new FerryEvent.Action();
			msg.Cue = cue;
			msg.PieceSound = pieceSound;
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.FerryEvent, msg);
		}

		private void Handle_SessionToast(string message)
		{
			FerryEvent.Toast msg = new FerryEvent.Toast();
			msg.Message = message;
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.FerryEvent, msg);
		}

		private void Handle_SessionChanged()
		{
			SendChanged();
			CheckLevelEnd();
		}

		private void Handle_SessionScored(FerryScoreEntry entry)
		{
			FerryEvent.Scored msg = new FerryEvent.Scored();
			msg.Value = entry.Value;
			msg.SameCount = entry.SameCount;
			msg.PieceCode = entry.Piece != null && entry.Piece.Dto != null ? entry.Piece.Dto.Code : string.Empty;
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.FerryEvent, msg);

			if (entry.SameCount >= 2)
				SendAction(EFerryAudioCue.Combo, null);
		}
	}
}
