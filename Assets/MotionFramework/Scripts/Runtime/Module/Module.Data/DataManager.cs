using System;
using System.Collections;
using System.Collections.Generic;
using MotionFramework;
using MotionFramework.CollectAttribute;
using UnityEngine;

namespace MotionFramework.Data
{
    public class DataManager : ModuleSingleton<DataManager>, IModule
    {
        private readonly List<DataBase> _allDatas = new List<DataBase>();

        void IModule.OnCreate(object createParam)
        {
            // 创建所有数据类
            var types = AssemblyManager.Instance.GetDataTypes();

			foreach (var type in types)
			{
                var data = CreateData(type);
                _allDatas.Add(data);
			}

            for (int i = 0; i < _allDatas.Count; i++)
            {
                _allDatas[i].Create();
            }
        }

        void IModule.OnUpdate()
        {
            for (int i = 0; i < _allDatas.Count; i++)
            {
                _allDatas[i].Update();
            }
        }
        void IModule.OnGUI()
        {
        }

        public void CreateAll()
		{

		}

        public void ClearAll()
        {
            for (int i = 0; i < _allDatas.Count; i++)
            {
                _allDatas[i].Destroy();
            }
            _allDatas.Clear();
        }

        public T GetData<T>() where T : DataBase
        {
            for (int i = 0; i < _allDatas.Count; i++)
            {
                if (_allDatas[i].GetType() == typeof(T))
                {
                    return _allDatas[i] as T;
                }
            }
            return null;
        }

        private DataBase CreateData(Type dataType)
		{
            DataAttribute attr = AssemblyManager.Instance.GetDataAttribute(dataType);
            if (attr.IsSave)
			{
                var jsonStr = PlayerPrefs.GetString(attr.LocalDataKey, "");
                if (!string.IsNullOrEmpty(jsonStr))
				{
                    try
					{
                        object sData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr, dataType);
                        if (sData is DataBase baseData)
						{
                            return baseData;
						}
                        return Activator.CreateInstance(dataType) as DataBase;
					}
                    catch
					{
                        return Activator.CreateInstance(dataType) as DataBase;
                    }
				}
			}

            return Activator.CreateInstance(dataType) as DataBase;
        }

    }
}