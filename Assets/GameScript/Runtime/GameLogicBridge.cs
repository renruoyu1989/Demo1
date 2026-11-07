using System;

/// <summary>
/// 游戏业务逻辑接口
/// 业务逻辑层（Hotfix程序集）实现该接口，并在启动时注册到 GameLogicBridge
/// </summary>
public interface IGameLogic
{
	/// <summary>
	/// 启动游戏逻辑
	/// </summary>
	void Start();

	/// <summary>
	/// 每帧轮询
	/// </summary>
	void Update();

	/// <summary>
	/// 每帧晚于Update轮询
	/// </summary>
	void LateUpdate();
}

/// <summary>
/// 游戏业务逻辑桥接器
/// 注意：把业务逻辑层和启动层解耦，启动层只依赖接口
/// </summary>
public static class GameLogicBridge
{
	public static IGameLogic Logic { get; private set; }

	public static bool IsRegistered
	{
		get { return Logic != null; }
	}

	public static void Register(IGameLogic logic)
	{
		if (logic == null)
			throw new ArgumentNullException(nameof(logic));
		Logic = logic;
	}

	public static void Unregister()
	{
		Logic = null;
	}
}
