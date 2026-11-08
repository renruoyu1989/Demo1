using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// 导出客户端 C# 配表。
/// 生成的代码与 Assets/GameScript/Hotfix/Config/AutoGenerateConfig/*.cs 一致：
/// 数据在生成期直接写进 C#（ConfigTab + AssetConfig.AddElement），运行时不做字节流解析。
/// </summary>
[ExportAttribute("导出CS脚本")]
public class CsExporter : BaseExporter
{
	private const string ProtectedProperty = " { protected set; get; }";

	public CsExporter(SheetData sheetData)
		: base(sheetData)
	{
	}

	public override void ExportFile(string path, string createLogo)
	{
		string filePath = StringHelper.MakeSaveFullPath(path, $"Cfg{_sheet.FileName}.cs");
		List<HeadWrapper> heads = GetExportHeads(createLogo);
		GetIdHead(heads); // 提前校验，避免写出一半文件

		using (FileStream fs = new FileStream(filePath, FileMode.Create))
		using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
		{
			WriteHeader(sw);
			WriteTabClass(sw, heads);
			sw.WriteLine();
			WriteConfigClass(sw, heads, createLogo);
			sw.WriteLine("}");
		}
	}

	/// <summary>参与本次导出的列（跳过策划注释列与不匹配导出标记的簇）</summary>
	private List<HeadWrapper> GetExportHeads(string createLogo)
	{
		List<HeadWrapper> heads = new List<HeadWrapper>();
		for (int i = 0; i < _sheet.Heads.Count; i++)
		{
			HeadWrapper head = _sheet.Heads[i];
			if (head.IsNotes || head.Logo.Contains(createLogo) == false)
				continue;
			heads.Add(head);
		}
		return heads;
	}

	private static HeadWrapper GetIdHead(List<HeadWrapper> heads)
	{
		for (int i = 0; i < heads.Count; i++)
		{
			if (heads[i].Name == ConstDefine.StrHeadId)
				return heads[i];
		}
		throw new Exception("表格必须设立一个 'id' 列.");
	}

	private void WriteHeader(StreamWriter sw)
	{
		sw.WriteLine("//--------------------------------------------------");
		sw.WriteLine("// 自动生成  请勿修改");
		sw.WriteLine($"// 数据来源：表格/{_sheet.FileName}.xlsx");
		sw.WriteLine("//--------------------------------------------------");
		sw.WriteLine("using System.Collections.Generic;");
		sw.WriteLine();
		sw.WriteLine("namespace Hotfix");
		sw.WriteLine("{");
	}

	private void WriteTabClass(StreamWriter sw, List<HeadWrapper> heads)
	{
		string tabName = $"Cfg{_sheet.FileName}Tab";
		sw.WriteLine($"\tpublic class {tabName} : ConfigTab");
		sw.WriteLine("\t{");

		// 字段
		for (int i = 0; i < heads.Count; i++)
		{
			HeadWrapper head = heads[i];
			if (head.Name == ConstDefine.StrHeadId)
				continue;

			string name = StringHelper.ToUpperFirstChar(head.Name);
			if (head.Type == "language")
			{
				// 多语言列保存总表 ID，读取时按当前语言转换
				sw.WriteLine($"\t\tpublic string {name} {{ get {{ return LANG.Convert({name}ID); }} }}");
				sw.WriteLine($"\t\tpublic int {name}ID{ProtectedProperty}");
			}
			else
			{
				sw.WriteLine($"\t\tpublic {GetCSharpType(head)} {name}{ProtectedProperty}");
			}
		}
		sw.WriteLine();

		// 构造函数：id 固定在第一位，其余按表格列顺序
		List<string> args = new List<string> { "int id" };
		for (int i = 0; i < heads.Count; i++)
		{
			HeadWrapper head = heads[i];
			if (head.Name == ConstDefine.StrHeadId)
				continue;

			string param = StringHelper.ToLowerFirstChar(head.Name);
			args.Add(head.Type == "language" ? $"int {param}ID" : $"{GetCSharpType(head)} {param}");
		}
		sw.WriteLine($"\t\tpublic {tabName}({string.Join(", ", args)})");
		sw.WriteLine("\t\t{");
		sw.WriteLine("\t\t\tId = id;");
		for (int i = 0; i < heads.Count; i++)
		{
			HeadWrapper head = heads[i];
			if (head.Name == ConstDefine.StrHeadId)
				continue;

			string name = StringHelper.ToUpperFirstChar(head.Name);
			string param = StringHelper.ToLowerFirstChar(head.Name);
			if (head.Type == "language")
				sw.WriteLine($"\t\t\t{name}ID = {param}ID;");
			else
				sw.WriteLine($"\t\t\t{name} = {param};");
		}
		sw.WriteLine("\t\t}");
		sw.WriteLine("\t}");
	}

	private void WriteConfigClass(StreamWriter sw, List<HeadWrapper> heads, string createLogo)
	{
		string cfgName = $"Cfg{_sheet.FileName}";
		string tabName = $"{cfgName}Tab";
		HeadWrapper idHead = GetIdHead(heads);

		sw.WriteLine($"\tpublic partial class {cfgName} : AssetConfig");
		sw.WriteLine("\t{");
		sw.WriteLine($"\t\tprivate static {cfgName} _instance;");
		sw.WriteLine($"\t\tpublic static {cfgName} Instance");
		sw.WriteLine("\t\t{");
		sw.WriteLine("\t\t\tget");
		sw.WriteLine("\t\t\t{");
		sw.WriteLine($"\t\t\t\tif (_instance == null) {{ _instance = new {cfgName}(); _instance.Create(); }}");
		sw.WriteLine("\t\t\t\treturn _instance;");
		sw.WriteLine("\t\t\t}");
		sw.WriteLine("\t\t}");
		sw.WriteLine();
		sw.WriteLine($"\t\tprivate {cfgName}() {{ }}");
		sw.WriteLine($"\t\tpublic {tabName} GetCfgTab(int key)");
		sw.WriteLine("\t\t{");
		sw.WriteLine($"\t\t\treturn GetTab(key) as {tabName};");
		sw.WriteLine("\t\t}");
		sw.WriteLine("\t\tpublic void Create()");
		sw.WriteLine("\t\t{");

		for (int row = 0; row < _sheet.Tables.Count; row++)
		{
			TableWrapper table = _sheet.Tables[row];
			string idLiteral = ToIdLiteral(idHead, table.GetCellValue(idHead.CellNum));

			List<string> literals = new List<string> { idLiteral };
			for (int i = 0; i < heads.Count; i++)
			{
				HeadWrapper head = heads[i];
				if (head.Name == ConstDefine.StrHeadId)
					continue;
				literals.Add(ToLiteral(head, table.GetCellValue(head.CellNum), createLogo));
			}

			sw.WriteLine($"\t\t\tAddElement({idLiteral}, new {tabName}({string.Join(", ", literals)}));");
		}

		sw.WriteLine("\t\t}");
		sw.WriteLine("\t}");
	}

	private static string GetCSharpType(HeadWrapper head)
	{
		string type = head.Type;
		if (type == "number" || type == "language")
			return type == "number" ? "float" : "string";
		if (type == "List<language>")
			return "List<string>";
		if (type.Contains("enum") || type.Contains("class"))
		{
			string extendType = StringHelper.GetExtendType(type);
			return type.Contains("List") ? $"List<{extendType}>" : extendType;
		}
		return type;
	}

	private static string ToIdLiteral(HeadWrapper head, string value)
	{
		if (string.IsNullOrEmpty(value))
			value = "0";
		if (head.Type == "string")
			return value.GetFixedHashCode().ToString(CultureInfo.InvariantCulture);
		return value;
	}

	private static string ToLiteral(HeadWrapper head, string value, string createLogo)
	{
		string type = head.Type;
		if (value == null)
			value = string.Empty;

		if (type == "int" || type == "long" || type == "double")
			return string.IsNullOrEmpty(value) ? "0" : value;

		if (type == "float")
			return FloatLiteral(ParseFloat(value));

		if (type == "number")
			return FloatLiteral(ParseFloat(value) * 0.0001f);

		if (type == "bool")
			return string.IsNullOrEmpty(value) || value == "0" ? "false" : "true";

		if (type == "string")
			return $"\"{EscapeString(value)}\"";

		if (type.StartsWith("List<", StringComparison.Ordinal))
			return ListLiteral(type, value, createLogo);

		if (type == "language")
			return LanguageMgr.Instance.GetValueKeyCode(value.GetFixedHashCode()).ToString(CultureInfo.InvariantCulture);

		if (type.Contains("enum"))
			return $"({StringHelper.GetExtendType(type)}){(string.IsNullOrEmpty(value) ? "0" : value)}";

		if (type.Contains("class"))
			throw new Exception($"列 {head.Name} 的类型 {type} 无法在生成期展开为 C# 字面量，请改用基础类型或 List<T>.");

		throw new Exception($"Not support head type {type}");
	}

	private static string ListLiteral(string type, string value, string createLogo)
	{
		string elemType = type.Substring(5, type.Length - 6);
		string csElemType = elemType == "language" ? "string" : elemType;
		string[] items = string.IsNullOrEmpty(value)
			? new string[0]
			: value.Split(new char[] { ConstDefine.StrSplitChar }, StringSplitOptions.RemoveEmptyEntries);

		List<string> literals = new List<string>();
		for (int i = 0; i < items.Length; i++)
		{
			string item = items[i].Trim();
			switch (elemType)
			{
				case "int":
				case "long":
				case "double":
					literals.Add(string.IsNullOrEmpty(item) ? "0" : item);
					break;
				case "float":
					literals.Add(FloatLiteral(ParseFloat(item)));
					break;
				case "string":
					literals.Add($"\"{EscapeString(item)}\"");
					break;
				case "language":
					literals.Add($"LANG.Convert({LanguageMgr.Instance.GetValueKeyCode(item.GetFixedHashCode())})");
					break;
				default:
					throw new Exception($"Not support list element type {elemType}");
			}
		}

		return literals.Count == 0
			? $"new List<{csElemType}>()"
			: $"new List<{csElemType}> {{ {string.Join(", ", literals)} }}";
	}

	private static float ParseFloat(string value)
	{
		if (string.IsNullOrEmpty(value))
			return 0f;
		return float.Parse(value, CultureInfo.InvariantCulture);
	}

	private static string FloatLiteral(float value)
	{
		return value.ToString("R", CultureInfo.InvariantCulture) + "f";
	}

	private static string EscapeString(string value)
	{
		return value
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"")
			.Replace("\r", "\\r")
			.Replace("\n", "\\n")
			.Replace("\t", "\\t");
	}
}
