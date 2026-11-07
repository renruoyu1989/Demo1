using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace Hotfix
{
	public class UIManager
	{
		public static readonly UIManager Instance = new UIManager();

		private readonly Dictionary<EWindowType, Type> _types = new Dictionary<EWindowType, Type>();
		private readonly List<UIWindow> _stack = new List<UIWindow>(100);
		private AssetHandle _handle;
		private GameObject _uiRoot;
		private GameObject _uiDesktop;
		private Camera _uiCamera;


		private UIManager()
		{
		}
		public void Start()
		{
			// 收集所有窗口类型
			CollectTypes();

			// 创建UIRoot
			CreateUIRoot();
		}
		public void Update()
		{
			// 快照遍历：窗口在 Update 期间可能打开/关闭窗口，直接遍历原列表会索引错位
			UIWindow[] snapshot = _stack.ToArray();
			for (int i = 0; i < snapshot.Length; i++)
			{
				UIWindow window = snapshot[i];
				if (window == null || _stack.Contains(window) == false)
					continue;
				if (window.IsPrepare && window.IsOpen)
					window.InternalUpdate();
			}
		}
		public void Destroy()
		{
			DestroyUIRoot();

			// 销毁所有窗口
			for (int i = 0; i < _stack.Count; i++)
			{
				UIWindow window = _stack[i];
				window.InternalDestroy();
			}
			_stack.Clear();
		}

		/// <summary>
		/// 获取UI相机
		/// </summary>
		public Camera GetUICamera()
		{
			return _uiCamera;
		}

		/// <summary>
		/// 获取UI桌面对象
		/// </summary>
		public GameObject GetUIDesktop()
		{
			return _uiDesktop;
		}

		/// <summary>
		/// 检测UIRoot是否准备完毕
		/// </summary>
		public bool IsPrepareUIRoot()
		{
			return _handle != null && _handle.Status == EOperationStatus.Succeed;
		}

		/// <summary>
		/// 预加载窗口
		/// </summary>
		public void PreloadWindow(EWindowType type)
		{
			// 如果窗口已经存在
			if (IsContains(type))
				return;

			UIWindow window = CreateWindowClass(type);
			Push(window);
			window.InternalClose();
			window.InternalLoad(OnWindowPrepare);
			HotfixLogger.Log($"Preload window {type}");
		}

		/// <summary>
		/// 是否有窗口正在加载
		/// </summary>
		public bool IsLoadingWindow()
		{
			for (int i = 0; i < _stack.Count; i++)
			{
				UIWindow window = _stack[i];
				if (window.IsDone == false)
					return true;
			}
			return false;
		}

		/// <summary>
		/// 打开窗口
		/// </summary>
		/// <param name="type">窗口类型</param>
		/// <param name="userData">用户数据</param>
		public void OpenWindow(EWindowType type, System.Object userData = null)
		{
			UIWindow window;

			// 如果窗口已经存在
			if (IsContains(type))
			{
				window = GetWindow(type);
				Pop(window); //弹出旧窗口
			}
			else
			{
				// 创建窗口类
				window = CreateWindowClass(type);
			}

			Push(window);
			window.InternalOpen(userData);
			window.InternalLoad(OnWindowPrepare);
			HotfixLogger.Log($"Open window {type}");
		}

		/// <summary>
		/// 窗口资源加载失败时的兜底处理
		/// </summary>
		private void OnWindowLoadFailed(UIWindow window)
		{
			HotfixLogger.Error($"Window load failed, remove it from stack : {window.WindowType}");
			Pop(window);
			window.InternalDestroy();
		}

		/// <summary>
		/// 关闭窗口
		/// </summary>
		/// <param name="type">窗口类型</param>
		public void CloseWindow(EWindowType type)
		{
			UIWindow window = GetWindow(type);
			if (window != null)
			{
				if (window.DontDestroy)
				{
					window.InternalClose();
				}
				else
				{
					window.InternalDestroy();
					Pop(window);
					OnSortWindowDepth(window.WindowLayer);
					OnSetWindowVisible(window.WindowLayer);
				}
				HotfixLogger.Log($"Close window {type}");
			}
		}

		/// <summary>
		/// 关闭所有窗口（常驻窗口除外）
		/// </summary>
		public void CloseAll()
		{
			List<UIWindow> tempList = new List<UIWindow>();
			for (int i = 0; i < _stack.Count; i++)
			{
				UIWindow window = _stack[i];
				if (window.DontDestroy == false)
					tempList.Add(window);
			}

			for (int i = 0; i < tempList.Count; i++)
			{
				UIWindow window = tempList[i];
				CloseWindow(window.WindowType);
			}
		}

		// UIRoot相关
		private void CreateUIRoot()
		{
			if (_handle == null)
			{
				_handle = YooAssets.LoadAssetAsync<GameObject>($"{GameDefine.AssetRootPath}/UIPanel/UIRoot");
				_handle.Completed += Handle_Completed;
			}
		}
		private void DestroyUIRoot()
		{
			if (_handle != null)
			{
				_handle.Release();
				_handle = null;
			}

			if (_uiRoot != null)
			{
				GameObject.Destroy(_uiRoot);
				_uiRoot = null;
			}
		}
		private void Handle_Completed(AssetHandle obj)
		{
			if (obj.Status != EOperationStatus.Succeed || _handle == null || _handle.AssetObject == null)
			{
				HotfixLogger.Error($"加载 UIRoot 失败 : {obj.Status} {obj.LastError}");
				if (_handle != null)
				{
					_handle.Release();
					_handle = null;
				}
				return;
			}

			_uiRoot = _handle.InstantiateSync();
			if (_uiRoot == null)
			{
				HotfixLogger.Error("实例化 UIRoot 失败");
				_handle.Release();
				_handle = null;
				return;
			}

			// 节点查找失败时给出明确错误，而不是直接空引用
			Transform desktop = _uiRoot.transform.BFSearch("UIDesktop");
			Transform cameraTrans = _uiRoot.transform.BFSearch("UICamera");
			if (desktop == null || cameraTrans == null)
			{
				HotfixLogger.Error("UIRoot 预制体缺少 UIDesktop 或 UICamera 节点");
				GameObject.Destroy(_uiRoot);
				_uiRoot = null;
				_handle.Release();
				_handle = null;
				return;
			}

			_uiDesktop = desktop.gameObject;
			_uiCamera = cameraTrans.GetComponent<Camera>();
			GameObject.DontDestroyOnLoad(_uiRoot);
		}

		// 准备完毕
		private void OnWindowPrepare(UIWindow window)
		{
			// 资源加载失败：出栈并回收，避免留下僵尸窗口
			if (window.IsFailed)
			{
				OnWindowLoadFailed(window);
				return;
			}

			// 把内置字体替换成支持中文的字体
			UIFontProvider.ApplyFont(window.Go);

			window.OnRefresh();
			OnSortWindowDepth(window.WindowLayer);
			OnSetWindowVisible(window.WindowLayer);			
		}
		// 排序深度
		private void OnSortWindowDepth(EWindowLayer layer)
		{
			int depth = (int)layer;
			for (int i = 0; i < _stack.Count; i++)
			{
				if (_stack[i].WindowLayer == layer)
				{
					_stack[i].Depth = depth;
					depth += 100; //注意：每次递增100深度
				}
			}
		}
		// 刷新可见性
		private void OnSetWindowVisible(EWindowLayer layer)
		{
			bool isHideNext = false;
			for (int i = _stack.Count - 1; i >= 0; i--)
			{
				UIWindow window = _stack[i];
				if (window.WindowLayer == layer)
				{
					if (isHideNext == false)
					{
						window.IsVisible = true;
						if (window.IsPrepare && window.IsOpen && window.FullScreen)
							isHideNext = true;
					}
					else
					{
						window.IsVisible = false;
					}
				}
			}
		}

		private UIWindow GetWindow(EWindowType type)
		{
			for (int i = 0; i < _stack.Count; i++)
			{
				UIWindow window = _stack[i];
				if (window.WindowType == type)
					return window;
			}
			return null;
		}
		private bool IsTopWindow(EWindowType type, EWindowLayer layer)
		{
			UIWindow lastOne = null;
			for (int i = 0; i < _stack.Count; i++)
			{
				if (_stack[i].WindowLayer == layer)
					lastOne = _stack[i];
			}

			if (lastOne == null)
				return false;

			return lastOne.WindowType == type;
		}
		private bool IsContains(EWindowType type)
		{
			for (int i = 0; i < _stack.Count; i++)
			{
				UIWindow window = _stack[i];
				if (window.WindowType == type)
					return true;
			}
			return false;
		}
		private void Push(UIWindow window)
		{
			// 如果已经存在
			if (IsContains(window.WindowType))
				throw new System.Exception($"Window {window.WindowType} is exist.");

			// 获取插入到所属层级的位置
			int insertIndex = -1;
			for (int i = 0; i < _stack.Count; i++)
			{
				if (window.WindowLayer == _stack[i].WindowLayer)
					insertIndex = i + 1;
			}

			// 如果没有所属层级，找到相邻层级
			if (insertIndex == -1)
			{
				for (int i = 0; i < _stack.Count; i++)
				{
					if (window.WindowLayer > _stack[i].WindowLayer)
						insertIndex = i + 1;
				}
			}

			// 如果是空栈或没有找到插入位置
			if (insertIndex == -1)
			{
				insertIndex = 0;
			}

			// 最后插入到堆栈
			_stack.Insert(insertIndex, window);
		}
		private void Pop(UIWindow window)
		{
			// 从堆栈里移除
			_stack.Remove(window);
		}

		private void CollectTypes()
		{
			Type[] types = HotfixTypeHelper.GetAllTypes();
			for (int i = 0; i < types.Length; i++)
			{
				System.Type type = types[i];

				// 只处理窗口类型
				if (type.IsAbstract || typeof(UIWindow).IsAssignableFrom(type) == false)
					continue;

				// 判断属性标签
				if (Attribute.IsDefined(type, typeof(WindowAttribute)))
				{
					// 判断是否重复
					WindowAttribute attribute = HotfixTypeHelper.GetAttribute<WindowAttribute>(type);
					if (_types.ContainsKey(attribute.WindowType))
						throw new Exception($"Window type {attribute.WindowType} already exist.");

					// 添加到集合
					_types.Add(attribute.WindowType, type);
				}
			}
		}
		private UIWindow CreateWindowClass(EWindowType windowType)
		{
			UIWindow window = null;
			Type type;
			if (_types.TryGetValue(windowType, out type))
			{
				// 注意：ILR下通过CreateInstance传递构造函数的参数无效
				WindowAttribute attribute = HotfixTypeHelper.GetAttribute<WindowAttribute>(type);
				window = (UIWindow)Activator.CreateInstance(type);
				window.Init(attribute.WindowType, attribute.WindowLayer);
			}

			if (window == null)
				throw new KeyNotFoundException($"{nameof(UIWindow)} {windowType} is not define.");

			return window;
		}
	}
}