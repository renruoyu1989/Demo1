using System.Collections;
using System.Collections.Generic;
using System;

namespace Hotfix
{
	public static class HotfixTypeHelper
	{
		public static T GetAttribute<T>(System.Type type) where T : Attribute
		{
			var attributeArray = type.GetCustomAttributes(typeof(T), false);
			return attributeArray[0] as T;
		}

		/// <summary>
		/// 在业务程序集里查找类型
		/// 注意：业务代码现在直接编译进工程，不再依赖热更程序集
		/// </summary>
		public static Type GetType(string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
				return null;

			Type type = typeof(HotfixTypeHelper).Assembly.GetType("Hotfix." + typeName);
			if (type != null)
				return type;

			// 兼容没有命名空间的情况
			return typeof(HotfixTypeHelper).Assembly.GetType(typeName);
		}

		/// <summary>
		/// 获取业务程序集里所有类型
		/// </summary>
		public static Type[] GetAllTypes()
		{
			return typeof(HotfixTypeHelper).Assembly.GetTypes();
		}
	}
}
