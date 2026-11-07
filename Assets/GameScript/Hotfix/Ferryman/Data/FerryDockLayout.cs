using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>
	/// 码头布局（表现层坐标，设计稿像素坐标系，原点左下、Y 向上）
	/// 由场景里的 Dock / Dock Hole 物件推导，逻辑层只使用 DockCount 与 SlotCount
	/// </summary>
	public class FerryDockLayout
	{
		public int Index;
		public string Name;
		public float CenterX;
		public float DeckY;

		/// <summary>码头贴图的半宽（表现层回填，用于把船停在岸沿外侧），未知时为 0</summary>
		public float HalfWidth;

		/// <summary>每个槽位的 X 坐标（升序）</summary>
		public List<float> SlotXs = new List<float>();

		public int SlotCount { get { return SlotXs.Count; } }
	}

	/// <summary>
	/// 码头布局推导：Dock 物件决定码头中心与甲板高度，Dock Hole 决定棋子站位
	/// </summary>
	public static class FerryDockLayoutBuilder
	{
		public static List<FerryDockLayout> Build(float canvasWidth, List<FerrySceneObjData> scene)
		{
			List<FerryDockLayout> docks = new List<FerryDockLayout>();
			List<FerrySceneObjData> dockObs = new List<FerrySceneObjData>();
			List<FerrySceneObjData> holes = new List<FerrySceneObjData>();

			if (scene != null)
			{
				for (int i = 0; i < scene.Count; i++)
				{
					FerrySceneObjData o = scene[i];
					if (o.tpl == "Dock")
						dockObs.Add(o);
					else if (o.tpl == "Dock Hole")
						holes.Add(o);
				}
			}

			float w = canvasWidth > 0f ? canvasWidth : FerryDefine.DesignWidth;
			if (dockObs.Count < 2)
			{
				dockObs.Clear();
				dockObs.Add(new FerrySceneObjData { x = w * 0.15f });
				dockObs.Add(new FerrySceneObjData { x = w * 0.85f });
			}

			dockObs.Sort((a, b) => a.x.CompareTo(b.x));
			for (int i = 0; i < dockObs.Count; i++)
			{
				FerryDockLayout dock = new FerryDockLayout();
				dock.Index = i;
				dock.CenterX = dockObs[i].x;
				dock.DeckY = dockObs[i].y;
				dock.Name = i == 0 ? "Start Dock" : (i == dockObs.Count - 1 ? "Far Dock" : "Mid Dock " + i);
				docks.Add(dock);
			}

			// 把每个 Dock Hole 归属到最近的码头
			for (int i = 0; i < holes.Count; i++)
			{
				int best = 0;
				float bestDistance = float.MaxValue;
				for (int d = 0; d < docks.Count; d++)
				{
					float dist = docks[d].CenterX - holes[i].x;
					if (dist < 0f) dist = -dist;
					if (dist < bestDistance)
					{
						bestDistance = dist;
						best = d;
					}
				}
				docks[best].SlotXs.Add(holes[i].x);
			}

			for (int d = 0; d < docks.Count; d++)
			{
				FerryDockLayout dock = docks[d];
				dock.SlotXs.Sort();
				if (dock.SlotXs.Count == 0)
				{
					float step = 96f;
					for (int k = -1; k <= 1; k++)
					{
						dock.SlotXs.Add(dock.CenterX + k * step);
					}
				}
			}

			return docks;
		}
	}
}
