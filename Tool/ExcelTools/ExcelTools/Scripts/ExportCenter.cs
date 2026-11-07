using System;
using System.Collections.Generic;
using System.IO;

public static class ExportCenter
{
	/// <summary>
	/// 导出表格列表
	/// </summary>
	public static void Export(List<string> fileList)
	{
		// 清空多语言管理器的缓存数据
		LanguageMgr.Instance.ClearCacheLanguage();
		// 加载多语言总表
		LanguageMgr.Instance.LoadAutoGenerateLanguageToCache();

		// 加载选择的Excel文件列表
		for (int i = 0; i < fileList.Count; i++)
		{
			string filePath = fileList[i];
			ExcelData excelFile = new ExcelData(filePath);
			if (excelFile.Load())
			{
				if (excelFile.Export())
				{
					// 导出成功后，我们解析表格的多语言数据
					var data = LanguageMgr.ParseLanguage(excelFile);
					LanguageMgr.Instance.CacheLanguage(data);
				}
			}
			excelFile.Dispose();
		}

		// 创建新的多语言总表文件
		LanguageMgr.Instance.CreateAutoGenerateLanguageFile();
		// 导出多语言总表文件
		LanguageMgr.Instance.ExportAutoGenerateLanguageFile();
	}

	/// <summary>
	/// 命令行入口
	/// </summary>
	public static void RunCommand(CommandLineOptions options)
	{
		// 初始化配置
		ExportConfig.Instance.Init(options);
		SettingConfig.Instance.Init();

		// 导出Excel文件列表
		Export(GetExcelFileList(ExportConfig.Instance.ExcelPath));
	}

	/// <summary>
	/// 获取表格目录下所有可导出的Excel文件
	/// （多语言总表是导出产物，不参与导出）
	/// </summary>
	public static List<string> GetExcelFileList(string excelPath)
	{
		List<string> fileList = new List<string>();
		if (Directory.Exists(excelPath) == false)
			return fileList;

		string[] files = Directory.GetFiles(excelPath, "*.*");
		for (int i = 0; i < files.Length; i++)
		{
			if (files[i].EndsWith(LanguageMgr.StrAutoGenerateLanguageExcelName + ".xlsx"))
				continue;

			if (files[i].EndsWith(".xlsx") || files[i].EndsWith(".xls"))
				fileList.Add(files[i]);
		}

		return fileList;
	}
}
