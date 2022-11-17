/*
 *  (c) 2015 HEADLOCK INC.
 */
using UnityEngine;
using System;
using System.Text;

namespace Framework
{
	public class ContainerFinder
	{
		/// <summary>
		///   Itemの検索用
		/// </summary>
		/// <param name="array">検索対象の配列</param>
		/// <param name="key">検索するためのキー</param>
		/// <returns></returns>
		static public IntKeyContainerBase.ItemBase GetItem(IntKeyContainerBase.ItemBase[] array, int key)
		{
			int n = array.Length;
			if (n == 0)
				return null;

			int s = 0;
			int e = n - 1;

			if (array[s].key > key)
				return null;
			else if (array[e].key < key)
				return null;

			if (array[s].key == key)
				return array[s];
			else if (array[e].key == key)
				return array[e];

			while ((e - s) >= 5)
			{
				int c = ((e - s) >> 1) + s;

				if (array[c].key == key)
					return array[c];
				else if (array[c].key > key)
					e = c;
				else
					s = c;
			}

			for (; s < e; ++s)
			{
				if (array[s].key == key)
					return array[s];
			}

			return null;
		}


		/// <summary>
		///   Itemの検索用
		/// </summary>
		/// <param name="array">検索対象の配列</param>
		/// <param name="key">検索するためのキー</param>
		/// <returns></returns>
		static public NotValueKeyContainerBase<TKey>.ItemBase GetItem<TKey>(NotValueKeyContainerBase<TKey>.ItemBase[] array, int[] hashs, TKey key) where TKey : class
		{
#if UNITY_EDITOR
			if (typeof(TKey).IsValueType == true)
				UnityEngine.Debug.LogWarning("TKeyが値型です。GCAllocが発生します。");
#endif
			int hash = key.GetHashCode();
			int index = Array.BinarySearch(hashs, hash);
			if (index < 0)
				return null;

			int n = array.Length;
			if (index < n)
			{
				for (int i = index; i >= 0 && hashs[i] == hash; --i)
				{
					if (array[i].key.Equals(key))
						return array[i];
				}
				for (int i = index + 1; i < n && hashs[i] == hash; ++i)
				{
					if (array[i].key.Equals(key))
						return array[i];
				}
			}
			return null;
		}

		/// <summary>
		///  インデックス検索
		/// </summary>
		/// <param name="key"></param>
		/// <param name="keyArray"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		static public int GetIndex(int key, int[] keyArray, int offset, int length)
		{
			int n = keyArray.Length;
			if (n == 0)
				return -1;

			int s = offset;
			int e = s + length - 1;

			if (keyArray[s] > key)
				return -1;
			else if (keyArray[e] < key)
				return -1;

			if (keyArray[s] == key)
				return s;
			else if (keyArray[e] == key)
				return e;

			while ((e - s) >= 5)
			{
				int c = ((e - s) >> 1) + s;

				if (keyArray[c] == key)
					return c;
				else if (keyArray[c] > key)
					e = c;
				else
					s = c;
			}

			for (; s < e; ++s)
			{
				if (keyArray[s] == key)
					return s;
			}

			return -1;
		}

		/// <summary>
		///  インデックス検索
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="key"></param>
		/// <param name="array"></param>
		/// <param name="hashs"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		static public int GetIndex<TKey>(TKey key, TKey[] array, int[] hashs, int offset, int length) where TKey : class
		{
#if UNITY_EDITOR
			if (typeof(TKey).IsValueType == true)
				UnityEngine.Debug.LogWarning("TKeyが値型です。GCAllocが発生します。");
#endif
			int hash = key.GetHashCode();
			int index = Array.BinarySearch(hashs, offset, length, hash);
			if (index < 0)
				return -1;

			int n = offset+length;
			if (index < n)
			{
				for (int i = index; i >= 0 && hashs[i] == hash; --i)
				{
					if (array[i].Equals(key))
						return i;
				}
				for (int i = index + 1; i < n && hashs[i] == hash; ++i)
				{
					if (array[i].Equals(key))
						return i;
				}
			}
			return -1;
		}

		static public int GetTruncateIndex(int index, int length, int head, int tail)
		{
			// 切り詰めた先端インデックス
			if (index <= head)
				return 0;

			int _index = index - head;
			int _length = length - head - tail;

			// 切り詰めた終端インデックス
			if (_index > _length - 1)
				return _length - 1;

			return _index;
		}

	}
}
