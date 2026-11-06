using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
public class UILocalizeDropdown : MonoBehaviour
{
	public List<string> Keywords = new List<string>();

	private Dropdown _dropdown;

	private void Awake()
	{
		_dropdown = GetComponent<Dropdown>();
	}
	private void OnEnable()
	{
		LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
		Refresh();
	}
	private void OnDisable()
	{
		LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
	}

	private void HandleLanguageChanged(ELanguage language)
	{
		Refresh();
	}
	private void Refresh()
	{
		if (_dropdown == null)
			return;

		// 当前文本
		string defaultKeyword = GetKeyword(_dropdown.value);
		if (_dropdown.captionText != null)
			_dropdown.captionText.text = LocalizationManager.Get(defaultKeyword);

		// 下拉列表
		for (int i = 0; i < _dropdown.options.Count; i++)
		{
			string keyword = GetKeyword(i);
			_dropdown.options[i].text = LocalizationManager.Get(keyword);
		}
	}

	private string GetKeyword(int index)
	{
		for (int i = 0; i < Keywords.Count; i++)
		{
			if (i == index)
				return Keywords[i];
		}

		Debug.LogWarning($"UILocalizationDropdown : index is out range {index}");
		return string.Empty;
	}
}
