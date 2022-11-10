using UnityEngine;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace Framework
{
    public class Main : SingletonComponent<Main>
    {
        protected override IEnumerator Initialize()
        {
            DontDestroyOnLoad(this);

            gameObject.AddComponent<UIManager>();

            yield return base.Initialize();
        }
    }
}
