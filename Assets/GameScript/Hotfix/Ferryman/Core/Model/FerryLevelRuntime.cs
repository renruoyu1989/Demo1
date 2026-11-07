using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>
	/// 一关的运行时数据（配置 + 由场景推导出的码头结构）
	/// </summary>
	public class FerryLevelRuntime
	{
		/// <summary>关卡配置</summary>
		public CfgLevelTab Dto;

		/// <summary>码头结构</summary>
		public List<FerryDock> Docks = new List<FerryDock>();

		/// <summary>初始布阵的棋子模板</summary>
		public List<CfgPieceTab> Pieces = new List<CfgPieceTab>();

		public string Background { get { return Dto.Background; } }
		public int Kind { get { return Dto.Kind; } }
		public bool IsComboMode { get { return Dto.Kind == FerryDefine.KindTutorial || Dto.Waiting.Count > 0; } }
	}
}
