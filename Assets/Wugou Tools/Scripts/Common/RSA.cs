using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Wugou
{
    public class RSA
    {
        private const int RsaKeySize = 2048;
        private const string publicKeyFileName = "RSA.Pub";
        private const string privateKeyFileName = "RSA.Private";

        /// <summary>
        ///�ڸ���·��������XML��ʽ��˽Կ�͹�Կ��
        /// </summary>
        public static void GenerateKeys(string path)
        {
            using (var rsa = new RSACryptoServiceProvider(RsaKeySize))
            {
                try
                {
                    // ��ȡ˽Կ�͹�Կ��
                    var publicKey = rsa.ToXmlString(false);
                    var privateKey = rsa.ToXmlString(true);

                    // ���浽����
                    File.WriteAllText(Path.Combine(path, publicKeyFileName), publicKey);
                    File.WriteAllText(Path.Combine(path, privateKeyFileName), privateKey);

                    //Console.WriteLine(string.Format("���ɵ�RSA��Կ��·��: {0}\\ [{1}, {2}]", path, publicKeyFileName, privateKeyFileName));
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        /// <summary>
        /// �ø���·����RSA��Կ�ļ����ܴ��ı���
        /// </summary>
        /// <param name="plainText">Ҫ���ܵ��ı�</param>
        /// <param name="pathToPublicKey">���ڼ��ܵĹ�Կ·��.</param>
        /// <returns>��ʾ�������ݵ�64λ�����ַ���.</returns>
        public static string Encrypt(string plainText, string pathToPublicKey)
        {
            using (var rsa = new RSACryptoServiceProvider(RsaKeySize))
            {
                try
                {
                    //���ع�Կ
                    var publicXmlKey = File.ReadAllText(pathToPublicKey);
                    rsa.FromXmlString(publicXmlKey);

                    var bytesToEncrypt = System.Text.Encoding.Unicode.GetBytes(plainText);

                    var bytesEncrypted = rsa.Encrypt(bytesToEncrypt, false);

                    return Convert.ToBase64String(bytesEncrypted);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        /// <summary>
        /// Decrypts encrypted text given a RSA private key file path.����·����RSA˽Կ�ļ����� �����ı�
        /// </summary>
        /// <param name="encryptedText">���ܵ�����</param>
        /// <param name="pathToPrivateKey">���ڼ��ܵ�˽Կ·��.</param>
        /// <returns>δ�������ݵ��ַ���</returns>
        public static string Decrypt(string encryptedText, string pathToPrivateKey)
        {
            using (var rsa = new RSACryptoServiceProvider(RsaKeySize))
            {
                try
                {
                    var privateXmlKey = File.ReadAllText(pathToPrivateKey);
                    rsa.FromXmlString(privateXmlKey);

                    var bytesEncrypted = Convert.FromBase64String(encryptedText);

                    var bytesPlainText = rsa.Decrypt(bytesEncrypted, false);

                    return System.Text.Encoding.Unicode.GetString(bytesPlainText);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }
    }
}
