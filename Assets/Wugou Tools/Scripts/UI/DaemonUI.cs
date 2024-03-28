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
                CreateInstance();
                return _Instance;
            }
        }


        private static void CreateInstance()
        {
            if (_Instance == null)
            {
                var pfb = Resources.Load<GameObject>(prefabName);
                if (!pfb)
                {
                    Logger.Error($"Threre no prefab {prefabName} in resources..");
                    return;
                }
                var obj = GameObject.Instantiate<GameObject>(pfb);
                GameObject.DontDestroyOnLoad(obj);
                _Instance = obj.GetComponent<UIRootWindow>();
            }

        }

        public static LoadingScenePage loadingPage => Instance.GetChildWindow<LoadingScenePage>();

        public static MakeSurePage makeSurePage => Instance.GetChildWindow<MakeSurePage>();
    }
}
