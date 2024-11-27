using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public interface IDownloader
    {
        public string name { get; }

        public string uri { get;}

        public float progress { get;}

        public bool isDone { get;}

        public int result { get; }

        public string errorMessage { get; }

        public void Cancel();
    }

}
