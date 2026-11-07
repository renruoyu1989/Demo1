using System;
using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>
	/// 一局游戏的纯逻辑层：码头、棋子、船、吃子规则、胜负判定、计分。
	/// 不引用 UnityEngine，不做任何渲染与坐标换算。
	/// 行为契约来自原版逆向文档 docs/re/05-unity-porting.md 的五条约束。
	/// </summary>
	public class FerryGameSession
	{
		/// <summary>本关数据</summary>
		public FerryLevelRuntime Level { get; private set; }

		/// <summary>码头</summary>
		public List<FerryDock> Docks { get; private set; }

		/// <summary>全部棋子</summary>
		public List<FerryPiece> Pieces { get; private set; }

		/// <summary>船上的棋子</summary>
		public List<FerryPiece> Boat { get; private set; }

		/// <summary>船当前停靠的码头</summary>
		public int BoatDock { get; private set; }

		/// <summary>船停在码头的哪一侧：+1 右侧，-1 左侧</summary>
		public int BoatSide { get; private set; }

		/// <summary>船容量</summary>
		public int BoatCapacity { get; private set; }

		/// <summary>是否结束</summary>
		public bool IsOver { get; private set; }

		/// <summary>是否胜利</summary>
		public bool IsWin { get; private set; }

		/// <summary>渡河次数</summary>
		public int MoveCount { get; private set; }

		/// <summary>被吃数量</summary>
		public int EatenCount { get; private set; }

		/// <summary>关键单位被吃数量</summary>
		public int EssentialEaten { get; private set; }

		/// <summary>上一次 Row 是否只是「上岸搬船」到码头另一侧（没有渡河）</summary>
		public bool LastMoveShore { get; private set; }

		/// <summary>本关用时（秒）</summary>
		public float ElapsedSeconds { get; private set; }

		/// <summary>连击模式</summary>
		public bool Survival { get; private set; }

		/// <summary>等待入场的棋子模板队列</summary>
		public List<int> Queue { get; private set; }

		/// <summary>还剩几次渡河触发补员</summary>
		public int FerryCount { get; private set; }

		/// <summary>遣散所需数量</summary>
		public int DismissNeed { get; private set; }

		/// <summary>本次结算的连击数</summary>
		public int Combo { get; private set; }

		/// <summary>总分</summary>
		public int Score { get; private set; }

		/// <summary>提示文本（表现层显示为浮字或 Toast）</summary>
		public event Action<string> Toast;

		/// <summary>状态变化</summary>
		public event Action Changed;

		/// <summary>单个单位结算</summary>
		public event Action<FerryScoreEntry> Scored;

		private bool _scored;
		private int _nextOrder;
		private readonly FerryRuleEvaluator _evaluator;

		public FerryGameSession(FerryLevelRuntime level, FerryRuleEvaluator evaluator)
		{
			Level = level;
			_evaluator = evaluator;
			Docks = level.Docks;
			if (Docks.Count == 0)
				Docks.Add(new FerryDock(0, "Dock", 3));

			Boat = new List<FerryPiece>();
			BoatDock = 0;
			BoatSide = 1;
			BoatCapacity = level.Dto.BoatSpace < 1 ? 1 : level.Dto.BoatSpace;

			Pieces = new List<FerryPiece>();
			for (int i = 0; i < level.Pieces.Count; i++)
			{
				Pieces.Add(new FerryPiece(level.Pieces[i], 0, i));
			}
			_nextOrder = Pieces.Count;

			Queue = new List<int>();
			List<int> waiting = level.Dto.Waiting;
			for (int i = 0; i < waiting.Count; i++)
			{
				if (waiting[i] > 0)
					Queue.Add(waiting[i]);
			}

			Survival = Queue.Count > 0 && level.Dto.RespawnTime > 0;
			FerryCount = level.Dto.RespawnTime < 1 ? 1 : level.Dto.RespawnTime;
			DismissNeed = level.Dto.DismissCount < 1 ? 1 : level.Dto.DismissCount;

			if (Pieces.Count == 0)
			{
				IsOver = true;
				IsWin = true;
			}
		}

		/// <summary>当前码头</summary>
		public FerryDock CurrentDock
		{
			get
			{
				int index = BoatDock;
				if (index < 0) index = 0;
				if (index > Docks.Count - 1) index = Docks.Count - 1;
				return Docks[index];
			}
		}

		/// <summary>终点码头索引</summary>
		public int LastDockIndex { get { return Docks.Count - 1; } }

		/// <summary>船已用空间</summary>
		public int BoatUsedSpace
		{
			get
			{
				int n = 0;
				for (int i = 0; i < Boat.Count; i++)
				{
					n += Boat[i].BoatSpace;
				}
				return n;
			}
		}

		/// <summary>已被送到终点（含已遣散）的数量</summary>
		public int Ferried
		{
			get
			{
				int n = 0;
				for (int i = 0; i < Pieces.Count; i++)
				{
					FerryPiece p = Pieces[i];
					if (p.Eaten) continue;
					if (p.Dismissed || p.Dock == LastDockIndex) n++;
				}
				return n;
			}
		}

		/// <summary>是否可以遣散</summary>
		public bool CanDismiss { get { return Survival && IsOver == false && Ferried >= DismissNeed; } }

		/// <summary>还需要运到终点的关键单位数量</summary>
		public int EssentialRemaining
		{
			get
			{
				int n = 0;
				for (int i = 0; i < Pieces.Count; i++)
				{
					FerryPiece p = Pieces[i];
					if (p.Dto.Essential == 1 && p.Alive && p.Dock != LastDockIndex) n++;
				}
				return n;
			}
		}

		#region 操作
		public bool CanBoard(FerryPiece p)
		{
			if (IsOver || p == null || p.Alive == false) return false;
			if (p.Dto.Moveable == 0) return false;
			if (p.Dock != BoatDock) return false;
			return BoatUsedSpace + p.BoatSpace <= BoatCapacity;
		}

		public bool Board(FerryPiece p)
		{
			if (CanBoard(p) == false)
			{
				if (p != null && p.Dock == BoatDock && p.Dto.Moveable == 1 && BoatUsedSpace >= BoatCapacity)
					Raise("The Boat is Full. Unload Something First.");
				return false;
			}

			p.Dock = FerryDefine.DockOnBoat;
			Boat.Add(p);
			Raise(p.Dto.Name + " boards the boat");
			Notify();
			return true;
		}

		public bool CanUnload(FerryPiece p)
		{
			return IsOver == false && p != null && p.Alive && p.OnBoat && Boat.Contains(p);
		}

		public bool Unload(FerryPiece p)
		{
			if (CanUnload(p) == false) return false;
			Boat.Remove(p);
			p.Dock = BoatDock;
			Raise(p.Dto.Name + " steps onto the dock");

			// 最后一只下船就该判胜，不能逼玩家再空划一次船
			CheckWinState();
			Notify();
			return true;
		}

		/// <summary>
		/// 推进一次渡河流程（dir = +1 / -1）。
		/// 船不在目标侧河面时，先把船沿码头搬到那一侧（岸上行走），下一次才真正横渡。
		/// </summary>
		public bool Row(int dir)
		{
			if (IsOver) return false;

			int step = dir >= 0 ? 1 : -1;
			LastMoveShore = false;

			int target = BoatDock + step;
			if (target < 0 || target >= Docks.Count)
			{
				Raise("No dock in that direction");
				return false;
			}

			int needSide = step > 0 ? 1 : -1;
			if (BoatSide != needSide)
			{
				if (Boat.Count > 0)
				{
					Raise("The Ferryman cannot carry the boat while passengers are aboard");
					return false;
				}

				BoatSide = needSide;
				LastMoveShore = true;
				Raise("The Ferryman carries the boat along the dock");
				Notify();
				return true;
			}

			int departed = BoatDock;
			BoatDock = target;
			BoatSide = -step;
			MoveCount++;
			Raise("Rowing to " + CurrentDock.Name + "...");
			Notify();

			ResolveEats(departed);
			if (IsOver == false) TickSpawn();
			if (IsOver == false) CheckWin();
			Notify();
			return true;
		}

		/// <summary>每渡一次河 Ferry Count 减一，减到 0 就从队列补员到首码头</summary>
		public void TickSpawn()
		{
			if (Survival == false || Queue.Count == 0) return;

			FerryCount--;
			if (FerryCount > 0)
			{
				Raise("New animals arrive in " + FerryCount);
				return;
			}

			int n = Level.Dto.SpawnCount < 1 ? 1 : Level.Dto.SpawnCount;
			int spawned = 0;
			for (int i = 0; i < n && Queue.Count > 0; i++)
			{
				int key = Queue[0];
				Queue.RemoveAt(0);

				CfgPieceTab dto = CfgPiece.Instance.GetCfgTab(key);
				if (dto == null) continue;

				Pieces.Add(new FerryPiece(dto, 0, _nextOrder));
				_nextOrder++;
				spawned++;
			}

			FerryCount = Level.Dto.RespawnTime < 1 ? 1 : Level.Dto.RespawnTime;
			if (spawned > 0) Raise("New animals arrive at the Start Dock");
		}

		/// <summary>把终点码头上数量最多的同族一次性遣散结算</summary>
		public bool Dismiss()
		{
			if (CanDismiss == false)
			{
				Raise("Ferry " + DismissNeed + " animals before dismissing");
				return false;
			}

			List<FerryPiece> atEnd = PiecesOn(LastDockIndex);
			if (atEnd.Count == 0)
			{
				Raise("No animals to dismiss");
				return false;
			}

			string species = null;
			int best = 0;
			for (int i = 0; i < atEnd.Count; i++)
			{
				int count = 0;
				for (int j = 0; j < atEnd.Count; j++)
				{
					if (atEnd[j].Dto.Code == atEnd[i].Dto.Code) count++;
				}
				if (count > best)
				{
					best = count;
					species = atEnd[i].Dto.Code;
				}
			}

			int dismissed = 0;
			for (int i = 0; i < atEnd.Count; i++)
			{
				if (atEnd[i].Dto.Code != species) continue;
				atEnd[i].Dismissed = true;
				dismissed++;
			}

			Combo = dismissed;
			for (int i = 0; i < atEnd.Count; i++)
			{
				FerryPiece p = atEnd[i];
				if (p.Dto.Code != species) continue;
				int value = dismissed * FerryDefine.ScorePerPiece;
				Score += value;
				if (Scored != null)
				{
					FerryScoreEntry entry = new FerryScoreEntry();
					entry.Piece = p;
					entry.Value = value;
					entry.SameCount = dismissed;
					Scored(entry);
				}
			}

			Raise(string.Format("Dismissed {0} x{1}  +{2}", dismissed, species, dismissed * dismissed * FerryDefine.ScorePerPiece));
			CheckWinState();
			Notify();
			return true;
		}

		/// <summary>累加本关用时；结算后停表</summary>
		public void Tick(float deltaTime)
		{
			if (IsOver || deltaTime <= 0f) return;
			ElapsedSeconds += deltaTime;
		}

		/// <summary>用时文本 "mm : ss"</summary>
		public string ElapsedText()
		{
			int total = (int)ElapsedSeconds;
			return string.Format("{0:00} : {1:00}", total / 60, total % 60);
		}
		#endregion

		#region 吃子结算
		/// <summary>船离开后结算被留下码头的吃子</summary>
		public void ResolveEats(int dockIndex)
		{
			List<FerryPiece> residents = PiecesOn(dockIndex);
			if (residents.Count == 0) return;

			// 守卫犬在场时其它动物都不敢动口，只有守卫犬自己会咬猫
			FerryPiece guard = FindBySpecies(residents, FerryDefine.SpeciesGuard);
			if (guard != null)
			{
				FerryPiece cat = null;
				for (int i = 0; i < residents.Count; i++)
				{
					if (residents[i] != guard && residents[i].Dto.Species == FerryDefine.SpeciesCat)
					{
						cat = residents[i];
						break;
					}
				}
				if (cat != null) Eat(guard, cat);
				return;
			}

			// 码头上有奶酪时，动物会先吃奶酪
			// 蓝奶酪 essential=1，存在普通奶酪时优先吃普通奶酪
			FerryPiece cheese = FindCheese(residents, false);
			if (cheese == null) cheese = FindCheese(residents, true);
			if (cheese != null)
			{
				FerryPiece nibbler = FindFirstEater(residents);
				if (nibbler != null)
				{
					Eat(nibbler, cheese);
					return;
				}
			}

			for (int i = 0; i < residents.Count && IsOver == false; i++)
			{
				FerryPiece predator = residents[i];
				if (predator.Alive == false || predator.Dto.CanEat == 0) continue;

				FerryPiece victim = _evaluator.PickVictim(predator, residents);
				if (victim != null) Eat(predator, victim);
			}
		}

		private static FerryPiece FindBySpecies(List<FerryPiece> residents, int species)
		{
			for (int i = 0; i < residents.Count; i++)
			{
				if (residents[i].Alive && residents[i].Dto.Species == species)
					return residents[i];
			}
			return null;
		}

		private static FerryPiece FindCheese(List<FerryPiece> residents, bool essential)
		{
			for (int i = 0; i < residents.Count; i++)
			{
				FerryPiece p = residents[i];
				if (p.Alive == false || p.Dto.Species != FerryDefine.SpeciesCheese) continue;
				bool isEssential = p.Dto.Essential == 1;
				if (isEssential == essential) return p;
			}
			return null;
		}

		private FerryPiece FindFirstEater(List<FerryPiece> residents)
		{
			for (int i = 0; i < residents.Count; i++)
			{
				FerryPiece p = residents[i];
				if (p.Alive == false || p.Dto.CanEat == 0) continue;
				if (p.Dto.Species == FerryDefine.SpeciesCheese) continue;
				if (_evaluator.HasAnyPrey(p, residents)) return p;
			}
			return null;
		}

		private void Eat(FerryPiece predator, FerryPiece victim)
		{
			victim.Eaten = true;
			EatenCount++;
			Raise(predator.Dto.Name + " eats " + victim.Dto.Name + "!");

			// 只有 essential 动物出事才算失败
			if (victim.Dto.Essential == 1)
			{
				EssentialEaten++;
				IsOver = true;
				IsWin = false;
			}
		}
		#endregion

		#region 胜负与计分
		/// <summary>检查通关</summary>
		public void CheckWin()
		{
			CheckWinState();
			Notify();
		}

		private void CheckWinState()
		{
			if (IsOver) return;
			if (Survival && Queue.Count > 0) return;

			int total = 0;
			int waiting = 0;
			for (int i = 0; i < Pieces.Count; i++)
			{
				FerryPiece p = Pieces[i];
				if (p.Eaten) continue;
				total++;
				if (p.Dismissed) continue;
				if (p.Dock != LastDockIndex) waiting++;
			}

			if (total == 0 || waiting > 0) return;

			IsOver = true;
			IsWin = true;
			ScoreLevel();
			Raise("LEVEL COMPLETE!");
		}

		/// <summary>关卡结算</summary>
		public void ScoreLevel()
		{
			if (_scored) return;
			_scored = true;
			if (Docks.Count == 0) return;

			List<FerryScoreEntry> entries = FerryScoreCalculator.Calculate(PiecesOn(LastDockIndex));
			Score += FerryScoreCalculator.Sum(entries);

			if (Scored != null)
			{
				for (int i = 0; i < entries.Count; i++)
				{
					Scored(entries[i]);
				}
			}
		}

		/// <summary>取某码头上存活的棋子</summary>
		public List<FerryPiece> PiecesOn(int dockIndex)
		{
			List<FerryPiece> list = new List<FerryPiece>();
			for (int i = 0; i < Pieces.Count; i++)
			{
				FerryPiece p = Pieces[i];
				if (p.Alive && p.Dock == dockIndex)
					list.Add(p);
			}
			return list;
		}
		#endregion

		private void Raise(string msg)
		{
			if (Toast != null)
				Toast(msg);
		}

		private void Notify()
		{
			CheckWinState();
			if (Changed != null)
				Changed();
		}
	}
}
