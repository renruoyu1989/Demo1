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
			MotionLog.Log(ELogLevel.Error, $"Save data is null. Slot : {slot}");
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
			MotionLog.Log(ELogLevel.Error, $"Save payload is null or empty. Slot : {slot}");
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
			MotionLog.Log(ELogLevel.Error, $"Failed to parse save file. Slot : {slot}, Error : {ex}");
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
				MotionLog.Log(ELogLevel.Error, $"Save file is invalid. Slot : {slot}");
				return false;
			}

			// 注意：高版本存档不允许被低版本程序读取，避免数据被破坏
			if (envelope.Version > CurrentVersion)
			{
				MotionLog.Log(ELogLevel.Error, $"Save file version is newer than program. Slot : {slot}, File : {envelope.Version}, Program : {CurrentVersion}");
				return false;
			}

			if (envelope.Version != CurrentVersion)
			{
				if (OnVersionMismatch != null)
					OnVersionMismatch.Invoke(slot, envelope.Version, CurrentVersion);
			}

			payload = envelope.Payload;
			return true;
		}
		catch (Exception ex)
		{
			MotionLog.Log(ELogLevel.Error, $"Failed to read save file. Slot : {slot}, Error : {ex}");
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
			MotionLog.Log(ELogLevel.Error, $"Failed to delete save file. Slot : {slot}, Error : {ex}");
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
					// 部分平台不支持File.Replace，退化为覆盖写入
					File.Copy(tempPath, filePath, true);
					File.Delete(tempPath);
					File.Copy(filePath, backupPath, true);
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
			MotionLog.Log(ELogLevel.Error, $"Failed to write save file : {filePath}, Error : {ex}");
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
