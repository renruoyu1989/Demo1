using UnityEngine;
using UnityEngine.UI;

namespace Hotfix
{
	/// <summary>设置：语言、音乐、音效与音量，全部走 AudioPlayerSetting 持久化</summary>
	[Window(EWindowType.UIOptions, EWindowLayer.Panel)]
	public class UIOptions : UIWindow
	{
		private Button _zhButton;
		private Button _twButton;
		private Button _enButton;
		private Button _musicButton;
		private Button _soundButton;
		private Text _volumeText;

		public override void OnCreate()
		{
			RectTransform root = FerryUIBuilder.CreateFullScreen(Go.transform, "Root");
			FerryUIBuilder.CreateBackground(root, new Color(0f, 0f, 0f, 0.68f));
			FerryUIBuilder.CreatePanel(root, Vector2.zero, new Vector2(820f, 620f), FerryUIBuilder.ColorPanel);

			FerryUIBuilder.CreateText(root, "Title", LocalizationManager.Get("Ferry_Options"), 44, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorAccent, new Vector2(0f, 235f), new Vector2(760f, 70f));

			FerryUIBuilder.CreateText(root, "LanguageLabel", LocalizationManager.Get("Ferry_Language"),
				24, TextAnchor.MiddleLeft, FerryUIBuilder.ColorTextDim,
				new Vector2(-330f, 150f), new Vector2(220f, 50f));

			_zhButton = FerryUIBuilder.CreateSmallButton(root, "简体中文", new Vector2(-220f, 90f),
				new Vector2(200f, 60f), FerryUIBuilder.ColorButton, () => SetLanguage(ELanguage.ChineseSimplified), 22);

			_twButton = FerryUIBuilder.CreateSmallButton(root, "繁體中文", new Vector2(0f, 90f),
				new Vector2(200f, 60f), FerryUIBuilder.ColorButton, () => SetLanguage(ELanguage.ChineseTraditional), 22);

			_enButton = FerryUIBuilder.CreateSmallButton(root, "English", new Vector2(220f, 90f),
				new Vector2(200f, 60f), FerryUIBuilder.ColorButton, () => SetLanguage(ELanguage.English), 22);

			FerryUIBuilder.CreateText(root, "MusicLabel", LocalizationManager.Get("Ferry_Music"),
				24, TextAnchor.MiddleLeft, FerryUIBuilder.ColorTextDim,
				new Vector2(-330f, 10f), new Vector2(220f, 50f));
			_musicButton = FerryUIBuilder.CreateSmallButton(root, string.Empty, new Vector2(220f, 10f),
				new Vector2(200f, 60f), FerryUIBuilder.ColorButton, ToggleMusic, 22);

			FerryUIBuilder.CreateText(root, "SoundLabel", LocalizationManager.Get("Ferry_Sfx"),
				24, TextAnchor.MiddleLeft, FerryUIBuilder.ColorTextDim,
				new Vector2(-330f, -70f), new Vector2(220f, 50f));
			_soundButton = FerryUIBuilder.CreateSmallButton(root, string.Empty, new Vector2(220f, -70f),
				new Vector2(200f, 60f), FerryUIBuilder.ColorButton, ToggleSound, 22);

			FerryUIBuilder.CreateText(root, "VolumeLabel", LocalizationManager.Get("Ferry_Volume"),
				24, TextAnchor.MiddleLeft, FerryUIBuilder.ColorTextDim,
				new Vector2(-330f, -150f), new Vector2(220f, 50f));

			FerryUIBuilder.CreateSmallButton(root, "-", new Vector2(-30f, -150f),
				new Vector2(80f, 56f), FerryUIBuilder.ColorButton, () => ChangeVolume(-0.1f), 28);
			_volumeText = FerryUIBuilder.CreateText(root, "Volume", string.Empty, 26, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorText, new Vector2(100f, -150f), new Vector2(180f, 56f));
			FerryUIBuilder.CreateSmallButton(root, "+", new Vector2(230f, -150f),
				new Vector2(80f, 56f), FerryUIBuilder.ColorButton, () => ChangeVolume(0.1f), 28);

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Close"), new Vector2(0f, -240f),
				new Vector2(260f, 64f), () =>
				{
					AudioPlayerSetting.SaveAudioSetting();
					UIManager.Instance.CloseWindow(EWindowType.UIOptions);
				});

			Refresh();
		}

		public override void OnDestroy()
		{
		}

		public override void OnRefresh()
		{
			Refresh();
		}

		public override void OnUpdate()
		{
		}

		private void Refresh()
		{
			ELanguage language = LocalizationManager.Language;
			FerryUIBuilder.SetButtonColor(_zhButton, language == ELanguage.ChineseSimplified ? FerryUIBuilder.ColorAccent : FerryUIBuilder.ColorButton);
			FerryUIBuilder.SetButtonColor(_twButton, language == ELanguage.ChineseTraditional ? FerryUIBuilder.ColorAccent : FerryUIBuilder.ColorButton);
			FerryUIBuilder.SetButtonColor(_enButton, language == ELanguage.English ? FerryUIBuilder.ColorAccent : FerryUIBuilder.ColorButton);

			FerryUIBuilder.SetButtonLabel(_musicButton,
				LocalizationManager.Get(AudioPlayerSetting.MusicMute ? "Ferry_Off" : "Ferry_On"));
			FerryUIBuilder.SetButtonLabel(_soundButton,
				LocalizationManager.Get(AudioPlayerSetting.SoundMute ? "Ferry_Off" : "Ferry_On"));
			_volumeText.text = Mathf.RoundToInt(AudioPlayerSetting.AudioVolume * 100f) + "%";
		}

		private void SetLanguage(ELanguage language)
		{
			LocalizationManager.SetLanguage(language);
			Refresh();

			// 配置文本（CfgPiece.Name 等）在切换后重新构建，保证立即生效
			if (FerryGameFlow.Instance.Session != null)
				HotfixEventManager.Instance.Send(HotfixEventMessageTag.FerryEvent, new FerryEvent.SessionChanged());
		}

		private void ToggleMusic()
		{
			AudioPlayerSetting.MusicMute = !AudioPlayerSetting.MusicMute;
			Refresh();
		}

		private void ToggleSound()
		{
			AudioPlayerSetting.SoundMute = !AudioPlayerSetting.SoundMute;
			Refresh();
		}

		private void ChangeVolume(float delta)
		{
			AudioPlayerSetting.AudioVolume = Mathf.Clamp01(AudioPlayerSetting.AudioVolume + delta);
			Refresh();
		}
	}
}
