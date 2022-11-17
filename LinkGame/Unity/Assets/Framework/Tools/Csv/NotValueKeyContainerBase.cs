/*
 *  (c) 2015 HEADLOCK INC.
 */
using UnityEngine;
using System;

namespace Framework
{
	/// <summary>
	///   ValueType以外のkeyが指定されているScriptableObject用規定クラス
	/// </summary>
	public class NotValueKeyContainerBase<TKey> : ScriptableObject where TKey : class
	{
		/// <summary>
		///   CSV一行分のデータのキー付き規定
		/// </summary>
		[Serializable]
		public class ItemBase
		{
			public TKey key;
		}

		/// <summary>
		///   検索用のhash配列
		/// </summary>
		public int[] hashs;

		/// <summary>
		///   Itemの検索用
		/// </summary>
		/// <param name="array">検索対象の配列</param>
		/// <param name="key">検索するためのキー</param>
		/// <returns></returns>
		protected ItemBase GetItem(ItemBase[] array, TKey key)
		{
			return ContainerFinder.GetItem<TKey>(array, hashs, key);
		}
	}
}
