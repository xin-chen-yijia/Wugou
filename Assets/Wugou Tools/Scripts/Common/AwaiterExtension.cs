using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public static class AwaiterExtension
{
    public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
    {
        TaskCompletionSource<object> source = new TaskCompletionSource<object>();
        asyncOp.completed += obj => { source.SetResult(null); };
        return ((Task)source.Task).GetAwaiter();
    }   
}
