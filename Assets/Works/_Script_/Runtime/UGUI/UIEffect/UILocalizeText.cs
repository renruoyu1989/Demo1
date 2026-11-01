using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class UILocalizeText : MonoBehaviour
{
	public string KeyWord = "";

	private Text _text;

	private void Awake()
	{
		_text = GetComponent<Text>();
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

	/// <summary>
	/// 设置本地化Key
	/// </summary>
	public void SetKey(string key)
	{
		KeyWord = key;
		Refresh();
	}

	private void HandleLanguageChanged(ELanguage language)
	{
		Refresh();
	}
	private void Refresh()
	{
		if (_text == null)
			_text = GetComponent<Text>();
		if (_text == null)
			return;

		_text.text = LocalizationManager.Get(KeyWord);
	}
}
