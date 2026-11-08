//**************************************************
// Copyright©2019 何冠峰
// Licensed under the MIT license
//**************************************************
using System;
using System.Collections.Generic;
using System.IO;

public class ExportConfig
{
	public static ExportConfig Instance = new ExportConfig();

	/// <summary>
	/// 默认导出目录（相对当前工作目录，即工具发布目录）
	///   cs    -> Assets/GameScript/Hotfix/Config/AutoGenerateConfig
	///   bytes / xml 当前工程没有运行时消费方，导出到工具自己的目录，避免污染 Unity 资源目录
	/// </summary>
	public const string DefaultCsDir = "../Assets/GameScript/Hotfix/Config/AutoGenerateConfig";
	public const string DefaultBytesDir = "../Export/Bytes";
	public const string DefaultXmlDir = "../Export/Xml";

	public class ExportWrapper
	{
		/// <summary>
		/// 导出器类型
		/// </summary>
		public Type ExporterType;

		/// <summary>
		/// 导出路径（绝对路径）
		/// </summary>
		public string ExportPath;

		/// <summary>
		/// 导出标记（C=客户端 S=服务器）
		/// </summary>
		public string ExportLogo = "C";

		/// <summary>
		/// 导出前是否清空导出目录
		/// </summary>
		public bool CleanDirectory = true;
	}

	/// <summary>
	/// 表格源文件目录（绝对路径）
	/// </summary>
	public string ExcelPath { get; private set; }

	/// <summary>
	/// 本次运行实际需要执行的导出任务
	/// </summary>
	public readonly List<ExportWrapper> ExportList = new List<ExportWrapper>();

	private ExportConfig()
	{
	}

	/// <summary>
	/// 初始化
	/// </summary>
	public void Init(CommandLineOptions options)
	{
		ExcelPath = options.ExcelPath;

		ExportList.Clear();
		for (int i = 0; i < options.ExporterTypes.Count; i++)
		{
			Type exporterType = options.ExporterTypes[i];
			ExportWrapper wrapper = CreateWrapper(exporterType);

			if (options.ExportPath != null)
			{
				// 只导出一种类型时，参数目录就是导出目录
				// 导出多种类型时，参数目录作为根目录，按类型名建子目录
				wrapper.ExportPath = options.ExporterTypes.Count == 1
					? options.ExportPath
					: Path.Combine(options.ExportPath, GetExporterFolderName(exporterType));

				// 自定义目录默认不清空，避免误删用户目录里的其他文件
				wrapper.CleanDirectory = options.CleanExport ?? false;
			}
			else
			{
				wrapper.ExportPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), wrapper.ExportPath));

				// 默认不清空：导出目录里可能有手工维护的文件，清空会误删；
				// 需要清空时显式传 --clean
				wrapper.CleanDirectory = options.CleanExport ?? false;
			}

			wrapper.ExportPath = Path.GetFullPath(wrapper.ExportPath);

			if (wrapper.CleanDirectory)
				CleanDirectory(wrapper.ExportPath);
			Directory.CreateDirectory(wrapper.ExportPath);

			ExportList.Add(wrapper);
		}
	}

	private ExportWrapper CreateWrapper(Type exporterType)
	{
		ExportWrapper wrapper = new ExportWrapper();
		wrapper.ExporterType = exporterType;
		wrapper.ExportLogo = "C";

		if (exporterType == typeof(CsExporter))
			wrapper.ExportPath = DefaultCsDir;
		else if (exporterType == typeof(BytesExporter))
			wrapper.ExportPath = DefaultBytesDir;
		else if (exporterType == typeof(XmlExporter))
			wrapper.ExportPath = DefaultXmlDir;
		else
			wrapper.ExportPath = "../" + exporterType.Name;

		return wrapper;
	}

	private static string GetExporterFolderName(Type exporterType)
	{
		if (exporterType == typeof(CsExporter))
			return "cs";
		if (exporterType == typeof(BytesExporter))
			return "bytes";
		if (exporterType == typeof(XmlExporter))
			return "xml";
		return exporterType.Name.ToLower();
	}

	private void CleanDirectory(string path)
	{
		if (!Directory.Exists(path))
			return;

		DirectoryInfo di = new DirectoryInfo(path);
		foreach (FileInfo file in di.GetFiles())
		{
			if (file.Extension.ToLower() != ".meta")
				file.Delete();
		}

		foreach (DirectoryInfo dir in di.GetDirectories())
		{
			CleanDirectory(dir.FullName);
		}
	}
}
