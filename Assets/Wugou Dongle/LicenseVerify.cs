using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using Wugou.Verify;

/// <summary>
/// 许可证验证
/// </summary>
public class LicenseVerify : MonoBehaviour
{
    public DongleDaemonPage donglePage;
    // Start is called before the first frame update
    void Start()
    {
        if (!RSADongle.Verify($"./Config/License/EmergencyFire.dlf", $"./Config/verify/RSA.private"))
        {
            donglePage.Show();

            StartCoroutine(DelayQuit());
        }
    }

    IEnumerator DelayQuit()
    {
        yield return new WaitForSeconds(5);
        Application.Quit();
    }

    // Update is called once per frame
    //void Update()
    //{
        
    //}
}
