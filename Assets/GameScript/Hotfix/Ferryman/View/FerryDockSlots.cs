using System.Collections.Generic;
using UnityEngine;

namespace Hotfix
{
	/// <summary>
	/// 码头站位表。算法逐条对齐原版 BoardView 的 BuildDockSlots / SlotXImg / BuildSideSlots：
	/// - 站位表每个码头只算一次，棋子离开后剩下的动物不会跟着挪位置（原版动物站在原地不动）；
	/// - 夹在两条河之间的码头，整组动物站到船停靠那一侧的半区；
	/// - 终点码头第一个站位右移、列距收紧，并按棋子总数一次给足槽位；
	/// - 槽位不够时按最大列距均分，超出表尾按最大列距向右延伸并夹在码头范围内。
	/// 坐标一律是设计稿图像坐标（X 向右、Y 向下）。
	/// </summary>
	public class FerryDockSlots
	{
		private readonly FerryGameSession _session;
		private readonly List<FerryDockLayout> _layouts;

		/// <summary>每个码头算好的站位 X 表</summary>
		private readonly Dictionary<int, List<float>> _slots = new Dictionary<int, List<float>>();

		/// <summary>中间码头构建时用的停靠侧（+1 右 / -1 左），构建后不再改</summary>
		private readonly Dictionary<int, bool> _sideRight = new Dictionary<int, bool>();

		/// <summary>码头已分配的槽位游标（新补员的动物依次往后排）</summary>
		private readonly Dictionary<int, int> _cursors = new Dictionary<int, int>();

		/// <summary>棋子的固定槽位号，进关时分配一次</summary>
		private readonly Dictionary<FerryPiece, int> _pieceSlot = new Dictionary<FerryPiece, int>();

		public FerryDockSlots(FerryGameSession session, List<FerryDockLayout> layouts)
		{
			_session = session;
			_layouts = layouts;
		}

		#region 槽位号分配
		/// <summary>给棋子分配固定槽位号（原版 _slots + _slotCursor）</summary>
		public void AssignSlot(FerryPiece piece)
		{
			if (piece == null || piece.Dock < 0)
				return;

			int slot;
			if (_cursors.TryGetValue(piece.Dock, out slot) == false)
				slot = 0;

			_pieceSlot[piece] = slot;
			_cursors[piece.Dock] = slot + 1;
		}

		/// <summary>取棋子的固定槽位号，没有就退回给定值</summary>
		public int SlotOf(FerryPiece piece, int fallback)
		{
			int slot;
			return _pieceSlot.TryGetValue(piece, out slot) ? slot : fallback;
		}

		public void Clear()
		{
			_slots.Clear();
			_sideRight.Clear();
			_cursors.Clear();
			_pieceSlot.Clear();
		}
		#endregion

		#region 站位表
		/// <summary>码头是否夹在两条河之间（只有这种码头要按停靠侧安排站位）</summary>
		public bool IsTwoRiverDock(int dockIndex)
		{
			return _session != null && dockIndex > 0 && dockIndex < _session.Docks.Count - 1;
		}

		/// <summary>
		/// 确保该码头有一份够用的站位表。表只增不减：人数变少时沿用原表，动物不会滑动。
		/// </summary>
		public void Ensure(int dockIndex, int count)
		{
			List<float> xs;
			if (_slots.TryGetValue(dockIndex, out xs) == false)
			{
				_slots[dockIndex] = Build(dockIndex, count);
				return;
			}

			// 中间码头的站位是按人数排的半区，人多到表不够时才重排
			if (IsTwoRiverDock(dockIndex) && count > xs.Count)
				_slots[dockIndex] = Build(dockIndex, count);
		}

		private List<float> Build(int dockIndex, int count)
		{
			List<float> xs = new List<float>();
			count = Mathf.Max(0, count);

			if (IsTwoRiverDock(dockIndex))
			{
				BuildSideSlots(dockIndex, Mathf.Max(1, count), xs);
				return xs;
			}

			FerryDockLayout dock = Layout(dockIndex);
			for (int i = 0; i < dock.SlotXs.Count; i++)
				xs.Add(dock.SlotXs[i]);

			if (xs.Count == 0)
				xs.Add(dock.CenterX);

			if (count > xs.Count)
			{
				// 槽位不够：在原有范围内按最大列距均分
				float min = xs[0];
				float max = xs[xs.Count - 1];
				float step = Mathf.Min(FerryDefine.DockSlotMaxStep, (max - min) / Mathf.Max(1, count - 1));
				xs.Clear();
				for (int i = 0; i < count; i++)
					xs.Add(min + step * i);
			}

			// 对岸（终点码头）站位单独调：第一站右移、列距收紧，一次给足所有棋子的位置
			if (dockIndex == _session.Docks.Count - 1)
			{
				float start = xs[0] + FerryDefine.FarDockFirstShiftX;
				int total = Mathf.Max(count, _session.Pieces.Count);
				xs.Clear();
				for (int i = 0; i < total; i++)
					xs.Add(start + FerryDefine.FarDockStep * i);
			}

			return xs;
		}

		/// <summary>
		/// 中间码头的站位：以码头贴图中心为界，把整组动物排进左半区或右半区（谁在这侧河里停靠就靠哪边站）。
		/// </summary>
		private void BuildSideSlots(int dockIndex, int count, List<float> xs)
		{
			FerryDockLayout dock = Layout(dockIndex);
			float half = dock.HalfWidth > 1f ? dock.HalfWidth : 300f;

			float step = FerryDefine.DockSlotMaxStep;
			if (dock.SlotXs.Count > 1)
				step = Mathf.Abs(dock.SlotXs[1] - dock.SlotXs[0]);
			step = Mathf.Clamp(step, FerryDefine.DockSlotMaxStep * 0.6f, FerryDefine.DockSlotMaxStep);

			bool right = SideRight(dockIndex);
			_sideRight[dockIndex] = right;

			float span = step * Mathf.Max(0, count - 1);
			float anchor = dock.CenterX + (right ? 1f : -1f) * half * FerryDefine.DockSideSlotOffset;
			float lo = dock.CenterX - half * 0.88f + span * 0.5f;
			float hi = dock.CenterX + half * 0.88f - span * 0.5f;
			anchor = hi >= lo ? Mathf.Clamp(anchor, lo, hi) : dock.CenterX;

			float start = anchor - span * 0.5f;
			for (int i = 0; i < count; i++)
				xs.Add(start + step * i);
		}

		/// <summary>该码头的动物应该站在哪一侧：船停在哪一侧就靠哪一侧（离开后沿用上次结果）</summary>
		private bool SideRight(int dockIndex)
		{
			if (_session != null && dockIndex == _session.BoatDock)
				return _session.BoatSide > 0;

			bool right;
			if (_sideRight.TryGetValue(dockIndex, out right))
				return right;
			return true;
		}
		#endregion

		#region 取坐标
		/// <summary>第 slot 个站位的图像 X</summary>
		public float SlotX(int dockIndex, int slot)
		{
			List<float> xs;
			if (_slots.TryGetValue(dockIndex, out xs) == false || xs.Count == 0)
				return Layout(dockIndex).CenterX;

			if (slot < 0)
				slot = 0;
			if (slot < xs.Count)
				return xs[slot];

			// 表用完了：按最大列距往右延伸，但不能站到码头外面去
			float min, max;
			SlotRange(dockIndex, out min, out max);
			float x = xs[xs.Count - 1] + FerryDefine.DockSlotMaxStep * (slot - xs.Count + 1);
			return Mathf.Clamp(x, min, max);
		}

		/// <summary>码头可以站人的图像 X 范围（留 20% 余量，别站到水面上）</summary>
		private void SlotRange(int dockIndex, out float min, out float max)
		{
			FerryDockLayout dock = Layout(dockIndex);
			float half = dock.HalfWidth > 1f ? dock.HalfWidth : 260f;
			min = dock.CenterX - half * 0.8f;
			max = dock.CenterX + half * 0.8f;
		}

		private FerryDockLayout Layout(int dockIndex)
		{
			if (_layouts == null || _layouts.Count == 0)
				return new FerryDockLayout { CenterX = FerryDefine.DesignWidth * 0.5f, DeckY = 437f };

			if (dockIndex < 0)
				dockIndex = 0;
			if (dockIndex > _layouts.Count - 1)
				dockIndex = _layouts.Count - 1;
			return _layouts[dockIndex];
		}
		#endregion
	}
}
