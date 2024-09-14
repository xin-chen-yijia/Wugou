using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Wugou.Verify
{
    /// <summary>
    /// 持续的验证加密狗
    /// </summary>
    public class DongleDaemon : MonoBehaviour
    {
        public DongleDaemonPage donglePage;
        public float interval = 5;

        // 启用服务器的加密狗验证
        public bool useServerVerify = false;

        private bool isDongleSucc = false;

        private Socket broadcastSock = null;

        // Start is called before the first frame update
        void Start()
        {
            GameObject.DontDestroyOnLoad(gameObject);

            // 不搞那么复杂的判断了，简单点
            // 1. 开始时有加密狗，开启广播， 持续的验证加密狗，若拔出，则走退出程序，不考虑再次插上的情况，否则状态容易乱
            // 2. 没有加密狗，则开启验证协程，此时不考虑再此插上加密狗
            isDongleSucc = Verify();
            if (isDongleSucc)
            {
                donglePage.Hide();

                // 插上加密狗后应该ServerVerify取消了，但设置了超时，走超时逻辑算了
                if (useServerVerify)
                {
                    StartCoroutine(BroadcastDongle());
                }

                StartCoroutine(ContinuousCheck());  // 持续检测，针对验证过后马上拔出加密狗的情况
            }
            else
            {
                if (useServerVerify)
                {
                    _ = ServerVerify();
                }
            }


        }

        IEnumerator ContinuousCheck()
        {
            while (isDongleSucc)
            {
                yield return new WaitForSeconds(interval);  // 定时检测加密狗是否还在
                isDongleSucc = Verify();
            }

            donglePage.ShowTips("未检测到加密狗，系统将在即将关闭！");
            StartCoroutine(DelayQuit(5));

        }

        IEnumerator DelayQuit(float time)
        {
            yield return new WaitForSeconds(time);

            Logger.DebugInfo("Application Quit.");
            Application.Quit();
        }

        public const int broadcastPort = 10010;
        private const string code = "dongle";

        /// <summary>
        /// 接受有加密狗的务器的广播，也就是局域网内有一个加密狗就可以使用
        /// </summary>
        /// <returns></returns>
        async Task ServerVerify()
        {
            donglePage.ShowTips("未检测到加密狗，正在使用服务器验证！");

            Logger.DebugInfo("Start Server Verify...");

            UdpClient udpClient = new UdpClient(broadcastPort)
            {
                EnableBroadcast = true,
                MulticastLoopback = false
            };

            bool isValid = true;
            while (isValid)
            {
                try
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        // timeout
                        var task = await Task.WhenAny(udpClient.ReceiveAsync(), Task.Delay(1000 * 10, cts.Token)) as Task<UdpReceiveResult>;
                        if (task != null)//如果是Delay先返回，是不能 as Task<UdpReceiveResult>的，task=null。
                        {
                            isValid = Encoding.ASCII.GetString(task.Result.Buffer) == code;
                            if (isValid)
                            {
                                donglePage.Hide();
                            }

                            cts.Cancel();//取消那个Delay，其实也可以不用处理，反正5秒后那家伙就自己去西天了

                            //await Task.Delay(1000 * 5); // 5 秒验证一下, 不能这么干，因为这意味着5秒才处理一次包，因为Socket会缓存包，那么如果服务器发了1000个包，那么就需要5 * 1000秒才能处理完。。。。
                        }
                        else
                        {
                            // timeout
                            isValid = false;
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // socket has been closed
                }
                catch (Exception) { }
            }


            // close
            udpClient.Close();

            donglePage.ShowTips("服务器验证失败，系统将在即将关闭！");
            StartCoroutine(DelayQuit(5));

            Logger.DebugInfo("End Server Verify...");
        }

        IEnumerator BroadcastDongle()
        {
            Logger.DebugInfo("Broadcast Dongle Message...");

            IPEndPoint iep = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            broadcastSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            broadcastSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            broadcastSock.EnableBroadcast = true;


            var data = Encoding.ASCII.GetBytes(code);

            string hostname = Dns.GetHostName();
            while (isDongleSucc)
            {
                broadcastSock.SendTo(data, iep);

                yield return new WaitForSeconds(1.0f);
            }

            broadcastSock.Close();
            broadcastSock = null;
            Logger.DebugInfo("End Broadcast Dongle Message...");
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        protected virtual bool Verify()
        {
            return Dongle.Verify("2E31DCDE");
        }

        private void OnApplicationQuit()
        {
            isDongleSucc = false;
        }
    }
}
