
public class GameDefine
{
	/// <summary>
	/// 运行时资源根目录（YooAsset 使用资源全路径作为定位地址）
	/// </summary>
	public const string AssetRootPath = "Assets/GameRes";
}

/// <summary>
/// 资源系统的运行模式
/// </summary>
public enum EAssetSystemMode
{
	/// <summary>
	/// 编辑器下模拟运行（直接读工程内资源）
	/// </summary>
	Resources = 0,

	/// <summary>
	/// 从AssetBundle加载
	/// </summary>
	AssetBundle = 1,
}
