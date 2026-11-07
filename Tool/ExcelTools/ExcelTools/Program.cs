using System;
using System.Collections.Generic;
using System.IO;

namespace ExcelTools
{
	class Program
	{
		static int Main(string[] args)
		{
			CommandLineOptions options;
			try
			{
				options = CommandLineOptions.Parse(args);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"参数错误：{ex.Message}");
				CommandLineOptions.PrintHelp();
				return 1;
			}

			if (options.ShowHelp)
			{
				CommandLineOptions.PrintHelp();
				return 0;
			}

			// 初始化配置
			ExportConfig.Instance.Init(options);
			SettingConfig.Instance.Init();

			PrintSummary();

			ExportExcel();

			Console.WriteLine("Export Config Over!");

			// 无参数启动（通常是双击）时保留窗口，避免一闪而过
			if (options.WaitForExit)
				Console.ReadLine();

			return 0;
		}

		static void PrintSummary()
		{
			Console.WriteLine($"表格目录 : {ExportConfig.Instance.ExcelPath}");
			for (int i = 0; i < ExportConfig.Instance.ExportList.Count; i++)
			{
				ExportConfig.ExportWrapper wrapper = ExportConfig.Instance.ExportList[i];
				Console.WriteLine($"导出类型 : {wrapper.ExporterType.Name} -> {wrapper.ExportPath}");
			}
			Console.WriteLine();
		}

		static void ExportExcel()
		{
			string excelPath = ExportConfig.Instance.ExcelPath;
			if (Directory.Exists(excelPath) == false)
			{
				Console.WriteLine($"表格目录不存在：{excelPath}");
				return;
			}

			List<string> fileList = ExportCenter.GetExcelFileList(excelPath);
			if (fileList.Count == 0)
			{
				Console.WriteLine($"表格目录里没有可导出的表格：{excelPath}");
				return;
			}

			Console.WriteLine($"待导出表格数量 : {fileList.Count}");
			Console.WriteLine();

			// 导出Excel文件列表
			ExportCenter.Export(fileList);
		}
	}
}
