using MotionFramework.Config;

namespace Hotfix
{
	/// <summary>
	/// 注册导表工具生成的配表
	/// 这些配表由 CsExporter 直接生成 C# 代码（无需二进制资源），
	/// 统一交由 ConfigManager 管理
	/// </summary>
	public static class HotfixConfigs
	{
		public static void RegisterAll()
		{
			ConfigManager.Instance.Register(CfgAutoGenerateLanguage.Instance);
			ConfigManager.Instance.Register(CfgLevel.Instance);
			ConfigManager.Instance.Register(CfgLevelMap.Instance);
			ConfigManager.Instance.Register(CfgLevelPiece.Instance);
			ConfigManager.Instance.Register(CfgPiece.Instance);
			ConfigManager.Instance.Register(CfgPieceRule.Instance);
			ConfigManager.Instance.Register(CfgTutorialStep.Instance);
		}
	}
}
