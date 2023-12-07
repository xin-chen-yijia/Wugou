using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Wugou {
    public static class AwaiterExtension
    {
        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            asyncOp.completed += obj => { source.SetResult(null); };
            return ((Task)source.Task).GetAwaiter();
        }
    }

    public class EnumeratorAwaiter
    {
        private TaskCompletionSource<object> source = new TaskCompletionSource<object>();
        public Task Task { get { return source.Task; } }
        public EnumeratorAwaiter(IEnumerator enumerator)
        {
            CoroutineLauncher.active.StartCoroutine(WaitAndDo(enumerator));
        }

        public IEnumerator WaitAndDo(IEnumerator enumerator)
        {
            yield return enumerator;

            source.SetResult(null);
        }

        public TaskAwaiter GetAwaiter()
        {
            return Task.GetAwaiter();
        }
    }

    /// <summary>
    /// 协程改造为Await/async
    /// </summary>
    public class YieldInstructionAwaiter
    {
        private TaskCompletionSource<object> source = new TaskCompletionSource<object>();

        public YieldInstructionAwaiter(YieldInstruction inst)
        {
            CoroutineLauncher.active.StartCoroutine(WaitAndDo(inst));
        }

        public Task Task { get { return source.Task; } }



        public IEnumerator WaitAndDo(YieldInstruction inst)
        {
            yield return inst;

            source.SetResult(null);
        }

        public TaskAwaiter GetAwaiter()
        {
            return Task.GetAwaiter();
        }
    }
}
