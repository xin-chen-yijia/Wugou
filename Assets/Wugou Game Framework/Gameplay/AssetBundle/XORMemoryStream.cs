using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// 使用异或加密
    /// </summary>
    public class XORMemoryStream : MemoryStream
    {
        const byte KEY = 61;
        public XORMemoryStream(byte[] buffer, int index, int count) : base(buffer, index, count)
        {
        }

        public XORMemoryStream(byte[] buffer) : base(buffer, 0, buffer.Length)
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
