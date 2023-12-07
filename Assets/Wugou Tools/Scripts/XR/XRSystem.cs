
#if WUGOU_XR
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Management;
using Valve.VR;

namespace Wugou.XR
{
    /// <summary>
    /// manual start xr
    /// </summary>
    public class XRSystem
    {
        static XRLoader m_SelectedXRLoader;

        /// <summary>
        /// 启动XR
        /// </summary>
        /// <param name="loaderIndex"></param>
        public static void StartXR(int loaderIndex, UnityAction onStart = null)
        {
            // Once a loader has been selected, prevent the RuntimeXRLoaderManager from
            // losing access to the selected loader
            if (m_SelectedXRLoader == null)
            {
                m_SelectedXRLoader = XRGeneralSettings.Instance.Manager.activeLoaders[loaderIndex];
            }
            CoroutineLauncher.active.StartCoroutine(StartXRCoroutine(onStart));
        }

        static IEnumerator StartXRCoroutine(UnityAction onStart)
        {
            Debug.Log("Init XR loader");

            var initSuccess = m_SelectedXRLoader.Initialize();
            if (!initSuccess)
            {
                Debug.LogError("Error initializing selected loader.");
            }
            else
            {
                yield return null;
                Debug.Log("Start XR loader");
                var startSuccess = m_SelectedXRLoader.Start();
                if (!startSuccess)
                {
                    yield return null;
                    Debug.LogError("Error starting selected loader.");
                    m_SelectedXRLoader.Deinitialize();
                }

                // 回调
                yield return null;
                onStart?.Invoke();
             }
        }

       /// <summary>
       /// 停止XR
       /// </summary>
        public static void StopXR()
        {
            if (!m_SelectedXRLoader)
            {
                return;
            }
            Debug.Log("Stopping XR Loader...");
            m_SelectedXRLoader.Stop();
            m_SelectedXRLoader.Deinitialize();
            m_SelectedXRLoader = null;

            Debug.Log("XR Loader stopped completely.");
        }

        /// <summary>
        /// 获取tracker的device索引
        /// </summary>
        /// <param name="index">第几个tracker</param>
        /// <returns></returns>
        public static uint GetViveTrackerDeviceIndex(uint index=0)
        {
            int ii = 0; 
            var error = ETrackedPropertyError.TrackedProp_Success;
            for (uint i = 0; i < 16; i++)
            {
                var result = new System.Text.StringBuilder((int)64);
                OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref error);
                if (result.ToString().Contains("tracker_vive_3_0"))
                {
                    if(ii == index)
                    {
                        return i;
                    }

                    ++ii;
                }
            }

            return uint.MaxValue;
        }
    }
}

#endif