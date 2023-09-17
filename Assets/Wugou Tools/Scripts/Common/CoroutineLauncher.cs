using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public class CoroutineLauncher : MonoBehaviour
    {
        // // Start is called before the first frame update
        // void Start()
        // {
            
        // }

        // // Update is called once per frame
        // void Update()
        // {
            
        // }

        private static CoroutineLauncher active_ = null;
        public static CoroutineLauncher active
        {
            get
            {
                if(active_ == null)
                {
                    GameObject obj = new GameObject("CoroutineLauncher");
                    active_ = obj.AddComponent<CoroutineLauncher>();
                    DontDestroyOnLoad(obj);
                }

                return active_;
            }
        }

    }
}

