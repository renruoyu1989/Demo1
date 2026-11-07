using System.Collections.Generic;

namespace Hotfix
{
	/// <summary>
	/// 单个单位的结算结果
	/// </summary>
	public struct FerryScoreEntry
	{
		/// <summary>单位</summary>
		public FerryPiece Piece;

		/// <summary>本次得分</summary>
		public int Value;

		/// <summary>终点码头上的同名数量（同时作为界面上的 xCombo 倍率）</summary>
		public int SameCount;
	}

	/// <summary>
	/// 计分：只统计终点码头，每个单位得 50 × 该码头同名数量
	/// </summary>
	public static class FerryScoreCalculator
	{
		public static List<FerryScoreEntry> Calculate(List<FerryPiece> atEnd)
		{
			List<FerryScoreEntry> result = new List<FerryScoreEntry>();
			for (int i = 0; i < atEnd.Count; i++)
			{
				FerryPiece p = atEnd[i];

				int same = 0;
				for (int j = 0; j < atEnd.Count; j++)
				{
					if (atEnd[j].Dto.Code == p.Dto.Code)
						same++;
				}

				FerryScoreEntry entry = new FerryScoreEntry();
				entry.Piece = p;
				entry.SameCount = same;
				entry.Value = same * FerryDefine.ScorePerPiece;
				result.Add(entry);
			}
			return result;
		}

		public static int Sum(List<FerryScoreEntry> entries)
		{
			int total = 0;
			for (int i = 0; i < entries.Count; i++)
			{
				total += entries[i].Value;
			}
			return total;
		}
	}
}
