using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hotfix.EditorTools
{
	/// <summary>
	/// 分层校验：过河逻辑层（Hotfix/Ferryman/Core）不允许引用 UnityEngine
	/// 原因：业务 asmdef 不能拆（UIManager 靠程序集反射收集窗口），
	///      所以「逻辑层无 UnityEngine」只能靠这条编辑器检查兜底
	/// </summary>
	public static class FerryLayeringValidator
	{
		private const string CoreDir = "Assets/GameScript/Hotfix/Ferryman/Core";

		[MenuItem("Tools/Ferryman/3. 校验分层规范", false, 3)]
		public static void Validate()
		{
			List<string> files = new List<string>();
			CollectFiles(CoreDir, files);

			int errors = 0;
			for (int i = 0; i < files.Count; i++)
			{
				string[] lines = File.ReadAllLines(files[i]);
				for (int l = 0; l < lines.Length; l++)
				{
					string line = lines[l].Trim();
					if (line.StartsWith("//"))
						continue;

					if (line.StartsWith("using UnityEngine") || line.StartsWith("using YooAsset"))
					{
						Debug.LogError($"[分层规范] 逻辑层禁止引用 UnityEngine/YooAsset：{files[i]}:{l + 1}  {line}");
						errors++;
					}
				}
			}

			if (errors == 0)
				Debug.Log($"[分层规范] 通过，检查了 {files.Count} 个逻辑层文件");
			else
				Debug.LogError($"[分层规范] 发现 {errors} 处违规");
		}

		private static void CollectFiles(string dir, List<string> files)
		{
			if (Directory.Exists(dir) == false)
				return;

			files.AddRange(Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories));
		}
	}
}
