
namespace Hotfix
{
	/// <summary>
	/// 码头（逻辑层）：只有序号和槽位数量，具体像素坐标由表现层根据场景物件推导
	/// </summary>
	public class FerryDock
	{
		public int Index;
		public string Name;

		/// <summary>槽位数量（由场景里的 Dock Hole 数量决定）</summary>
		public int SlotCount;

		public FerryDock(int index, string name, int slotCount)
		{
			Index = index;
			Name = name;
			SlotCount = slotCount < 1 ? 1 : slotCount;
		}
	}
}
