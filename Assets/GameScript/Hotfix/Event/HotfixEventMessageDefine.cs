
namespace Hotfix
{
	/// <summary>
	/// 业务层的事件标签
	/// </summary>
	public enum HotfixEventMessageTag
	{
		FerryEvent,
	}

	/// <summary>音效提示类型（Core 层只发意图，声音由表现层 FerryAudio 播放）</summary>
	public enum EFerryAudioCue
	{
		Board,
		Unload,
		Row,
		Eat,
		Dismiss,
		Spawn,
		Combo,
		Win,
		Lose,
	}

	/// <summary>
	/// 过河玩法事件
	/// 注意：消息类统一写在这里，格式为「静态容器类 + 内嵌 IHotfixEventMessage 类」，字段全 public
	/// </summary>
	public class FerryEvent
	{
		/// <summary>对局状态变化（棋子上下船、渡河、吃子后）</summary>
		public class SessionChanged : IHotfixEventMessage
		{
		}

		/// <summary>界面提示文本</summary>
		public class Toast : IHotfixEventMessage
		{
			public string Message;
		}

		/// <summary>玩法动作（只作为音效/表现提示，不承载逻辑状态）</summary>
		public class Action : IHotfixEventMessage
		{
			public EFerryAudioCue Cue;
			public string PieceSound;
		}

		/// <summary>单个单位结算（用于浮字 NxCombo）</summary>
		public class Scored : IHotfixEventMessage
		{
			public int Value;
			public int SameCount;
			public string PieceCode;
		}

		/// <summary>关卡结束（胜利或失败）</summary>
		public class LevelEnd : IHotfixEventMessage
		{
			public bool IsWin;
			public int LevelId;
			public int Score;
			public float ElapsedSeconds;
			public string ElapsedText;
		}

		/// <summary>请求切换状态（UI 只发意图，由状态机执行）</summary>
		public class ChangeState : IHotfixEventMessage
		{
			public EHotfixStateType State;
		}
	}
}
