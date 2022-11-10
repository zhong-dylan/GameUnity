using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Framework
{
	public class NotEnoughCurrencyPopup : Popup
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private Text		titleText			= null;
		[SerializeField] private Text		messageText			= null;
		[SerializeField] private Text		rewardAdButtonText	= null;
		[SerializeField] private GameObject rewardAdButton		= null;
		[SerializeField] private GameObject storeButton			= null;
		[SerializeField] private GameObject buttonsContainer	= null;

		#endregion

		#region Member Variables

		private string	currencyId;
		private int		rewardAmount;

		#endregion

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);
		}

		public void OnRewardAdButtonClick()
		{
			Hide(false);
		}
	}
}
