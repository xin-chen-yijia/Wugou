using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.UI
{
    /// <summary>
    /// 用于只显示一个窗口的情况
    /// </summary>
    public class UIWindowsGroup : MonoBehaviour
    {
        private List<UIBaseWindow> windows_ = new List<UIBaseWindow>();

        //// Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void NotifyWindowShow(UIBaseWindow window)
        {
            for(int i=0;i < windows_.Count;i++) 
            {
                if(windows_[i] != window)
                {
                    windows_[i].Hide();
                }
            }
        }

        public void UnregisterWindow(UIBaseWindow window)
        {
            if (windows_.Contains(window))
            {
                windows_.Remove(window);
            }
        }

        public void RegisterWindow(UIBaseWindow window)
        {
            if (!windows_.Contains(window))
            {
                windows_.Add(window);
            }
        }
    }
}

