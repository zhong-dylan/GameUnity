using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    public class Loader : MonoBehaviour
    {
        private Dictionary<string, AsyncOperationHandle> _resourceDic = new Dictionary<string, AsyncOperationHandle>();

        public IEnumerator InstantiateAsyncCorutine(string key, GameObject parent, Action<object> callback = null)
        {
            bool done = false;
            InstantiateAsync(key, parent, (go)=> {
                done = true;
                callback?.Invoke(go);
            });

            while(done == false)
            {
                yield return null;
            }
        }

        public void InstantiateAsync(string key, GameObject parent, Action<object> callback = null)
        {
            GetAssetAsync(key, (go)=> {
                var go_result = Instantiate(go as GameObject);
                go_result.transform.SetParent(parent.transform);
                callback?.Invoke(go_result);
            });
        }

        public void GetAssetAsync(string key, Action<object> callback)
        {
            if(_resourceDic.ContainsKey(key))
            {
                callback?.Invoke(_resourceDic[key].Result);
            }
            else
            {
                LoadAssetAsync(key, callback);
            }
        }

        public void LoadAssetAsync(string key, Action<object> callback)
        {
            var handleCom = Addressables.LoadAssetAsync<object>(key);
            handleCom.Completed += (handle) =>
            {
                callback?.Invoke(handle.Result);
                _resourceDic[key] = handle;
            };
        }

        public void OnDestroy()
        {
            foreach(var item in _resourceDic)
            {
                Addressables.Release(item.Value);
            }
        }
    }
}
