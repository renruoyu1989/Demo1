using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using MotionFramework.Window;

namespace Hotfix
{
	/// <summary>
	/// 谜题规则说明：本关的棋子、吃子关系、数量条件与否定（守卫）规则，全部来自配表。
	/// 内容可滚动，避免棋子多的关卡把面板撑爆。
	/// </summary>
	[Window((int)EWindowLayer.Panel, false)]
	public class UIRules : WindowBase
	{
		private Text _body;

		public override void OnCreate()
		{
			RectTransform root = FerryUIBuilder.CreateFullScreen(Go.transform, "Root");
			FerryUIBuilder.CreateBackground(root, new Color(0f, 0f, 0f, 0.68f));
			FerryUIBuilder.CreatePanel(root, new Vector2(0f, 10f), new Vector2(940f, 760f), FerryUIBuilder.ColorPanel);

			FerryUIBuilder.CreateText(root, "Title", LocalizationManager.Get("Ferry_RulesTitle"), 42, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorAccent, new Vector2(0f, 330f), new Vector2(860f, 70f));

			_body = CreateScrollableText(root, new Vector2(0f, 10f), new Vector2(840f, 560f), 22);

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Close"), new Vector2(0f, -330f),
				new Vector2(260f, 64f), () => WindowManager.Instance.CloseWindow<UIRules>());
		}

		/// <summary>可滚动文本区（Viewport + Mask + ScrollRect + 自动撑高的 Content）</summary>
		public static Text CreateScrollableText(Transform parent, Vector2 pos, Vector2 size, int fontSize)
		{
			RectTransform view = FerryUIBuilder.CreateRect(parent, "Viewport", pos, size);
			Image maskImage = view.gameObject.AddComponent<Image>();
			maskImage.color = new Color(1f, 1f, 1f, 0.01f);
			Mask mask = view.gameObject.AddComponent<Mask>();
			mask.showMaskGraphic = false;

			ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
			scroll.horizontal = false;
			scroll.vertical = true;
			scroll.movementType = ScrollRect.MovementType.Clamped;
			scroll.viewport = view;

			RectTransform content = FerryUIBuilder.CreateRect(view, "Content", Vector2.zero, Vector2.zero);
			content.anchorMin = new Vector2(0f, 1f);
			content.anchorMax = new Vector2(1f, 1f);
			content.pivot = new Vector2(0.5f, 1f);
			content.anchoredPosition = Vector2.zero;
			content.sizeDelta = new Vector2(0f, 0f);

			Text text = content.gameObject.AddComponent<Text>();
			text.font = FerryUIBuilder.Font;
			text.fontSize = fontSize;
			text.color = FerryUIBuilder.ColorText;
			text.alignment = TextAnchor.UpperLeft;
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Overflow;
			text.raycastTarget = false;
			text.supportRichText = true;

			ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			scroll.content = content;
			return text;
		}

		public override void OnDestroy()
		{
		}

		public override void OnRefresh()
		{
			if (_body == null)
				return;

			FerryGameSession session = FerryGameFlow.Instance.Session;
			if (session == null)
			{
				_body.text = string.Empty;
				return;
			}

			StringBuilder builder = new StringBuilder();
			builder.Append(LocalizationManager.Get("Ferry_RulesIntro")).Append("\n\n");

			List<FerryPiece> pieces = session.Pieces;
			List<int> printed = new List<int>();
			for (int i = 0; i < pieces.Count; i++)
			{
				CfgPieceTab piece = pieces[i].Dto;
				if (printed.Contains(piece.Id))
					continue;
				printed.Add(piece.Id);

				builder.Append("<b>").Append(piece.Name).Append("</b>");
				if (piece.Essential == 1)
					builder.Append("  ★ ").Append(LocalizationManager.Get("Ferry_Essential"));
				if (string.IsNullOrEmpty(piece.Desc) == false)
					builder.Append("   ").Append(piece.Desc);
				builder.Append('\n');

				List<int> keys = CfgPieceRule.Instance.GetKeys();
				for (int k = 0; k < keys.Count; k++)
				{
					CfgPieceRuleTab rule = CfgPieceRule.Instance.GetCfgTab(keys[k]);
					if (rule == null || rule.PieceID != piece.Id)
						continue;

					CfgPieceTab prey = FerryConfigService.Instance.GetPieceBySpecies(rule.TargetSpecies);
					string preyName = prey != null ? prey.Name : ("#" + rule.TargetSpecies);
					string verb = rule.Eat == 1
						? LocalizationManager.Get("Ferry_Prey")
						: LocalizationManager.Get("Ferry_Guard");

					builder.Append("    ").Append(verb).Append(' ').Append(preyName);
					if (rule.MinCount > 1)
						builder.Append("   (").Append(rule.MinCount).Append("+)");
					if (string.IsNullOrEmpty(rule.Desc) == false)
						builder.Append("   ").Append(rule.Desc);
					builder.Append('\n');
				}

				builder.Append('\n');
			}

			builder.Append(LocalizationManager.Get("Ferry_RulesScoring"));
			_body.text = builder.ToString();
		}

		public override void OnUpdate()
		{
		}
	}
}
