namespace Hotfix
{
	/// <summary>
	/// 多语言接口类
	/// 当前语言由 LocalizationManager 统一管理（简体 / 繁体 / 英文）
	/// </summary>
	public static class LANG
	{
		/// <summary>
		/// 根据多语言哈希值获取当前语言的文本
		/// </summary>
		public static string Convert(int hashCode)
		{
			var tab = CfgAutoGenerateLanguage.Instance.GetCfgTab(hashCode);
			if (tab == null)
				return string.Empty;

			switch (LocalizationManager.Language)
			{
				case ELanguage.ChineseTraditional:
					return string.IsNullOrEmpty(tab.Lang_ct) ? tab.Lang : tab.Lang_ct;

				case ELanguage.English:
					return string.IsNullOrEmpty(tab.Lang_en) ? tab.Lang : tab.Lang_en;

				default:
					return tab.Lang;
			}
		}
	}
}
