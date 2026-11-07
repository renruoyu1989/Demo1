using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using MotionFramework;

/// <summary>
/// 存档文件包装
/// 注意：业务数据的JSON放在Payload里，方便以后做数据迁移和兼容处理
/// </summary>
[Serializable]
public class SaveEnvelope
{
	public int Version;
	public long SaveTicks;
	public string Payload;
}

/// <summary>
/// 通用存档管理器
/// 特性：原子写入（先写临时文件再替换）、保留一份.bak备份、版本号校验
/// 落盘位置：Application.persistentDataPath/Save/{slot}.json
/// </summary>
public static class SaveManager
{
	/// <summary>
	/// 当前存档版本号
	/// 注意：修改了存档结构之后必须递增该版本号，并处理旧档兼容
	/// </summary>
	public const int CurrentVersion = 1;

	/// <summary>
	/// 默认存档槽
	/// </summary>
	public const string DefaultSlot = "player";

	private const string FileExtension = ".json";
	private const string TempExtension = ".tmp";
	private const string BackupExtension = ".bak";

	/// <summary>
	/// 存档版本不匹配回调（存档槽，文件版本，当前版本）
	/// </summary>
	public static event Action<string, int, int> OnVersionMismatch;

	/// <summary>
	/// 自动存档请求回调
	/// </summary>
	public static event Action OnRequestSave;

	/// <summary>
	/// 存档根目录
	/// </summary>
	public static string SaveDirectory
	{
		get { return Path.Combine(Application.persistentDataPath, "Save"); }
	}

	/// <summary>
	/// 获取存档文件路径
	/// </summary>
	public static string GetSaveFilePath(string slot)
	{
		return Path.Combine(SaveDirectory, SanitizeSlot(slot) + FileExtension);
	}

	/// <summary>
	/// 存档是否存在
	/// </summary>
	public static bool Exists(string slot)
	{
		return File.Exists(GetSaveFilePath(slot));
	}

	/// <summary>
	/// 保存对象（Unity的JsonUtility，要求类型带[Serializable]）
	/// </summary>
	public static bool Save<T>(string slot, T data)
	{
		if (data == null)
		{
			Debug.LogError($"Save data is null. Slot : {slot}");
			return false;
		}

		return SaveJson(slot, JsonUtility.ToJson(data));
	}

	/// <summary>
	/// 保存JSON字符串
	/// </summary>
	public static bool SaveJson(string slot, string payload)
	{
		if (string.IsNullOrEmpty(payload))
		{
			Debug.LogError($"Save payload is null or empty. Slot : {slot}");
			return false;
		}

		SaveEnvelope envelope = new SaveEnvelope();
		envelope.Version = CurrentVersion;
		envelope.SaveTicks = DateTime.UtcNow.Ticks;
		envelope.Payload = payload;
		string json = JsonUtility.ToJson(envelope);

		return WriteFile(GetSaveFilePath(slot), json);
	}

	/// <summary>
	/// 读取对象
	/// </summary>
	public static bool Load<T>(string slot, out T data)
	{
		data = default(T);
		string payload;
		if (LoadJson(slot, out payload) == false)
			return false;

		try
		{
			data = JsonUtility.FromJson<T>(payload);
			return data != null;
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed to parse save file. Slot : {slot}, Error : {ex}");
			return false;
		}
	}

	/// <summary>
	/// 读取对象（失败时返回默认值）
	/// </summary>
	public static T Load<T>(string slot, T defaultValue)
	{
		T data;
		if (Load<T>(slot, out data))
			return data;
		return defaultValue;
	}

	/// <summary>
	/// 读取JSON字符串
	/// </summary>
	public static bool LoadJson(string slot, out string payload)
	{
		payload = null;
		string filePath = GetSaveFilePath(slot);
		if (File.Exists(filePath) == false)
			return false;

		try
		{
			string json = File.ReadAllText(filePath, Encoding.UTF8);
			SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(json);
			if (envelope == null || string.IsNullOrEmpty(envelope.Payload))
			{
				Debug.LogError($"Save file is invalid. Slot : {slot}");
				return false;
			}

			// 注意：高版本存档不允许被低版本程序读取，避免数据被破坏
			if (envelope.Version > CurrentVersion)
			{
				Debug.LogError($"Save file version is newer than program. Slot : {slot}, File : {envelope.Version}, Program : {CurrentVersion}");
				return false;
			}

			if (envelope.Version != CurrentVersion)
			{
				// 先备份旧档，迁移失败时数据仍可人工恢复
				TryBackup(filePath);

				if (OnVersionMismatch != null)
					OnVersionMismatch.Invoke(slot, envelope.Version, CurrentVersion);

				// 版本不一致视为读取失败：交由上层使用默认数据，
				// 否则缺字段会被 JsonUtility 静默填成默认值，等于悄悄丢进度
				Debug.LogWarning($"Save file version mismatch, fallback to default data. Slot : {slot}, File : {envelope.Version}, Program : {CurrentVersion}");
				return false;
			}

			payload = envelope.Payload;
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed to read save file. Slot : {slot}, Error : {ex}");
			return false;
		}
	}

	/// <summary>
	/// 删除存档（同时删除备份）
	/// </summary>
	public static bool Delete(string slot)
	{
		string filePath = GetSaveFilePath(slot);
		string backupPath = filePath + BackupExtension;
		bool result = false;

		try
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
				result = true;
			}
			if (File.Exists(backupPath))
				File.Delete(backupPath);
			return result;
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed to delete save file. Slot : {slot}, Error : {ex}");
			return false;
		}
	}

	/// <summary>
	/// 获取所有存档槽名称
	/// </summary>
	public static string[] GetSlots()
	{
		if (Directory.Exists(SaveDirectory) == false)
			return new string[0];

		List<string> result = new List<string>();
		string[] files = Directory.GetFiles(SaveDirectory, "*" + FileExtension);
		for (int i = 0; i < files.Length; i++)
		{
			result.Add(Path.GetFileNameWithoutExtension(files[i]));
		}
		result.Sort();
		return result.ToArray();
	}

	/// <summary>
	/// 请求自动存档
	/// </summary>
	public static void RequestSave()
	{
		if (OnRequestSave != null)
			OnRequestSave.Invoke();
	}

	#region 内部实现
	/// <summary>
	/// 尝试把存档文件复制一份为 .bak（失败只记录日志，不阻断流程）
	/// </summary>
	private static void TryBackup(string filePath)
	{
		try
		{
			string backupPath = filePath + BackupExtension;
			if (File.Exists(backupPath))
				File.Delete(backupPath);
			File.Copy(filePath, backupPath, true);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed to backup save file. Path : {filePath}, Error : {ex}");
		}
	}

	private static bool WriteFile(string filePath, string content)
	{
		string directory = Path.GetDirectoryName(filePath);
		string tempPath = filePath + TempExtension;
		string backupPath = filePath + BackupExtension;

		try
		{
			if (Directory.Exists(directory) == false)
				Directory.CreateDirectory(directory);

			// 先写入临时文件，避免写一半断电导致存档损坏
			File.WriteAllText(tempPath, content, new UTF8Encoding(false));

			if (File.Exists(filePath))
			{
				if (File.Exists(backupPath))
					File.Delete(backupPath);

				try
				{
					File.Replace(tempPath, filePath, backupPath, true);
				}
				catch (Exception)
				{
					// 部分平台不支持 File.Replace，退化为两次改名（都是原子操作）
					// 注意：绝不能用 File.Copy 覆盖正式档，否则写一半断电会同时毁掉新旧两代存档
					File.Move(filePath, backupPath);
					try
					{
						File.Move(tempPath, filePath);
					}
					catch
					{
						// 回滚，保证至少存在一份可用存档
						File.Move(backupPath, filePath);
						throw;
					}
				}
			}
			else
			{
				File.Move(tempPath, filePath);
			}

			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed to write save file : {filePath}, Error : {ex}");
			return false;
		}
	}

	private static string SanitizeSlot(string slot)
	{
		if (string.IsNullOrEmpty(slot))
			return DefaultSlot;

		char[] invalidChars = Path.GetInvalidFileNameChars();
		StringBuilder builder = new StringBuilder(slot.Length);
		for (int i = 0; i < slot.Length; i++)
		{
			char c = slot[i];
			if (Array.IndexOf(invalidChars, c) >= 0)
				builder.Append('_');
			else
				builder.Append(c);
		}
		return builder.ToString();
	}
	#endregion
}
