/*
 *  (c) 2015 HEADLOCK INC.
 */
using UnityEngine;
using System;

namespace Framework
{
	/// <summary>
	///  IntをKeyとしたScriptableObject用規定クラス
	/// </summary>
	public class IntKeyContainerBase : ScriptableObject
	{
		/// <summary>
		///   CSV一行分のデータのキー付き規定
		/// </summary>
		[Serializable]
		public class ItemBase
		{
			public int key;
		}

		/// <summary>
		///   Itemの検索用
		/// </summary>
		/// <param name="array">検索対象の配列</param>
		/// <param name="key">検索するためのキー</param>
		/// <returns></returns>
		protected ItemBase GetItem(ItemBase[] array, int key)
		{
			return ContainerFinder.GetItem(array, key);
		}
	}
}
