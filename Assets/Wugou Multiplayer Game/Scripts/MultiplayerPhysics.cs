//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Mirror;

//namespace Wugou.Multiplayer
//{
//    /// <summary>
//    /// 实现手动物理更新，主要用于网络同步的情况下，需要一帧更新多次物理状态的情况
//    /// </summary>
//    public class MultiplayerPhysics : MonoBehaviour
//    {
//        private static MultiplayerPhysics _instance;
//        public static MultiplayerPhysics instance
//        {
//            get
//            {
//                if(_instance == null)
//                {
//                    GameObject go = new GameObject("MultiplayerPhysics");
//                    GameObject.DontDestroyOnLoad(go);
//                    _instance = go.AddComponent<MultiplayerPhysics>();
//                }

//                return _instance;
//            }
//        }

//        class TickAction : IComparable
//        {
//            public double time;
//            public System.Action action;

//            public int CompareTo(object obj)
//            {
//                var other = obj as TickAction;
//                if(other != null )
//                {
//                    if(time > other.time)
//                    {
//                        return 1;
//                    }
//                    else if(time < other.time)
//                    {
//                        return -1;
//                    }

//                    return 0;
//                }

//                return 1;
//            }
//        }

//        private SortedSet<TickAction> actions = new SortedSet<TickAction>();

//        // Start is called before the first frame update
//        void Start()
//        {
//            Logger.DebugInfo("Physics.autoSimulation false...");
//            Physics.autoSimulation = false;
//        }

//        private float timer_;

//        private float fpsTime;
//        private int fpsCount;
//        private int fps;

//        // 0424: 一开始使用Update，人物移动在编辑器中看起来不那么平滑，编辑器的帧率较高
//        // 但我们在Update中也是以Time.fixedDeltaTime的速率去追的，甚至Update的帧率还要高于50（默认的FixedUpdate帧率），那么为什么用Update会不平滑呢？
//        // 原因：Time.deltaTime可能小于Time.fixedDeltaTime。。。。。
//        void Update()
//        {
//            fpsCount++;
//            fpsTime += Time.deltaTime;
//            if(fpsCount >= 60)
//            {
//                fps = (int)(1.0f / (fpsTime / fpsCount));
//                fpsCount = 0;
//                fpsTime = 0;
//            }

//            if (Input.GetKeyDown(KeyCode.F1))
//            {
//                Wugou.UI.DaemonUI.makeSurePage.Tips($"fps:{fps}");
//            }

//            if (MultiplayerGameManager.instance.mode != NetworkManagerMode.Host)
//            {
//                Logger.Error("MultiplayerPhysics not running on the server...");
//            }

//            while (actions.Count > 0 && timer_ + Time.fixedDeltaTime < NetworkTime.time)
//            {
//                timer_ += Time.fixedDeltaTime;
//                foreach (var action in actions)
//                {
//                    if (action.time < timer_)
//                    {
//                        action.action();
//                    }
//                }

//                Physics.Simulate(Time.fixedDeltaTime);

//                actions.RemoveWhere((action) =>
//                {
//                    return action.time < timer_;
//                });

//            } 

//        }

//        public void AddAction(double time, System.Action action)
//        {
//            actions.Add(new TickAction()
//            {
//                time = time, action = action
//            });
//        }

//        /// <summary>
//        /// 清除它
//        /// </summary>
//        public static void Release()
//        {
//            if (_instance)
//            {
//                GameObject.Destroy(_instance);
//                _instance = null;
//            }
//        }
//    }
//}

