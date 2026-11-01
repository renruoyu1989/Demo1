using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MotionFramework.Audio;

namespace Hotfix
{
	[Window(EWindowType.UISetting, EWindowLayer.Panel)]
	public class UISetting : UIWindow
	{
		private Slider _volumeSlider;
		private Toggle _musicToggle;
		private Toggle _soundToggle;

		// 语言切换按钮（代码动态创建，不依赖预制体）
		private Text _languageText;

		public override void OnCreate()
		{
			AddButtonListener("UISetting/Mask", OnClickClose);
			AddButtonListener("UISetting/Window/Button (Save)", OnClickClose);

			_volumeSlider = GetUIComponent<Slider>("UISetting/Window/Content/Control Area (Slider)/Slider");
			_volumeSlider.onValueChanged.AddListener(OnSliderValueChange);
			_musicToggle = GetUIComponent<Toggle>("UISetting/Window/Content/Control Area (Toggles)/Toggle 1");
			_musicToggle.onValueChanged.AddListener(OnMusicToggleValueChange);
			_soundToggle = GetUIComponent<Toggle>("UISetting/Window/Content/Control Area (Toggles)/Toggle 2");
			_soundToggle.onValueChanged.AddListener(OnSoundToggleValueChange);

			CreateLanguageButton();
		}
		public override void OnDestroy()
		{
			LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
		}
		public override void OnRefresh()
		{
			_volumeSlider.value = AudioPlayerSetting.AudioVolume;
			_musicToggle.isOn = !AudioPlayerSetting.MusicMute;
			_soundToggle.isOn = !AudioPlayerSetting.SoundMute;

			RefreshLanguageText();
		}
		public override void OnUpdate()
		{
		}

		private void OnClickClose()
		{
			// 关闭窗口时保存一次数据（音量等设置类数据）
			AudioPlayerSetting.SaveAudioSetting();
			DataManager.Instance.SaveAll();
			UIManager.Instance.CloseWindow(EWindowType.UISetting);
		}
		private void OnSliderValueChange(float value)
		{
			AudioPlayerSetting.AudioVolume = value;
		}
		private void OnMusicToggleValueChange(bool value)
		{
			AudioPlayerSetting.MusicMute = !value;
		}
		private void OnSoundToggleValueChange(bool value)
		{
			AudioPlayerSetting.SoundMute = !value;
		}

		#region 语言切换
		private void CreateLanguageButton()
		{
			Transform content = GetUIElement("UISetting/Window/Content");
			if (content == null)
			{
				HotfixLogger.Warning("UISetting : not found content node, skip language button.");
				return;
			}

			// 复用界面里已有的字体
			Font font = GetDefaultFont();
			if (font == null)
			{
				HotfixLogger.Warning("UISetting : not found font, skip language button.");
				return;
			}

			GameObject buttonGo = new GameObject("Button (Language)");
			buttonGo.transform.SetParent(content, false);

			RectTransform rect = buttonGo.AddComponent<RectTransform>();
			rect.sizeDelta = new Vector2(240, 60);

			Image image = buttonGo.AddComponent<Image>();
			image.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

			Button button = buttonGo.AddComponent<Button>();
			button.targetGraphic = image;
			button.onClick.AddListener(OnClickSwitchLanguage);

			GameObject textGo = new GameObject("Text");
			textGo.transform.SetParent(buttonGo.transform, false);

			RectTransform textRect = textGo.AddComponent<RectTransform>();
			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;
			textRect.offsetMin = Vector2.zero;
			textRect.offsetMax = Vector2.zero;

			_languageText = textGo.AddComponent<Text>();
			_languageText.font = font;
			_languageText.alignment = TextAnchor.MiddleCenter;
			_languageText.color = Color.white;
			_languageText.fontSize = 24;

			LocalizationManager.OnLanguageChanged += OnLanguageChanged;
			RefreshLanguageText();
		}

		private void OnClickSwitchLanguage()
		{
			ELanguage next = (ELanguage)(((int)LocalizationManager.Language + 1) % 3);
			LocalizationManager.SetLanguage(next);
		}

		private void OnLanguageChanged(ELanguage language)
		{
			RefreshLanguageText();
		}

		private void RefreshLanguageText()
		{
			if (_languageText == null)
				return;

			string label = LocalizationManager.Get("UISetting6");
			string languageName = LocalizationManager.Get(GetLanguageKey(LocalizationManager.Language));
			_languageText.text = $"{label} : {languageName}";
		}

		private string GetLanguageKey(ELanguage language)
		{
			switch (language)
			{
				case ELanguage.ChineseSimplified:
					return "Language_ChineseSimplified";
				case ELanguage.ChineseTraditional:
					return "Language_ChineseTraditional";
				default:
					return "Language_English";
			}
		}
		#endregion
	}
}
