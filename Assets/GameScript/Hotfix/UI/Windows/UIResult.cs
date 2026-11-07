using UnityEngine;
using UnityEngine.UI;

namespace Hotfix
{
	/// <summary>
	/// 结算弹窗：胜负标题、得分、用时、连击、最高分/总分，以及继续 / 重试 / 关卡列表 / 主菜单。
	/// </summary>
	[Window(EWindowType.UIResult, EWindowLayer.Tips)]
	public class UIResult : UIWindow
	{
		private Text _title;
		private Text _score;
		private Text _detail;
		private Text _best;
		private Button _nextButton;

		public override void OnCreate()
		{
			RectTransform root = FerryUIBuilder.CreateFullScreen(Go.transform, "Root");
			FerryUIBuilder.CreateBackground(root, new Color(0f, 0f, 0f, 0.62f));
			FerryUIBuilder.CreatePanel(root, Vector2.zero, new Vector2(860f, 600f), FerryUIBuilder.ColorPanel);

			_title = FerryUIBuilder.CreateText(root, "Title", string.Empty, 62, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorAccent, new Vector2(0f, 210f), new Vector2(800f, 90f));

			_score = FerryUIBuilder.CreateText(root, "Score", string.Empty, 52, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorText, new Vector2(0f, 110f), new Vector2(800f, 80f));

			_detail = FerryUIBuilder.CreateText(root, "Detail", string.Empty, 26, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorText, new Vector2(0f, 30f), new Vector2(800f, 60f));

			_best = FerryUIBuilder.CreateText(root, "Record", string.Empty, 24, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorTextDim, new Vector2(0f, -35f), new Vector2(800f, 60f));

			_nextButton = FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Next"),
				new Vector2(-145f, -160f), new Vector2(250f, 72f), Next, 26);

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Retry"),
				new Vector2(145f, -160f), new Vector2(250f, 72f), Retry, 26);

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Select"),
				new Vector2(-145f, -245f), new Vector2(250f, 60f), GoSelect, 22);

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Menu"),
				new Vector2(145f, -245f), new Vector2(250f, 60f), GoMenu, 22);
		}

		public override void OnDestroy()
		{
		}

		public override void OnRefresh()
		{
			FerryGameSession session = FerryGameFlow.Instance.Session;
			if (session == null)
				return;

			_title.text = LocalizationManager.Get(session.IsWin ? "Ferry_Win" : "Ferry_Lose");
			_title.color = session.IsWin ? FerryUIBuilder.ColorAccent : new Color(0.90f, 0.35f, 0.35f, 1f);

			_score.text = string.Format("{0}  {1}", LocalizationManager.Get("Ferry_Score"), session.Score);
			_detail.text = string.Format(LocalizationManager.Get("Ferry_ResultDetail"),
				session.Ferried, Mathf.Max(1, session.Combo), session.ElapsedText());

			DataFerryProfile data = DataManager.Instance.GetData<DataFerryProfile>();
			if (data != null)
			{
				_best.text = string.Format("{0}  {1}     {2}  {3}",
					LocalizationManager.Get("Ferry_Best"), data.GetBestScore(session.Level.Dto.Id),
					LocalizationManager.Get("Ferry_Total"), data.TotalScore);
			}

			_nextButton.gameObject.SetActive(session.IsWin);
		}

		public override void OnUpdate()
		{
		}

		private void Next()
		{
			UIManager.Instance.CloseWindow(EWindowType.UIResult);
			FerryGameCommands.NextLevel();
		}

		private void Retry()
		{
			UIManager.Instance.CloseWindow(EWindowType.UIResult);
			FerryGameCommands.Restart();
		}

		private void GoSelect()
		{
			UIManager.Instance.CloseWindow(EWindowType.UIResult);
			FerryGameCommands.GoSelect();
		}

		private void GoMenu()
		{
			UIManager.Instance.CloseWindow(EWindowType.UIResult);
			FerryGameCommands.GoMenu();
		}
	}
}
