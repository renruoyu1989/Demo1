using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>
	/// 吃子规则求值
	/// 语义对齐原版 .puzz 的「N-P, target, min count, priority」规则表：
	///   eat=1      该种族吃 targetSpecies
	///   eat=0      例外规则（否定规则），minCount&lt;0 且目标数量多于自己时禁止吃
	///   minCount&gt;0 数量条件（例如老虎数量必须多于公牛）
	///   priority   大于 0 的规则先结算
	/// </summary>
	public class FerryRuleEvaluator
	{
		private readonly Dictionary<int, List<CfgPieceRuleTab>> _rules = new Dictionary<int, List<CfgPieceRuleTab>>();

		/// <summary>
		/// 获取某个棋子模板的全部规则
		/// </summary>
		public List<CfgPieceRuleTab> GetRules(int pieceId)
		{
			List<CfgPieceRuleTab> list;
			if (_rules.TryGetValue(pieceId, out list))
				return list;

			list = new List<CfgPieceRuleTab>();
			List<int> keys = CfgPieceRule.Instance.GetKeys();
			for (int i = 0; i < keys.Count; i++)
			{
				CfgPieceRuleTab tab = CfgPieceRule.Instance.GetCfgTab(keys[i]);
				if (tab != null && tab.PieceID == pieceId)
					list.Add(tab);
			}
			_rules.Add(pieceId, list);
			return list;
		}

		/// <summary>
		/// 统计某码头上指定种族存活数量
		/// </summary>
		public int CountAlive(List<FerryPiece> residents, int species)
		{
			int n = 0;
			for (int i = 0; i < residents.Count; i++)
			{
				FerryPiece p = residents[i];
				if (p.Alive && p.Dto.Species == species)
					n++;
			}
			return n;
		}

		/// <summary>
		/// 该掠食者在当前码头上是否有任何猎物
		/// </summary>
		public bool HasAnyPrey(FerryPiece predator, List<FerryPiece> residents)
		{
			List<CfgPieceRuleTab> rules = GetRules(predator.Dto.Id);
			for (int i = 0; i < rules.Count; i++)
			{
				if (rules[i].Eat == 1 && CountAlive(residents, rules[i].TargetSpecies) > 0)
					return true;
			}
			return false;
		}

		/// <summary>
		/// 按优先级挑一个猎物：优先级高的规则先结算，数量条件不满足则跳过
		/// </summary>
		public FerryPiece PickVictim(FerryPiece predator, List<FerryPiece> residents)
		{
			List<CfgPieceRuleTab> rules = GetRules(predator.Dto.Id);
			int predCount = CountAlive(residents, predator.Dto.Species);

			CfgPieceRuleTab best = null;
			FerryPiece bestPrey = null;

			// pass 0：priority > 0 的规则；pass 1：priority <= 0 的规则
			for (int pass = 0; pass < 2; pass++)
			{
				for (int i = 0; i < rules.Count; i++)
				{
					CfgPieceRuleTab rule = rules[i];
					if (rule.Eat != 1)
						continue;
					if (pass == 0 && rule.Priority <= 0f)
						continue;
					if (pass == 1 && rule.Priority > 0f)
						continue;

					FerryPiece prey = null;
					for (int j = 0; j < residents.Count; j++)
					{
						FerryPiece p = residents[j];
						if (p.Alive && p != predator && p.Dto.Species == rule.TargetSpecies)
						{
							prey = p;
							break;
						}
					}
					if (prey == null)
						continue;

					int preyCount = CountAlive(residents, rule.TargetSpecies);

					// 否定规则：fox does not eat sheep if fox is outnumbered by sheep
					if (IsSuppressed(rules, rule.TargetSpecies, preyCount, predCount))
						continue;

					// 数量条件：tiger eats bull if there are more tigers than bulls
					if (rule.MinCount > 0 && predCount <= preyCount)
						continue;

					if (best == null || rule.Priority > best.Priority)
					{
						best = rule;
						bestPrey = prey;
					}
				}
			}

			return bestPrey;
		}

		/// <summary>
		/// 否定规则判定
		/// </summary>
		public bool IsSuppressed(List<CfgPieceRuleTab> rules, int targetSpecies, int preyCount, int predCount)
		{
			for (int i = 0; i < rules.Count; i++)
			{
				CfgPieceRuleTab r = rules[i];
				if (r.Eat == 0 && r.TargetSpecies == targetSpecies && r.MinCount < 0 && preyCount > predCount)
					return true;
			}
			return false;
		}
	}
}
