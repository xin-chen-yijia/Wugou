using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.Editor
{
    /// <summary>
    /// 事务模拟
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// false表示Undo，true表示Redo
        /// </summary>
        public bool bFlip = false;

        /// <summary>
        /// 操作记录
        /// </summary>
        private List<ObjectRecord> records_ = new List<ObjectRecord>();

        public void BeginOperation()
        {

        }

        public void EndOperation()
        {

        }


        public void Apply()
        {
            if (bFlip)
            {
                //for (int i = 0; i < records_.Count; i++)
                //{
                //    records_[i].Save();
                //}

                for (int i = 0; i < records_.Count; i++)
                {
                    records_[i].Load();
                }
            }
            else
            {
                for (int i = records_.Count - 1; i >= 0; i--)
                {
                    records_[i].Save();     // 这个针对有些操作，需要
                }

                for (int i = records_.Count - 1; i >= 0; i--)
                {
                    records_[i].Revert();
                }
            }
            bFlip = !bFlip;
        }

        public void Add(ObjectRecord record)
        {
            records_.Add(record);
        }

        public void FinalizeDo()
        {
            for (int i = 0; i < records_.Count; i++)
            {
                records_[i].FinalizeDo();
            }
        }
    }
}

