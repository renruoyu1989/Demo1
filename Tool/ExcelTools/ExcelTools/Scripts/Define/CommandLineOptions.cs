using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 命令行参数
/// 用法：ExcelTools.exe [-e 表格目录] [-t 导出类型] [-o 导出目录] [-c|-n] [-h]
/// </summary>
public class CommandLineOptions
{
	/// <summary>
	/// 导出类型别名表
	/// </summary>
	private static readonly Dictionary<string, Type> ExporterAlias = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
	{
		{ "cs", typeof(CsExporter) },
		{ "csharp", typeof(CsExporter) },
		{ "script", typeof(CsExporter) },
		{ "bytes", typeof(BytesExporter) },
		{ "byte", typeof(BytesExporter) },
		{ "binary", typeof(BytesExporter) },
		{ "xml", typeof(XmlExporter) },
	};

	/// <summary>
	/// 表格源文件目录（绝对路径）
	/// </summary>
	public string ExcelPath { get; private set; }

	/// <summary>
	/// 需要导出的类型（顺序即执行顺序）
	/// </summary>
	public readonly List<Type> ExporterTypes = new List<Type>();

	/// <summary>
	/// 导出根目录（为null时使用各导出类型的默认目录）
	/// </summary>
	public string ExportPath { get; private set; }

	/// <summary>
	/// 是否清空导出目录
	/// null 表示按默认策略：默认目录清空，自定义目录不清空
	/// </summary>
	public bool? CleanExport { get; private set; }

	/// <summary>
	/// 是否显示帮助
	/// </summary>
	public bool ShowHelp { get; private set; }

	/// <summary>
	/// 结束后是否等待按键（无参数启动时默认等待，方便双击运行）
	/// </summary>
	public bool WaitForExit { get; private set; }

	/// <summary>
	/// 解析命令行参数
	/// </summary>
	public static CommandLineOptions Parse(string[] args)
	{
		CommandLineOptions options = new CommandLineOptions();
		options.WaitForExit = args == null || args.Length == 0;

		if (args != null)
		{
			for (int i = 0; i < args.Length; i++)
			{
				string key = args[i];
				if (string.IsNullOrWhiteSpace(key))
					continue;

				// 支持 --key=value 形式
				string inlineValue = null;
				int equalIndex = key.IndexOf('=');
				if (key.StartsWith("-") && equalIndex > 0)
				{
					inlineValue = key.Substring(equalIndex + 1);
					key = key.Substring(0, equalIndex);
				}

				switch (key.ToLower())
				{
					case "-e":
					case "--excel":
					case "-excel":
						options.ExcelPath = ToFullPath(GetValue(args, ref i, inlineValue, key));
						break;

					case "-t":
					case "--type":
					case "-type":
						options.AddExportTypes(GetValue(args, ref i, inlineValue, key));
						break;

					case "-o":
					case "--out":
					case "-out":
						options.ExportPath = ToFullPath(GetValue(args, ref i, inlineValue, key));
						break;

					case "-c":
					case "--clean":
						options.CleanExport = true;
						break;

					case "-n":
					case "--no-clean":
						options.CleanExport = false;
						break;

					case "-h":
					case "--help":
					case "-help":
					case "-?":
						options.ShowHelp = true;
						return options;

					default:
						throw new ArgumentException($"未知的参数：{args[i]}（使用 -h 查看帮助）");
				}
			}
		}

		// 默认表格目录
		if (string.IsNullOrEmpty(options.ExcelPath))
			options.ExcelPath = ToFullPath("../Excel/");

		// 默认导出类型
		if (options.ExporterTypes.Count == 0)
		{
			options.AddExporterType(typeof(CsExporter));
			options.AddExporterType(typeof(BytesExporter));
		}

		return options;
	}

	/// <summary>
	/// 打印帮助信息
	/// </summary>
	public static void PrintHelp()
	{
		Console.WriteLine("用法: ExcelTools [选项]");
		Console.WriteLine();
		Console.WriteLine("选项:");
		Console.WriteLine("  -e, --excel <目录>   表格源文件目录（默认 ../Excel/）");
		Console.WriteLine("  -t, --type <类型>    导出类型，逗号分隔: cs | bytes | xml | all（默认 cs,bytes）");
		Console.WriteLine("  -o, --out <目录>     导出目录。只导出一种类型时直接使用该目录；");
		Console.WriteLine("                       导出多种类型时作为根目录，按类型建 cs/ bytes/ xml/ 子目录");
		Console.WriteLine("  -c, --clean          导出前清空导出目录（删除除 .meta 外的所有文件）");
		Console.WriteLine("  -n, --no-clean       导出前不清空导出目录");
		Console.WriteLine("  -h, --help           显示帮助");
		Console.WriteLine();
		Console.WriteLine("说明:");
		Console.WriteLine("  * 不指定 --out 时使用各导出类型的默认目录：");
		Console.WriteLine($"      cs    -> {ExportConfig.DefaultCsDir}");
		Console.WriteLine($"      bytes -> {ExportConfig.DefaultBytesDir}");
		Console.WriteLine($"      xml   -> {ExportConfig.DefaultXmlDir}");
		Console.WriteLine("  * 默认目录导出前会清空；用 --out 指定目录时默认不清空（需显式加 --clean）。");
		Console.WriteLine("  * 路径可以是绝对路径，也可以是相对当前工作目录的路径。");
		Console.WriteLine();
		Console.WriteLine("示例:");
		Console.WriteLine("  ExcelTools.exe");
		Console.WriteLine("  ExcelTools.exe -e D:\\Demo1\\表格 -t cs -o D:\\Demo1\\Temp\\Cfg");
		Console.WriteLine("  ExcelTools.exe --excel=..\\..\\表格 --type=cs,bytes --out=..\\..\\Out --clean");
	}

	private void AddExportTypes(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			throw new ArgumentException("导出类型不能为空");

		string[] items = raw.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < items.Length; i++)
		{
			string name = items[i].Trim();
			if (name.Length == 0)
				continue;

			if (name.Equals("all", StringComparison.OrdinalIgnoreCase))
			{
				foreach (Type type in ExporterAlias.Values)
					AddExporterType(type);
				continue;
			}

			Type exporterType;
			if (ExporterAlias.TryGetValue(name, out exporterType) == false)
			{
				// 允许直接写类名，例如 CsExporter / BytesExporter / XmlExporter
				string shortName = name.Replace("Exporter", string.Empty, StringComparison.OrdinalIgnoreCase);
				if (ExporterAlias.TryGetValue(shortName, out exporterType) == false)
					throw new ArgumentException($"不支持的导出类型：{name}（可选 cs / bytes / xml / all）");
			}

			AddExporterType(exporterType);
		}
	}

	private void AddExporterType(Type exporterType)
	{
		if (ExporterTypes.Contains(exporterType))
			return;
		ExporterTypes.Add(exporterType);
	}

	private static string GetValue(string[] args, ref int index, string inlineValue, string key)
	{
		if (inlineValue != null)
		{
			if (inlineValue.Length == 0)
				throw new ArgumentException($"参数 {key} 缺少值");
			return inlineValue;
		}

		if (index + 1 >= args.Length)
			throw new ArgumentException($"参数 {key} 缺少值");

		index++;
		return args[index];
	}

	private static string ToFullPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("路径不能为空");

		return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
	}
}
