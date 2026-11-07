using System;
using System.Collections;
using System.Collections.Generic;
using MotionFramework.Scene;
using MotionFramework.Audio;
using YooAsset;

namespace Hotfix
{
	public class HotfixStateTown : HotfixFsmState
	{
		private GameWorld _gameWorld = new GameWorld();
		private bool _initWorld = false;
		private bool _isEntered = false;

		public HotfixStateTown(EHotfixStateType type) : base(type)
		{
		}

		public override void Enter()
		{
			_isEntered = true;
			_initWorld = false;

			string sceneName = $"{GameDefine.AssetRootPath}/Scene/Town";
			SceneManager.Instance.ChangeMainScene(sceneName, true, OnSceneLoad);
			UIManager.Instance.OpenWindow(EWindowType.UILoading, sceneName);
			UIManager.Instance.OpenWindow(EWindowType.UIMain);
			AudioManager.Instance.PlayMusic("Audio/Music/town", true);
		}
		public override void Execute()
		{
			if(_initWorld)
				_gameWorld.Update();
		}
		public override void Exit()
		{
			// 复位标记：迟到的场景加载回调不能再初始化已经销毁的世界
			_isEntered = false;
			_initWorld = false;
			_gameWorld.Destroy();
		}
		
		private void OnSceneLoad(SceneHandle instance)
		{
			// 状态已经切走，忽略迟到的回调
			if (_isEntered == false)
			{
				HotfixLogger.Warning($"{nameof(HotfixStateTown)} already exited, ignore scene load callback.");
				return;
			}

			if (instance == null || instance.Status != EOperationStatus.Succeed)
			{
				HotfixLogger.Error($"Failed to load town scene : {instance?.LastError}");
				UIManager.Instance.CloseWindow(EWindowType.UILoading);
				return;
			}

			_initWorld = true;
			_gameWorld.Init();
		}
	}
}
