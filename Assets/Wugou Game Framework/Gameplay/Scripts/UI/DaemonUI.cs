using System.Security.AccessControl;
using UnityEngine;

namespace Wugou.UI
{
    public static class DaemonUI
    {
        public const string prefabName = "UI/Daemon UI";

        private static UIRootWindow _Instance = null;
        public static UIRootWindow Instance
        {
            get
            {
                if (_Instance == null)
                {
                    var pfb = Resources.Load<GameObject>(prefabName);
                    if (!pfb)
                    {
                        Logger.Error($"Threre no prefab {prefabName} in resources..");
                    }
                    else
                    {
                        var obj = GameObject.Instantiate<GameObject>(pfb);
                        GameObject.DontDestroyOnLoad(obj);
                        _Instance = obj.GetComponent<UIRootWindow>();
                    }

                }
                return _Instance;
            }
        }

        /// <summary>
        /// 是否创建了Instance
        /// 不用Instance的原因是Instance会自动创建实例
        /// </summary>
        public static bool isInitialized =>  _Instance != null;

        public static void HideAllWindow()
        {
            for(int i=0;i<Instance.transform.childCount;i++)
            {
                Instance.transform.GetChild(i).GetComponent<UIBaseWindow>()?.Hide();
            }
        }

        public static LoadingScenePage loadingPage => Instance.GetChildWindow<LoadingScenePage>();

        public static MakeSurePage makeSurePage => Instance.GetChildWindow<MakeSurePage>();

        public static FadeOutTipsPage fadeOutTipsPage => Instance.GetChildWindow<FadeOutTipsPage>();

        public static HoverTipsPage hoverTipsPage => Instance.GetChildWindow<HoverTipsPage>();
    }
}
