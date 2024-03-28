using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.Editor
{
    /// <summary>
    /// 事务管理程序
    /// 参考UE4,简单点搞
    /// 1. 维护一个数组和游标就行了，不搞两个栈了
    /// </summary>
    public class Transactor
    {
        /// <summary>
        /// 当前事务
        /// </summary>
        public Transaction currentTransaction { get; private set; }

        List<Transaction> transactions_ = new List<Transaction>();
        private int index_ = -1;

        public bool CanUndo()
        {
            return index_ > -1 && transactions_.Count > 0;
        }

        public bool CanRedo()
        {
            return index_ + 1 < transactions_.Count;
        }

        public bool Undo()
        {
            if(CanUndo())
            {
                var transaction = transactions_[index_];
                index_--;
                transaction.Apply();

                return true;
            }

            return false;
        }

        public bool Redo()
        {
            if(CanRedo())
            {
                var transaction = transactions_[(++index_)];
                transaction.BeginOperation();
                transaction.Apply();
                transaction.EndOperation();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 添加新的操作
        /// </summary>
        /// <param name="transaction"></param>
        public void BeginTransaction()
        {
            ClearRedos();

            // 新的事务
            currentTransaction = new Transaction();
        }

        /// <summary>
        /// 取消当前事务，需要回滚的
        /// </summary>
        public void CancelTransaction()
        {
            if (currentTransaction != null)
            {
                currentTransaction = null;
            }
        }

        public void EndTransaction()
        {
            currentTransaction.FinalizeDo();
            transactions_.Add(currentTransaction);
            index_ = transactions_.Count - 1;
        }

        /// <summary>
        /// 给当前事务添加一个记录
        /// </summary>
        /// <param name="record"></param>
        public void Record(ObjectRecord record)
        {
            if(currentTransaction != null)
            {
                currentTransaction.Add(record);
            }
            else
            {
                Wugou.Logger.Error("Undo record but not start a transaction!");
            }
        }

        /// <summary>
        /// 用于undo之后，再次做了操作时清除之前的redos
        /// </summary>
        private void ClearRedos()
        {
            if(index_ + 1 < transactions_.Count)
            {
                transactions_.RemoveRange(index_ + 1, transactions_.Count - index_ - 1);
            }

        }
    }
}
