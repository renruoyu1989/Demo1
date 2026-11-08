using System;
using System.Collections;
using System.Collections.Generic;

namespace MotionFramework.Config
{
    /// <summary>
    /// 配表管理器
    /// 支持两种配表来源：
    /// 1. 二进制配表资源（AssetConfig + LoadConfig，原版流程）
    /// 2. 导表工具直接生成的 C# 配表类（由业务层 Register 注册）
    /// </summary>
    public sealed class ConfigManager : ModuleSingleton<ConfigManager>, IModule, IProgress
    {
        public class LoadPair
        {
            public Type ClassType;
            public string Location;
            public LoadPair(Type classType, string location)
            {
                ClassType = classType;
                Location = location;
            }
        }

        /// <summary>
        /// 二进制配表集合（以类型全名为键）
        /// </summary>
        private readonly Dictionary<string, AssetConfig> _configs = new Dictionary<string, AssetConfig>();

        /// <summary>
        /// 代码生成配表集合（以类型为键）
        /// </summary>
        private readonly Dictionary<Type, object> _generatedConfigs = new Dictionary<Type, object>();

        private int maxCfgNum;
        private int loadNum;

        void IModule.OnCreate(System.Object param)
        {
        }

        void IModule.OnUpdate()
        {
        }

        void IModule.OnGUI()
        {
        }

        #region 代码生成配表
        /// <summary>
        /// 注册一个代码生成的配表对象
        /// </summary>
        public void Register(object config)
        {
            if (config == null)
            {
                MotionLog.Error("Config is null.");
                return;
            }

            Type type = config.GetType();
            if (_generatedConfigs.ContainsKey(type))
            {
                MotionLog.Warning($"Config {type.FullName} is already registered.");
                return;
            }

            _generatedConfigs.Add(type, config);
        }

        /// <summary>
        /// 获取代码生成的配表
        /// </summary>
        public T GetGeneratedConfig<T>() where T : class
        {
            object config;
            if (_generatedConfigs.TryGetValue(typeof(T), out config))
                return config as T;

            MotionLog.Error($"Not found config {typeof(T).FullName}");
            return null;
        }
        public object GetGeneratedConfig(Type configType)
        {
            object config;
            if (_generatedConfigs.TryGetValue(configType, out config))
                return config;

            MotionLog.Error($"Not found config {configType.FullName}");
            return null;
        }
        #endregion

        #region 二进制配表
        /// <summary>
        /// 按照列表顺序批量加载配表
        /// </summary>
        public IEnumerator LoadConfigs(List<LoadPair> loadPairs)
        {
            maxCfgNum = loadPairs.Count;
            loadNum = 0;

            for (int i = 0; i < loadPairs.Count; i++)
            {
                Type type = loadPairs[i].ClassType;
                string location = loadPairs[i].Location;
                AssetConfig config = LoadConfig(type, location);
                yield return config;
                loadNum++;
            }
        }

        public AssetConfig LoadConfig(Type configType, string location)
        {
            string configName = configType.FullName;

            // 防止重复加载
            if (_configs.ContainsKey(configName))
            {
                MotionLog.Error($"Config {configName} is already existed.");
                return null;
            }

            // 注意：配表类通常是私有构造，需要用非公开构造创建
            AssetConfig config = Activator.CreateInstance(configType, true) as AssetConfig;
            if (config == null)
            {
                MotionLog.Error($"Config {configName} create instance failed.");
            }
            else
            {
                config.Load(location);
                _configs.Add(configName, config);
            }

            return config;
        }

        /// <summary>
        /// 获取配表
        /// </summary>
        public T GetConfig<T>() where T : AssetConfig
        {
            return GetConfig(typeof(T)) as T;
        }

        public AssetConfig GetConfig(Type configType)
        {
            return GetConfig(configType.FullName);
        }

        /// <summary>
        /// 获取配表
        /// </summary>
        public AssetConfig GetConfig(string configName)
        {
            AssetConfig config;
            if (_configs.TryGetValue(configName, out config))
                return config;

            MotionLog.Error($"Not found config {configName}");
            return null;
        }

        public float GetProgress()
        {
            // 没有配表时视为已完成，避免 0/0 得到 NaN
            if (maxCfgNum <= 0)
                return 1f;

            float progress = loadNum * 1f / maxCfgNum;
            return progress > 1f ? 1f : progress;
        }

        public bool IsDone()
        {
            return loadNum >= maxCfgNum;
        }
        #endregion
    }
}
