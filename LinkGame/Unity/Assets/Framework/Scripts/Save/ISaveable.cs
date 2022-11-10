using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
	public interface ISaveable
	{
		string SaveId { get; }
		Dictionary<string, object> Save();
	}
}
