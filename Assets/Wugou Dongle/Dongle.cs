using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using DONGLE_HANDLE = System.UInt64;

namespace Wugou.Verify
{
    internal class Dongle
    {
        /************************************************************************/
        /*                              结构                                    */
        /************************************************************************/
        //RSA公钥格式(兼容1024,2048)
        [StructLayout(LayoutKind.Sequential)]
        public struct RSA_PUBLIC_KEY
        {
            public uint bits;                   // length in bits of modulus        	
            public uint modulus;                  // modulus
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] exponent;       // public exponent
        }
        //RSA私钥格式(兼容1024,2048)
        [StructLayout(LayoutKind.Sequential)]
        public struct RSA_PRIVATE_KEY
        {
            public uint bits;                   // length in bits of modulus        	
            public uint modulus;                  // modulus  
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] publicExponent;       // public exponent
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] exponent;       // public exponent
        }
        //外部ECCSM2公钥格式 ECC(支持bits为192或256)和SM2的(bits为固定值0x8100)公钥格式
        [StructLayout(LayoutKind.Sequential)]
        public struct ECCSM2_PUBLIC_KEY
        {
            public uint bits;                   // length in bits of modulus        	
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] XCoordinate;       // 曲线上点的X坐标
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] YCoordinate;       // 曲线上点的Y坐标
        }
        //外部ECCSM2私钥格式 ECC(支持bits为192或256)和SM2的(bits为固定值0x8100)私钥格式  
        [StructLayout(LayoutKind.Sequential)]
        public struct ECCSM2_PRIVATE_KEY
        {
            public uint bits;                   // length in bits of modulus        	
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] PrivateKey;           // 私钥
        }

        //加密锁信息
        [StructLayout(LayoutKind.Sequential)]
        public struct DONGLE_INFO
        {
            public ushort m_Ver;               //COS版本,比如:0x0201,表示2.01版             	
            public ushort m_Type;              //产品类型: 0xFF表示标准版, 0x00为时钟锁,0x01为带时钟的U盘锁,0x02为标准U盘锁  
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] m_BirthDay;       //出厂日期 
            public uint m_Agent;             //代理商编号,比如:默认的0xFFFFFFFF
            public uint m_PID;               //产品ID
            public uint m_UserID;            //用户ID
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] m_HID;            //8字节的硬件ID
            public uint m_IsMother;          //母锁标志: 0x01表示是母锁, 0x00表示不是母锁
            public uint m_DevType;           //设备类型(PROTOCOL_HID或者PROTOCOL_CCID)
        }
        /*************************文件授权结构***********************************/
        //数据文件授权结构
        [StructLayout(LayoutKind.Sequential)]
        public struct DATA_LIC
        {
            public ushort m_Read_Priv;     //读权限: 0为最小匿名权限，1为最小用户权限，2为最小开发商权限            	
            public ushort m_Write_Priv;    //写权限: 0为最小匿名权限，1为最小用户权限，2为最小开发商权限
        }

        //私钥文件授权结构
        [StructLayout(LayoutKind.Sequential)]
        public struct PRIKEY_LIC
        {
            public uint m_Count;        //可调次数: 0xFFFFFFFF表示不限制, 递减到0表示已不可调用
            public byte m_Priv;         //调用权限: 0为最小匿名权限，1为最小用户权限，2为最小开发商权限
            public byte m_IsDecOnRAM;   //是否是在内存中递减: 1为在内存中递减，0为在FLASH中递减
            public byte m_IsReset;      //用户态调用后是否自动回到匿名态: TRUE为调后回到匿名态 (开发商态不受此限制)
            public byte m_Reserve;      //保留,用于4字节对齐
        }

        //对称加密算法(SM4/TDES)密钥文件授权结构
        [StructLayout(LayoutKind.Sequential)]
        public struct KEY_LIC
        {
            public uint m_Priv_Enc;   //加密时的调用权限: 0为最小匿名权限，1为最小用户权限，2为最小开发商权限
        }


        //可执行文件授权结构
        [StructLayout(LayoutKind.Sequential)]
        public struct EXE_LIC
        {
            public ushort m_Priv_Exe;   //运行的权限: 0为最小匿名权限，1为最小用户权限，2为最小开发商权限
        }

        /****************************文件属性结构********************************/
        //数据文件属性数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct DATA_FILE_ATTR
        {
            public uint m_Size;      //数据文件长度，该值最大为4096
            public DATA_LIC m_Lic;       //授权
        }

        //ECCSM2/RSA私钥文件属性数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct PRIKEY_FILE_ATTR
        {
            public ushort m_Type;       //数据类型:ECCSM2私钥 或 RSA私钥
            public ushort m_Size;       //数据长度:RSA该值为1024或2048, ECC该值为192或256, SM2该值为0x8100
            public PRIKEY_LIC m_Lic;        //授权
        }

        //对称加密算法(SM4/TDES)密钥文件属性数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct KEY_FILE_ATTR
        {
            public uint m_Size;       //密钥数据长度=16
            public KEY_LIC m_Lic;        //授权
        }

        //可执行文件属性数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct EXE_FILE_ATTR
        {
            public EXE_LIC m_Lic;        //授权	
            public ushort m_Len;        //文件长度
        }
        /*************************文件列表结构***********************************/
        //获取私钥文件列表时返回的数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct PRIKEY_FILE_LIST
        {
            public ushort m_FILEID;  //文件ID
            public ushort m_Reserve; //保留,用于4字节对齐
            public PRIKEY_FILE_ATTR m_attr;    //文件属性
        }

        //获取SM4及TDES密钥文件列表时返回的数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct KEY_FILE_LIST
        {
            public ushort m_FILEID;  //文件ID
            public ushort m_Reserve; //保留,用于4字节对齐
            public KEY_FILE_ATTR m_attr;    //文件属性
        }

        //获取数据文件列表时返回的数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct DATA_FILE_LIST
        {
            public ushort m_FILEID;  //文件ID
            public ushort m_Reserve; //保留,用于4字节对齐
            public DATA_FILE_ATTR m_attr;    //文件属性
        }

        //获取可执行文件列表时返回的数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct EXE_FILE_LIST
        {
            public ushort m_FILEID;    //文件ID
            public EXE_FILE_ATTR m_attr;
            public ushort m_Reserve;  //保留,用于4字节对齐
        }

        //下载和列可执行文件时填充的数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct EXE_FILE_INFO
        {
            public ushort m_dwSize;           //可执行文件大小
            public ushort m_wFileID;          //可执行文件ID
            public byte m_Priv;             //调用权限: 0为最小匿名权限，1为最小用户权限，2为最小开发商权限

            public byte[] m_pData;            //可执行文件数据
        }


        //需要发给空锁的初始化数据
        [StructLayout(LayoutKind.Sequential)]
        public struct SON_DATA
        {
            public int m_SeedLen;                 //种子码长度
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string m_SeedForPID;        //产生产品ID和开发商密码的种子码 (最长250个字节)
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 18)]
            public string m_UserPIN;         //用户密码(16个字符的0终止字符串)
            public sbyte m_UserTryCount;            //用户密码允许的最大错误重试次数
            public sbyte m_AdminTryCount;           //开发商密码允许的最大错误重试次数
                                                    //RSA_PRIVATE_KEY m_UpdatePriKey;   //远程升级私钥
            public int m_UserID_Start;            //起始用户ID
        }

        //母锁数据
        [StructLayout(LayoutKind.Sequential)]
        public struct MOTHER_DATA
        {
            public SON_DATA m_Son;                  //子锁初始化数据
            public int m_Count;                //可产生子锁初始化数据的次数 (-1表示不限制次数, 递减到0时会受限)
        }


        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_Enum(ref DONGLE_INFO pDongleInfo, out int pCount);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_Open(ref DONGLE_HANDLE phDongle, int nIndex);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_Close(DONGLE_HANDLE hDongle);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_VerifyPIN(DONGLE_HANDLE hDongle, uint nFlags, byte[] pPIN, out int pRemainCount);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_CreateFile(DONGLE_HANDLE hDongle, uint nFileType, ushort wFileID, uint pFileAttr);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_WriteFile(DONGLE_HANDLE hDongle, uint nFileType, ushort wFileID, short wOffset, byte[] buffer, int nDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_ReadFile(DONGLE_HANDLE hDongle, short wFileID, short wOffset, byte[] buffer, int nDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_ListFile(DONGLE_HANDLE hDongle, uint nFileType, DATA_FILE_LIST[] pFileList, ref int pDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_DeleteFile(DONGLE_HANDLE hDongle, uint nFileType, short wFileID);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_DownloadExeFile(DONGLE_HANDLE hDongle, EXE_FILE_INFO[] pExeFileInfo, int nCount);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_RunExeFile(DONGLE_HANDLE hDongle, short wFileID, byte[] pInOutData, short wInOutDataLen, ref int nMainRet);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_WriteShareMemory(DONGLE_HANDLE hDongle, byte[] pData, int nDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_ReadShareMemory(DONGLE_HANDLE hDongle, byte[] pData);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_WriteData(DONGLE_HANDLE hDongle, int nOffset, byte[] pData, int nDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_ReadData(DONGLE_HANDLE hDongle, int nOffset, byte[] pData, int nDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_LEDControl(DONGLE_HANDLE hDongle, uint nFlag);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_SwitchProtocol(DONGLE_HANDLE hDongle, uint nFlag);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_GetUTCTime(DONGLE_HANDLE hDongle, ref uint pdwUTCTime);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_SetDeadline(DONGLE_HANDLE hDongle, uint dwTime);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_GetDeadline(DONGLE_HANDLE hDongle, ref uint dwTime);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_GenUniqueKey(DONGLE_HANDLE hDongle, int nSeedLen, byte[] pSeed, byte[] pPIDstr, byte[] pAdminPINstr);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_ResetState(DONGLE_HANDLE hDongle);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_ChangePIN(DONGLE_HANDLE hDongle, uint nFlags, byte[] pOldPIN, byte[] pNewPIN, int nTryCount);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_RFS(DONGLE_HANDLE hDongle);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_SetUserID(DONGLE_HANDLE hDongle, uint dwUserID);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_ResetUserPIN(DONGLE_HANDLE hDongle, byte[] pAdminPIN);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_RsaGenPubPriKey(DONGLE_HANDLE hDongle, ushort wPriFileID, ref RSA_PUBLIC_KEY pPubBakup, ref RSA_PRIVATE_KEY pPriBakup);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_RsaPri(DONGLE_HANDLE hDongle, ushort wPriFileID, uint nFlag, byte[] pInData, uint nInDataLen, byte[] pOutData, ref uint pOutDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_RsaPub(DONGLE_HANDLE hDongle, uint nFlag, ref RSA_PUBLIC_KEY pPubKey, byte[] pInData, uint nInDataLen, byte[] pOutData, ref uint pOutDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_TDES(DONGLE_HANDLE hDongle, ushort wKeyFileID, uint nFlag, byte[] pInData, byte[] pOutData, uint nDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_SM4(DONGLE_HANDLE hDongle, ushort wKeyFileID, uint nFlag, byte[] pInData, byte[] pOutData, uint nDataLen);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_DeleteFile(DONGLE_HANDLE hDongle, uint nFileType, ushort wFileID);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_HASH(DONGLE_HANDLE hDongle, uint nFlag, byte[] pInData, uint nDataLen, byte[] pHash);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_LimitSeedCount(DONGLE_HANDLE hDongle, int nCount);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_Seed(DONGLE_HANDLE hDongle, byte[] pSeed, uint nSeedLen, byte[] pOutData);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_EccGenPubPriKey(DONGLE_HANDLE hDongle, ushort wPriFileID, ref ECCSM2_PUBLIC_KEY vPubBakup, ref ECCSM2_PRIVATE_KEY vPriBakup);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_EccSign(DONGLE_HANDLE hDongle, ushort wPriFileID, byte[] pHashData, uint nHashDataLen, byte[] pOutData);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_EccVerify(DONGLE_HANDLE hDongle, ref ECCSM2_PUBLIC_KEY pPubKey, byte[] pHashData, uint nHashDataLen, byte[] pSign);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_SM2GenPubPriKey(DONGLE_HANDLE hDongle, ushort wPriFileID, ref ECCSM2_PUBLIC_KEY pPubBakup, ref ECCSM2_PRIVATE_KEY pPriBakup);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_SM2Sign(DONGLE_HANDLE hDongle, ushort wPriFileID, byte[] pHashData, uint nHashDataLen, byte[] pOutData);
        [DllImport("Dongle_d.dll")]
        static extern uint Dongle_SM2Verify(DONGLE_HANDLE hDongle, ref ECCSM2_PUBLIC_KEY pPubKey, byte[] pHashData, uint nHashDataLen, byte[] pSign);

        public const uint DONGLE_SUCCESS = 0x00000000;          // 操作成功
        public const uint DONGLE_NOT_FOUND = 0xF0000001;          // 未找到指定的设备
        public const uint DONGLE_INVALID_HANDLE = 0xF0000002;		   // 无效的句柄
        public const uint DONGLE_INVALID_PARAMETER = 0xF0000003;		   // 参数错误
        public const uint DONGLE_COMM_ERROR = 0xF0000004;		   // 通讯错误
        public const uint DONGLE_INSUFFICIENT_BUFFER = 0xF0000005;		   // 缓冲区空间不足
        public const uint DONGLE_NOT_INITIALIZED = 0xF0000006;		   // 产品尚未初始化 (即没设置PID)
        public const uint DONGLE_ALREADY_INITIALIZED = 0xF0000007;		   // 产品已经初始化 (即已设置PID)
        public const uint DONGLE_ADMINPIN_NOT_CHECK = 0xF0000008;		   // 开发商密码没有验证
        public const uint DONGLE_USERPIN_NOT_CHECK = 0xF0000009;		   // 用户密码没有验证
        public const uint DONGLE_INCORRECT_PIN = 0xF000FF00;		   // 密码不正确 (后2位指示剩余次数)
        public const uint DONGLE_PIN_BLOCKED = 0xF000000A;		   // PIN码已锁死
        public const uint DONGLE_ACCESS_DENIED = 0xF000000B;		   // 访问被拒绝 
        public const uint DONGLE_FILE_EXIST = 0xF000000E;		   // 文件已存在
        public const uint DONGLE_FILE_NOT_FOUND = 0xF000000F;		   // 未找到指定的文件
        public const uint DONGLE_READ_ERROR = 0xF0000010;          // 读取数据错误
        public const uint DONGLE_WRITE_ERROR = 0xF0000011;          // 写入数据错误
        public const uint DONGLE_FILE_CREATE_ERROR = 0xF0000012;          // 创建文件错误
        public const uint DONGLE_FILE_READ_ERROR = 0xF0000013;          // 读取文件错误
        public const uint DONGLE_FILE_WRITE_ERROR = 0xF0000014;          // 写入文件错误
        public const uint DONGLE_FILE_DEL_ERROR = 0xF0000015;          // 删除文件错误
        public const uint DONGLE_FAILED = 0xF0000016;          // 操作失败
        public const uint DONGLE_CLOCK_EXPIRE = 0xF0000017;          // 加密锁时钟到期
        public const uint DONGLE_ERROR_UNKNOWN = 0xFFFFFFFF;		   // 未知的错误

        public void GetDogInfo()
        {
            uint ret = 0;
            int pCount = 0;
            DONGLE_INFO pDongleInfo = new DONGLE_INFO();
            DONGLE_HANDLE hDongle = 0;
            try
            {
                ret = Dongle_Enum(ref pDongleInfo, out pCount);
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
            }

            if (ret != 0)
            {
                return;

            }

            //Debug.WriteLine("Enum Dongle Success!Count: " + pCount + "\r\n");
            ret = Dongle_Enum(ref pDongleInfo, out pCount);
            if (ret != 0)
            {
                //Debug.WriteLine("GetInfo Dongle Failed! Return value:" + ret.ToString("X") + "\r\n");
                return;

            }

            //Debug.WriteLine("GetInfo Dongle Success!" + "\r\n");
            string armInfo = "";
            for (int k = 0; k < pCount; k++)
            {
                armInfo += "\n*********Dongle ARM INFO*******\n" + "\r\n"
                + "The index: " + k + "\r\n"
                + "Agent ID: " + pDongleInfo.m_Agent.ToString("X") + "\r\n"
                + "Dev Type: " + pDongleInfo.m_DevType + "\r\n"
                + "HID: ";

                for (int i = 0; i < 8; i++)
                {
                    armInfo += pDongleInfo.m_HID[i].ToString("X") + "  ";

                }
                armInfo += "\r\n";

                armInfo += "Brith day: 20" + pDongleInfo.m_BirthDay[0].ToString("X") + "-" + pDongleInfo.m_BirthDay[1].ToString("X") + "-" + pDongleInfo.m_BirthDay[2].ToString("X") + "  " + pDongleInfo.m_BirthDay[3].ToString("X") + ":" + pDongleInfo.m_BirthDay[4].ToString("X") + ":" + pDongleInfo.m_BirthDay[5].ToString("X") + "\r\n";
                armInfo += "Is Mother Dongle: " + pDongleInfo.m_IsMother + "\r\n";
                armInfo += "PID: " + pDongleInfo.m_PID.ToString("X") + "\r\n";

                armInfo += "Product Type: " + pDongleInfo.m_Type.ToString("X") + "\r\n";
                armInfo += "UID: " + pDongleInfo.m_UserID.ToString("X") + "\r\n";
            }

            //Debug.WriteLine(armInfo);

            // open dongle
            ret = Dongle_Open(ref hDongle, 0);
            if (ret != 0)
            {
                //Debug.WriteLine("Open Dongle Failed! Return value:" + ret.ToString("X") + "\r\n");
                return;

            }

            //Debug.WriteLine("Open Dongle Success! " + hDongle + "\r\n");
            uint utcTime = 0;
            uint result = Dongle_GetUTCTime(hDongle, ref utcTime);
            //Debug.WriteLine("期限：" + utcTime + "\r\n" + result + "\r\n");

            ret = Dongle_Close(hDongle);
            if (ret != 0)
            {
                //Debug.WriteLine("Close Dongle Failed! Return value:" + ret.ToString("X") + "\r\n");
                return;

            }

            //Debug.WriteLine("Close Dongle Success! \r\n");
        }


        //public static string pid { get; set; } = "2E31DCDE";
        /// <summary>
        /// dongle verify
        /// </summary>
        /// <param name="productionId">production id</param>
        /// <returns></returns>
        public static bool Verify(string productionId)
        {
            uint ret = 0;
            int pCount = 0;
            DONGLE_INFO pDongleInfo = new DONGLE_INFO();
            try
            {
                ret = Dongle_Enum(ref pDongleInfo, out pCount);
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
                return false;
            }

            if (ret != DONGLE_SUCCESS)
            {
                return false;
            }

            int index = -1;
            if (Dongle_Enum(ref pDongleInfo, out pCount) == DONGLE_SUCCESS)
            {
                for (int k = 0; k < pCount; k++)
                {
                    string pid = pDongleInfo.m_PID.ToString("X");
                    if (pid == productionId)
                    {
                        index = k;
                        break;
                    }
                }
            }

            // 没找到锁
            if (index == -1)
            {
                return false;
            }

            DONGLE_HANDLE hDongle = 0;
            // open dongle
            ret = Dongle_Open(ref hDongle, index);
            if (ret == 0)
            {
                // 检查时间
                uint dwTime = 0;
                ret = Dongle_GetDeadline(hDongle, ref dwTime);
                if (ret != DONGLE_SUCCESS)
                {
                    return false;
                }

                if (dwTime != 0xFFFFFFFF)
                {
                    uint val = 0xFFFF0000 & dwTime;
                    if ((0xFFFF0000 & dwTime) == 0)  // 用于PIN验证后的计时
                    {
                        return dwTime > 0;
                    }
                    else
                    {
                        DateTime deadline = new DateTime(1970, 1, 1);
                        deadline = deadline.AddSeconds(dwTime);
                        return DateTime.Compare(deadline, DateTime.Now) >= 0;
                    }
                }

                // close dongle
                ret = Dongle_Close(hDongle);
                if (ret != DONGLE_SUCCESS)
                {
                    //Debug.WriteLine("Close Dongle Failed! Return value:" + ret.ToString("X") + "\r\n");
                }

                return true;
            }

            return false;
        }
    }
}
