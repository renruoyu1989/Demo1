using MotionFramework;
using MotionFramework.Config;
using MotionFramework.Console;
using MotionFramework.Data;
using MotionFramework.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MotionFramework.CollectAttribute
{
	public class AssemblyManager : ModuleSingleton<AssemblyManager>, IModule
	{
		protected readonly Dictionary<Type, DataAttribute> dataAttributes = new Dictionary<Type, DataAttribute>();

		protected readonly Dictionary<Type, ConfigAttribute> cfgAttributes = new Dictionary<Type, ConfigAttribute>();

		protected readonly Dictionary<Type, ConfigAttribute> tabAttributes = new Dictionary<Type, ConfigAttribute>();

		public void OnCreate(object createParam)
		{
			List<Type> gameTypes = new List<Type>();

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				string assemblyName = assembly.GetName().Name;
				if (assemblyName == AssemblyUtility.GameScriptAssemblyName)
					gameTypes.AddRange(assembly.GetTypes());
			}

			foreach (var type in gameTypes)
			{
				if (typeof(AssetConfig).IsAssignableFrom(type))
				{
					ConfigAttribute attr = type.GetCustomAttribute<ConfigAttribute>();
					if (attr != null)
					{
						cfgAttributes.Add(type, attr);
					}
					else
					{
						Debug.LogError($"{type.Name}'s Attribute Undefined");
					}
				}
				else if (typeof(ConfigTable).IsAssignableFrom(type))
				{
					ConfigAttribute attr = type.GetCustomAttribute<ConfigAttribute>();
					if (attr != null)
					{
						tabAttributes.Add(type, attr);
					}
					else
					{
						Debug.LogError($"{type.Name}'s Attribute Undefined");
					}
				}
				else if (typeof(DataBase).IsAssignableFrom(type))
				{
					DataAttribute attr = type.GetCustomAttribute<DataAttribute>();
					if (attr != null)
					{
						dataAttributes.Add(type, attr);
					}
					else
					{
						Debug.LogError($"{type.Name}'s Attribute Undefined");
					}
				}
			}
		}

		public void OnGUI()
		{
		}

		public void OnUpdate()
		{

		}

		public List<Type> GetDataTypes()
		{
			List<DataAttribute> dataAttrs = new List<DataAttribute>(dataAttributes.Values);
			dataAttrs.Sort((l, r) => { 
				return l.Priority.CompareTo(r.Priority);
			});

			List<Type> types = new List<Type>();
			foreach (DataAttribute attr in dataAttrs)
			{
				types.Add(attr.DataType);
			}

			return types;
		}

		public DataAttribute GetDataAttribute(Type type)
		{
			if (dataAttributes.ContainsKey(type))
				return dataAttributes[type];
			return null;
		}

		public List<Type> GetConfigTypes()
		{
			return new List<Type>(cfgAttributes.Keys);
		}

		public ConfigAttribute GetConfigAttribute(Type type)
		{
			if (cfgAttributes.ContainsKey(type))
				return cfgAttributes[type];
			return null;
		}

		public ConfigAttribute GetConfigAttributeByTable(Type type)
		{
			if (tabAttributes.ContainsKey(type))
				return tabAttributes[type];
			return null;
		}
	}
}
