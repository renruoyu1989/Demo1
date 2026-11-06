using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hotfix
{
	[Window(EWindowType.UILogin, EWindowLayer.Panel)]
	public class UILogin : UIWindow
	{
		private UISprite _loginSprite;
		private InputField _account;
		private InputField _password;

		public override void OnCreate()
		{
			_loginSprite = GetUIComponent<UISprite>("UILogin/Window/Button (Login)") ;
			_account = GetUIComponent<InputField>("UILogin/Window/Content/Text Field (Username)");
			_password = GetUIComponent<InputField>("UILogin/Window/Content/Text Field (Password)");

			// 监听按钮点击事件
			AddButtonListener("UILogin/Window/Button (Login)", OnClickLogin);

			LocalizationManager.OnLanguageChanged += OnLanguageChanged;
			RefreshLocalization();
		}
		public override void OnDestroy()
		{
			LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
		}
		public override void OnRefresh()
		{
			RefreshLocalization();
		}
		public override void OnUpdate()
		{
		}

		/// <summary>
		/// 输入框占位文案本地化
		/// </summary>
		private void RefreshLocalization()
		{
			if (_account != null && _account.placeholder != null)
				_account.placeholder.GetComponent<Text>().text = LocalizationManager.Get("UILogin2");
			if (_password != null && _password.placeholder != null)
				_password.placeholder.GetComponent<Text>().text = LocalizationManager.Get("UILogin3");
		}
		private void OnLanguageChanged(ELanguage language)
		{
			RefreshLocalization();
		}

		private void OnClickLogin()
		{
			// 替换按钮图片
			if (_loginSprite.SpriteName == "Button_Rectangular_Large_Green_Background")
				_loginSprite.SpriteName = "Button_Rectangular_Large_Red_Background";
			else
				_loginSprite.SpriteName = "Button_Rectangular_Large_Green_Background";

			// 发送登录事件（单机版：直接用账号名作为存档槽）
			var message = new LoginEvent.ConnectServer();
			message.Account = string.IsNullOrEmpty(_account.text) ? "Player" : _account.text;
			message.Password = _password.text;
			HotfixEventManager.Instance.Send(HotfixEventMessageTag.LoginEvent, message);
		}
	}
}
