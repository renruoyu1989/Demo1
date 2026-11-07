using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>
	/// 把关卡配置 + 场景布局组装成逻辑层可以直接跑的运行时数据
	/// </summary>
	public static class FerryLevelLoader
	{
		public static FerryLevelRuntime Build(int levelId)
		{
			FerryConfigService config = FerryConfigService.Instance;
			CfgLevelTab level = config.GetLevel(levelId);
			if (level == null)
			{
				HotfixLogger.Error($"FerryLevelLoader level not found : {levelId}");
				return null;
			}

			FerryLevelRuntime runtime = new FerryLevelRuntime();
			runtime.Dto = level;

			List<FerryDockLayout> layouts = config.BuildDockLayouts(level);
			for (int i = 0; i < layouts.Count; i++)
			{
				runtime.Docks.Add(new FerryDock(layouts[i].Index, layouts[i].Name, layouts[i].SlotCount));
			}

			runtime.Pieces = config.GetLevelPieces(levelId);
			return runtime;
		}
	}
}
