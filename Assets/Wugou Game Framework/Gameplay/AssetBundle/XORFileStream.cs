using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// 使用异或加密
    /// </summary>
    public class XORFileStream : FileStream
    {
        const byte KEY = 61;
        public XORFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) : base(path, mode, access, share, bufferSize, useAsync)
        {
        }
        public XORFileStream(string path, FileMode mode) : base(path, mode)
        {
        }
        public override int Read(byte[] array, int offset, int count)
        {
            var index = base.Read(array, offset, count);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] ^= KEY;
            }
            return index;
        }
        public override void Write(byte[] array, int offset, int count)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] ^= KEY;
            }
            base.Write(array, offset, count);
        }
    }
}
