using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using MotionFramework.Window;

using MotionFramework.Data;

namespace Hotfix
{
	/// <summary>
	/// 关卡选择：按 LevelMap 配表把关卡画成地图节点（含相邻路径），
	/// 点击已解锁节点弹出关卡卡片，卡片里再进入关卡。经典 / 连击两套地图。
	/// </summary>
	[Window((int)EWindowLayer.Panel, true)]
	public class UISelect : WindowBase
	{
		private const float MapScale = 1.2f;
		private const float MapHalfWidth = 500f;
		private const float MapHalfHeight = 281f;

		private RectTransform _mapRoot;
		private RectTransform _card;
		private Text _cardTitle;
		private Text _cardDetail;
		private Button _classicTab;
		private Button _comboTab;
		private readonly List<GameObject> _nodes = new List<GameObject>();
		private int _mode;
		private int _selectedLevelId;

		public override void OnCreate()
		{
			RectTransform root = FerryUIBuilder.CreateFullScreen(Go.transform, "Root");
			FerryUIBuilder.CreateBackground(root, FerryUIBuilder.ColorBackdrop);

			FerryUIBuilder.CreateText(root, "Title", LocalizationManager.Get("Ferry_Select"), 46, TextAnchor.MiddleCenter,
				FerryUIBuilder.ColorAccent, new Vector2(0f, 470f), new Vector2(900f, 70f));

			_classicTab = FerryUIBuilder.CreateSmallButton(root, LocalizationManager.Get("Ferry_Classic"),
				new Vector2(-180f, 400f), new Vector2(280f, 58f), FerryUIBuilder.ColorAccent,
				() => SwitchMode(0), 24);

			_comboTab = FerryUIBuilder.CreateSmallButton(root, LocalizationManager.Get("Ferry_ComboMode"),
				new Vector2(180f, 400f), new Vector2(280f, 58f), FerryUIBuilder.ColorButton,
				() => SwitchMode(1), 24);

			// 地图底板 + 一条示意河道
			FerryUIBuilder.CreatePanel(root, new Vector2(0f, -20f), new Vector2(1360f, 750f),
				new Color(0.09f, 0.13f, 0.19f, 1f));
			FerryUIBuilder.CreateBar(root, "River", new Vector2(0f, -20f), new Vector2(1320f, 150f),
				new Color(0.13f, 0.36f, 0.58f, 0.45f));

			_mapRoot = FerryUIBuilder.CreateRect(root, "Map", new Vector2(0f, -20f), new Vector2(1300f, 700f));

			BuildCard(root);

			FerryUIBuilder.CreateButton(root, LocalizationManager.Get("Ferry_Back"), new Vector2(-820f, 430f),
				new Vector2(180f, 58f), () =>
				{
					WindowManager.Instance.CloseWindow<UISelect>();
					FerryGameCommands.GoMenu();
				}, 22);
		}

		private void BuildCard(RectTransform root)
		{
			// 卡片放在地图底板下方，避免遮住下半区的关卡节点
			_card = FerryUIBuilder.CreateRect(root, "Card", new Vector2(0f, -460f), new Vector2(760f, 130f));
			Image bg = _card.gameObject.AddComponent<Image>();
			bg.color = FerryUIBuilder.ColorPanel;
			bg.raycastTarget = true;

			_cardTitle = FerryUIBuilder.CreateText(_card, "CardTitle", string.Empty, 28, TextAnchor.MiddleLeft,
				FerryUIBuilder.ColorAccent, new Vector2(-230f, 20f), new Vector2(420f, 44f));

			_cardDetail = FerryUIBuilder.CreateText(_card, "CardDetail", string.Empty, 22, TextAnchor.MiddleLeft,
				FerryUIBuilder.ColorTextDim, new Vector2(-230f, -22f), new Vector2(420f, 40f));

			FerryUIBuilder.CreateButton(_card, LocalizationManager.Get("Ferry_Start"),
				new Vector2(240f, 0f), new Vector2(220f, 72f), StartSelected, 26);

			_card.gameObject.SetActive(false);
		}

		public override void OnDestroy()
		{
		}

		public override void OnRefresh()
		{
			if (UserData is int)
			{
				_mode = ((int)UserData) == 1 ? 1 : 0;
				FerryUIBuilder.SetButtonColor(_classicTab, _mode == 0 ? FerryUIBuilder.ColorAccent : FerryUIBuilder.ColorButton);
				FerryUIBuilder.SetButtonColor(_comboTab, _mode == 1 ? FerryUIBuilder.ColorAccent : FerryUIBuilder.ColorButton);
			}

			_card.gameObject.SetActive(false);
			_selectedLevelId = 0;
			BuildMap();
		}

		public override void OnUpdate()
		{
		}

		private void SwitchMode(int mode)
		{
			if (_mode == mode)
				return;

			_mode = mode;
			FerryUIBuilder.SetButtonColor(_classicTab, mode == 0 ? FerryUIBuilder.ColorAccent : FerryUIBuilder.ColorButton);
			FerryUIBuilder.SetButtonColor(_comboTab, mode == 1 ? FerryUIBuilder.ColorAccent : FerryUIBuilder.ColorButton);
			_card.gameObject.SetActive(false);
			_selectedLevelId = 0;
			BuildMap();
		}

		private void BuildMap()
		{
			for (int i = 0; i < _nodes.Count; i++)
			{
				if (_nodes[i] != null)
					Object.Destroy(_nodes[i]);
			}
			_nodes.Clear();

			List<CfgLevelMapTab> nodes = FerryConfigService.Instance.GetMapNodes(_mode);
			nodes.Sort((a, b) => a.Order.CompareTo(b.Order));

			DataFerryProfile data = DataManager.Instance.GetData<DataFerryProfile>();

			// 路径线先画，节点后画
			for (int i = 0; i + 1 < nodes.Count; i++)
			{
				Vector2 a = ToMap(nodes[i].X, nodes[i].Y);
				Vector2 b = ToMap(nodes[i + 1].X, nodes[i + 1].Y);
				Image line = FerryUIBuilder.CreateLine(_mapRoot, "Path_" + i, a, b, 5f,
					new Color(0.42f, 0.52f, 0.62f, 0.65f));
				_nodes.Add(line.gameObject);
			}

			for (int i = 0; i < nodes.Count; i++)
			{
				CfgLevelMapTab node = nodes[i];
				int levelId = node.LevelID;
				bool unlocked = data == null || data.IsUnlocked(levelId);
				bool cleared = data != null && data.IsCleared(levelId);

				Color color = unlocked
					? (cleared ? new Color(0.24f, 0.48f, 0.32f, 1f) : FerryUIBuilder.ColorAccent)
					: new Color(0.18f, 0.20f, 0.24f, 1f);

				Vector2 pos = ToMap(node.X, node.Y);
				Button button = FerryUIBuilder.CreateSmallButton(_mapRoot, (node.Order + 1).ToString(), pos,
					new Vector2(52f, 52f), color,
					unlocked ? (System.Action)(() => SelectNode(levelId)) : null, 20);
				button.interactable = unlocked;
				_nodes.Add(button.gameObject);
			}
		}

		private static Vector2 ToMap(float designX, float designY)
		{
			return new Vector2((designX - MapHalfWidth) * MapScale,
				(MapHalfHeight - designY) * MapScale);
		}

		private void SelectNode(int levelId)
		{
			_selectedLevelId = levelId;
			CfgLevelTab level = CfgLevel.Instance.GetCfgTab(levelId);
			DataFerryProfile data = DataManager.Instance.GetData<DataFerryProfile>();

			_cardTitle.text = level != null ? string.Format("{0}   #{1}", level.Code, levelId) : ("Level " + levelId);
			_cardDetail.text = string.Format("{0} {1}    {2} {3}",
				LocalizationManager.Get("Ferry_Best"), data != null ? data.GetBestScore(levelId) : 0,
				LocalizationManager.Get("Ferry_Cleared"), data != null && data.IsCleared(levelId)
					? LocalizationManager.Get("Ferry_On") : LocalizationManager.Get("Ferry_Off"));
			_card.gameObject.SetActive(true);
		}

		private void StartSelected()
		{
			if (_selectedLevelId <= 0)
				return;

			WindowManager.Instance.CloseWindow<UISelect>();
			FerryGameCommands.SelectLevel(_selectedLevelId);
		}
	}
}
