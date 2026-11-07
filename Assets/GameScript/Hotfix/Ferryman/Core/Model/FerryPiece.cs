
namespace Hotfix
{
	/// <summary>
	/// 运行时棋子（逻辑层，不含任何坐标）
	/// </summary>
	public class FerryPiece
	{
		/// <summary>配置</summary>
		public CfgPieceTab Dto;

		/// <summary>所在码头索引；FerryDefine.DockOnBoat 表示在船上</summary>
		public int Dock;

		/// <summary>已被吃掉</summary>
		public bool Eaten;

		/// <summary>连击模式下已被遣散结算</summary>
		public bool Dismissed;

		/// <summary>初始顺序，用于稳定布局</summary>
		public int Order;

		public bool Alive { get { return Eaten == false && Dismissed == false; } }
		public bool OnBoat { get { return Dock < 0 && Alive; } }

		/// <summary>占船格数</summary>
		public int BoatSpace { get { return Dto.BoatSpace < 1 ? 1 : Dto.BoatSpace; } }

		public FerryPiece(CfgPieceTab dto, int dock, int order)
		{
			Dto = dto;
			Dock = dock;
			Order = order;
		}
	}
}
