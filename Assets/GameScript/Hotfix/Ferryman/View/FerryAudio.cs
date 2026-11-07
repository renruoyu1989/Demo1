using MotionFramework.Audio;

namespace Hotfix
{
	/// <summary>
	/// 过河音效。Core 层只发 FerryEvent.Action 意图，声音在这里统一播，
	/// 保证 Core 不引用 UnityEngine / MotionFramework 音频模块。
	/// 资源走 YooAsset 全路径：Assets/GameRes/Ferryman/Audio/&lt;name&gt;。
	/// </summary>
	public static class FerryAudio
	{
		public const string LocationRoot = "Ferryman/Audio/";

		public static void PlayCue(EFerryAudioCue cue, string pieceSound)
		{
			switch (cue)
			{
				case EFerryAudioCue.Board:
					Play(string.IsNullOrEmpty(pieceSound) ? "Walk" : pieceSound);
					break;
				case EFerryAudioCue.Unload:
					Play("Walk");
					break;
				case EFerryAudioCue.Row:
					Play("Boat");
					break;
				case EFerryAudioCue.Eat:
					Play("fight");
					break;
				case EFerryAudioCue.Dismiss:
				case EFerryAudioCue.Combo:
					Play("cling");
					break;
				case EFerryAudioCue.Spawn:
					Play("Walk");
					break;
				case EFerryAudioCue.Win:
					Play("Win");
					break;
				case EFerryAudioCue.Lose:
					Play("Lost");
					break;
			}
		}

		/// <summary>UI 通用点击音（不带棋子名称）</summary>
		public static void Play(string name)
		{
			if (string.IsNullOrEmpty(name))
				return;

			try
			{
				AudioManager.Instance.PlaySound(LocationRoot + name);
			}
			catch (System.Exception e)
			{
				// 切场景/退出播放时音频源可能已被销毁，音效失败不能打断玩法操作
				HotfixLogger.Log("FerryAudio skip sound " + name + " : " + e.GetType().Name);
			}
		}
	}
}
