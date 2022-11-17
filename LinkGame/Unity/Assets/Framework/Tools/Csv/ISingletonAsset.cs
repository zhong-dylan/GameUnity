/*
 *  (c) 2015 HEADLOCK INC.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
	/// <summary>
	/// シングルトンアセットインターフェイス
	/// </summary>
	public interface ISingletonAsset
	{
		void _SetInstance();
		void _ClearInstance();
	}

	// ISingletonAssetを継承したら、下記のようなコードを書いておく
	/*
		/// <summary>
		/// for ISingletonAsset
		/// </summary>
		public static Hoge instance { get; private set; }
		public void _SetInstance() { instance = this; }
		public void _ClearInstance() { if (instance == this) instance = null; }
	*/
}
