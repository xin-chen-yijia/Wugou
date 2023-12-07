using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// ����Unity���̺߳�ʱ��������������Texture
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
        /// ��ʼִ������
        /// </summary>
        /// <param name="interval">����</param>
        public async void Start(int interval = 20)
        {
            isRunning = true;
            while (isRunning && tasks_.Count > 0)
            {
                var func = tasks_.Dequeue();
                func.Invoke();

                //await Task.Delay(interval);   // ò����web�ϲ�����
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
