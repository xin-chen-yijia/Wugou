using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Wugou
{
    /// <summary>
    /// 用于下载文件,文件存储在temp文件夹
    /// </summary>
    public class FileDownloader : IDownloader
    {
        public event System.Action<string> onComplete;

        public string name { get; private set; }

        public string uri { get; private set; }

        public FileDownloader(string name, string uri) 
        {
            this.name = name;
            this.uri = uri;
            CoroutineLauncher.active.StartCoroutine(DownloadContentCoroutine(uri));
        }

        private UnityWebRequest request_;
        private UnityWebRequestAsyncOperation requestAso_;
        public float progress => requestAso_ != null ? requestAso_.progress : 0;

        public bool isDone => requestAso_ != null ? requestAso_.isDone : false;

        private int _result = -1;
        public int result => _result == (int)UnityWebRequest.Result.Success ? 0 : 1;

        private IEnumerator DownloadContentCoroutine(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                request_ = webRequest;
                requestAso_ = webRequest.SendWebRequest();
                // Request and wait for the desired page.
                yield return requestAso_;

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                _result = (int)webRequest.result;
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        //Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                        //Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadedBytes);
                        var bytes = webRequest.downloadHandler.data;
                        var filePath = $"{Gameplay.cachePath}/{pages[page]}";
                        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(bytes, 0, bytes.Length);
                            fs.Close();

                            onComplete?.Invoke(filePath);
                        }
                        break;
                }

                request_ = null;
            }
        }

        public void Abort()
        {
            if(request_ != null)
            {
                request_.Abort();
                request_ = null;
            }
        }

        public void Cancel()
        {
            Abort();
        }
    }
}
