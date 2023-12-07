using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 用于Unity主线程耗时的情况，比如加载Texture
    /// </summary>
    public class MainThreadTask
    {
        Queue<Action> tasks_ = new Queue<Action>();

        public bool isRunning { get; private set; }

        public void AddTask(Action func)
        {
            tasks_.Enqueue(func);
        }

        /// <summary>
        /// 开始执行任务
        /// </summary>
        /// <param name="interval">毫秒</param>
        public async void Start(int interval = 20)
        {
            isRunning = true;
            while (isRunning && tasks_.Count > 0)
            {
                var func = tasks_.Dequeue();
                func.Invoke();

                //await Task.Delay(interval);   // 貌似在web上不能用
                await new YieldInstructionAwaiter(new WaitForSeconds(20 / 1000.0f));
            }
        }

        public void Stop()
        {
            isRunning = false;
            tasks_.Clear();
        }
    }
}
