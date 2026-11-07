using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hotfix
{
	public abstract class EntityObject
	{
		public int EntityID { private set; get; }
		public GameWorld World { private set; get; }
		public GameObject Root { private set; get; }
		public EntityAvatar Avatar { private set; get; }
		
		/// <summary>
		/// Avatar是否准备完毕
		/// </summary>
		protected bool _isPrepareAvatar { private set; get; } = false;


		public EntityObject(int entityID)
		{
			EntityID = entityID;
		}
		public void Create(GameWorld world, int avatarID, Vector3 pos, Vector3 rot)
		{
			if (Root != null)
			{
				HotfixLogger.Error("EntityObject is create yet.");
				return;
			}

			World = world;

			// 先创建游戏对象：对象池命中时 Avatar 的加载回调是同步执行的，
			// 回调里会访问 Root，必须先于资源请求创建
			Root = new GameObject($"Entity{EntityID}");
			Root.transform.position = pos;
			Root.transform.rotation = Quaternion.Euler(rot);

			// 创建Avatar
			Avatar = new EntityAvatar(this, avatarID);
			Avatar.Create(OnAvatarLoad);

			OnCreate();
		}
		public void Destroy()
		{
			if(Avatar != null)
			{
				Avatar.Destroy();
				Avatar = null;
			}

			if (Root != null)
			{
				GameObject.Destroy(Root);
				Root = null;
			}

			OnDestroy();
		}
		public void Update(float deltaTime)
		{
			OnUpdate(deltaTime);

			if (_isPrepareAvatar)
				OnUpdateAvatar(deltaTime);
		}
		public void HandleEvent(IHotfixEventMessage msg)
		{
			OnHandleEvent(msg);
		}

		private void OnAvatarLoad()
		{
			OnPrepareAvatar();
			_isPrepareAvatar = true;
		}

		protected abstract void OnCreate();
		protected abstract void OnDestroy();
		protected abstract void OnUpdate(float deltaTime);
		protected abstract void OnPrepareAvatar();
		protected abstract void OnUpdateAvatar(float deltaTime);
		protected abstract void OnHandleEvent(IHotfixEventMessage msg);
	}
}