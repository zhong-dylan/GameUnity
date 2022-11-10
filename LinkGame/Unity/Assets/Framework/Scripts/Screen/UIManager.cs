using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using System;

namespace Framework
{ 
    public class UIManager : SingletonComponent<UIManager>
    {
        public GameObject goUI { get; private set; }
        public Loader loader { get; private set; }
        public Dictionary<CONST_DATA.SORT_LAYER, ScreenContext> screenContextDic { get; private set; }
        public Camera uiCamera { get; private set; }

        protected override IEnumerator Initialize()
        {
            screenContextDic = new Dictionary<CONST_DATA.SORT_LAYER, ScreenContext>();

            loader = gameObject.GetOrAddComponent<Loader>();
            loader.InstantiateAsync("prefabs/system/eventsystem.prefab", gameObject);

            yield return loader.InstantiateAsyncCorutine("prefabs/camera/cameraui.prefab", gameObject, (go)=>
            {
                uiCamera = (go as GameObject).GetComponent<Camera>();
            });

            goUI = Utilities.CreateGameObject(gameObject, "UI");

            //create context
            screenContextDic[CONST_DATA.SORT_LAYER.BACKGROUND] = CreateContext(goUI, CONST_DATA.SORT_LAYER.BACKGROUND);
            screenContextDic[CONST_DATA.SORT_LAYER.GAME] = CreateContext(goUI, CONST_DATA.SORT_LAYER.GAME);
            screenContextDic[CONST_DATA.SORT_LAYER.HEAD] = CreateContext(goUI, CONST_DATA.SORT_LAYER.HEAD);
            screenContextDic[CONST_DATA.SORT_LAYER.POPUP] = CreateContext(goUI, CONST_DATA.SORT_LAYER.POPUP);
            screenContextDic[CONST_DATA.SORT_LAYER.EFFECT] = CreateContext(goUI, CONST_DATA.SORT_LAYER.EFFECT);

            AddUI("prefabs/screen/background.prefab", "background", CONST_DATA.SORT_LAYER.BACKGROUND);
            AddUI("prefabs/screen/titlescreen.prefab", "titlescreen", CONST_DATA.SORT_LAYER.GAME);
            AddUI("prefabs/screen/head.prefab", "head", CONST_DATA.SORT_LAYER.HEAD);

            yield return base.Initialize();
        }

        public ScreenContext CreateContext(GameObject go_parent, CONST_DATA.SORT_LAYER sort_layer)
        {
            var go = Utilities.CreateGameObject(go_parent, sort_layer.ToString());
            go.transform.position = new Vector3(0, 0, (int)sort_layer);
            return go.AddComponent<ScreenContext>();
        }

        public void AddUI(string key, string name, CONST_DATA.SORT_LAYER layer, Action<GameObject> callback = null)
        {
            var canvas_go = CreateCanvasObject(name, layer);
            var loader = Utilities.GetOrAddComponent<Loader>(canvas_go);
            loader.InstantiateAsync(key, canvas_go, (go)=> {
                (go as GameObject).transform.localPosition = Vector3.zero;
                (go as GameObject).transform.localScale = Vector3.one;
                callback?.Invoke(go as GameObject);
            });
        }

        public GameObject CreateCanvasObject(string name, CONST_DATA.SORT_LAYER layer)
        {
            var go = Utilities.CreateGameObject(screenContextDic[layer].gameObject, name);
            go.layer = LayerMask.NameToLayer("UI");
            AddCanvas(go, layer);
            return go;
        }

        public void AddCanvas(GameObject go, CONST_DATA.SORT_LAYER layer)
        {
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.sortingOrder = (int)layer;

            var canvas_scaler = go.AddComponent<CanvasScaler>();
            canvas_scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas_scaler.referenceResolution = new Vector2(CONST_DATA.SCREEN_WIDTH, CONST_DATA.SCREEN_HEIGHT);

            var graphic_raycaster = go.AddComponent<GraphicRaycaster>();
        }
    }
}
