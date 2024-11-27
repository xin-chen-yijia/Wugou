using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    class ObjectPool<T>
    { 
        private Stack<T> cached_ = new Stack<T>();   //缓存池

        public delegate T InstantiateFunc();
        private InstantiateFunc instantiateFunc_;
        private int maxCount_;

        public ObjectPool(int cacheCount,InstantiateFunc func)
        {
            maxCount_ = cacheCount;
            instantiateFunc_ = func;
            for(int i=0;i<cacheCount;++i)
            {
                T obj =instantiateFunc_();
                cached_.Push(obj);
            }
        }

        public T Assign()
        {
            if(cached_.Count == 0)
            {
                Logger.Error("There no more " + typeof(T).Name + " to assign.");
                // return instantiateFunc_();
                return default(T);
            }

            return cached_.Pop();
        }

        public void Retrieve(T obj)
        {
            cached_.Push(obj);
        }

    }
}

