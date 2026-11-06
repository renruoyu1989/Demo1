using System;
using System.Collections;
using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>
	/// 登录数据（单机版）
	/// 注意：没有服务器校验，输入账号后直接读取本地存档进入城镇
	/// </summary>
	public class DataLogin : DataBase
	{
		private string _account;

		public override void Start()
		{
			AddEventListener(HotfixEventMessageTag.LoginEvent, OnHandleEventMessage);
		}
		public override void Update()
		{
		}
		public override void Destroy()
		{
		}

		private void OnHandleEventMessage(IHotfixEventMessage msg)
		{
			if (msg is LoginEvent.ConnectServer)
			{
				LoginEvent.ConnectServer message = msg as LoginEvent.ConnectServer;
				_account = message.Account;
				HotfixLogger.Log($"单机模式登录：{_account}");

				// 切换存档槽并载入该账号的存档
				DataManager.Instance.SetAccount(_account);
				DataManager.Instance.LoadAll(_account);

				DataPlayer dataPlayer = DataManager.Instance.GetData<DataPlayer>();
				if (dataPlayer != null)
					dataPlayer.SetAccount(_account);

				// 登录后立即存一次档（首次登录相当于创建存档）
				DataManager.Instance.SaveAll(_account);

				HotfixFsmManager.Instance.ChangeState(EHotfixStateType.Town);
			}
		}
	}
}
