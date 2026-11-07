//--------------------------------------------------
// ET Framework
// Copyright (c) 2018 tanghai
//--------------------------------------------------
using System;
using System.Collections.Generic;

public class DoubleMap<K, V>
{
	private readonly Dictionary<K, V> _kv = new Dictionary<K, V>();
	private readonly Dictionary<V, K> _vk = new Dictionary<V, K>();

	public DoubleMap()
	{
	}
	public DoubleMap(int capacity)
	{
		_kv = new Dictionary<K, V>(capacity);
		_vk = new Dictionary<V, K>(capacity);
	}

	public void ForEach(Action<K, V> action)
	{
		if (action == null)
		{
			return;
		}
		// 快照遍历：回调里可能增删元素，直接遍历 Keys 会抛"集合已修改"异常
		KeyValuePair<K, V>[] snapshot = new KeyValuePair<K, V>[_kv.Count];
		((ICollection<KeyValuePair<K, V>>)_kv).CopyTo(snapshot, 0);
		for (int i = 0; i < snapshot.Length; i++)
		{
			action(snapshot[i].Key, snapshot[i].Value);
		}
	}

	public List<K> Keys
	{
		get
		{
			return new List<K>(_kv.Keys);
		}
	}
	public List<V> Values
	{
		get
		{
			return new List<V>(_vk.Keys);
		}
	}

	public void Add(K key, V value)
	{
		if (key == null || value == null)
			throw new ArgumentNullException(nameof(key), "DoubleMap 的 key/value 不允许为 null");

		// 重复添加属于调用方逻辑错误，直接抛异常而不是静默丢弃（静默会掩盖数据不一致）
		if (_kv.ContainsKey(key))
			throw new ArgumentException($"Key already exists : {key}", nameof(key));
		if (_vk.ContainsKey(value))
			throw new ArgumentException($"Value already exists : {value}", nameof(value));

		_kv.Add(key, value);
		_vk.Add(value, key);
	}

	/// <summary>
	/// 尝试添加（重复时不抛异常，返回 false）
	/// </summary>
	public bool TryAdd(K key, V value)
	{
		if (key == null || value == null || _kv.ContainsKey(key) || _vk.ContainsKey(value))
			return false;

		_kv.Add(key, value);
		_vk.Add(value, key);
		return true;
	}

	public V GetValueByKey(K key)
	{
		if (key != null && _kv.ContainsKey(key))
		{
			return _kv[key];
		}
		return default(V);
	}
	public K GetKeyByValue(V value)
	{
		if (value != null && _vk.ContainsKey(value))
		{
			return _vk[value];
		}
		return default(K);
	}

	public void RemoveByKey(K key)
	{
		if (key == null)
		{
			return;
		}
		V value;
		if (!_kv.TryGetValue(key, out value))
		{
			return;
		}

		_kv.Remove(key);
		_vk.Remove(value);
	}
	public void RemoveByValue(V value)
	{
		if (value == null)
		{
			return;
		}

		K key;
		if (!_vk.TryGetValue(value, out key))
		{
			return;
		}

		_kv.Remove(key);
		_vk.Remove(value);
	}

	public void Clear()
	{
		_kv.Clear();
		_vk.Clear();
	}

	public bool ContainsKey(K key)
	{
		if (key == null)
		{
			return false;
		}
		return _kv.ContainsKey(key);
	}
	public bool ContainsValue(V value)
	{
		if (value == null)
		{
			return false;
		}
		return _vk.ContainsKey(value);
	}
	public bool Contains(K key, V value)
	{
		if (key == null || value == null)
		{
			return false;
		}
		return _kv.ContainsKey(key) && _vk.ContainsKey(value);
	}
}