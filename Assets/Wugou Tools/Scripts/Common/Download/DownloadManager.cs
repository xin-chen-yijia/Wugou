using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace Wugou{

    /// <summary>
    /// ÏÂÔØ¼ÇÂ¼
    /// </summary>
    public class DownloadAssetRecord
    {
        public string name;
        public string url;
    }

    public static class DownloadManager
    {
        public static string file => $"{Gameplay.workplacePath}/download-list";

        private static List<DownloadAssetRecord> _records = null;
        private static List<DownloadAssetRecord> records
        {
            get
            {
                if(_records == null)
                {
                    List<DownloadAssetRecord> records;
                    if (File.Exists(file))
                    {
                        var content = System.IO.File.ReadAllText(file);
                        records = JsonConvert.DeserializeObject<List<DownloadAssetRecord>>(content);
                    }
                    else
                    {
                        records = new List<DownloadAssetRecord>();
                    }

                    _records = records;
                }
                return _records;
            }
        }

        public static IDownloader Download(string name, string uri, System.Action<string> onComplete) {

            System.Action<string> completeAct = (value) =>
            {
                onComplete.Invoke(value);

                var parts = uri.Split('/');
                string name = parts[parts.Length - 1];
                AddRecord(name, uri);
            };

            var downloader = new FileDownloader(name, uri);
            downloader.onComplete += completeAct;

            return downloader;
        }

        public static void AddRecord(string name, string url)
        {
            var record = new DownloadAssetRecord();
            record.name = name;
            record.url = url;

            records.Add(record);
            File.WriteAllText(file, JsonConvert.SerializeObject(records));
        }

        public static void RemoveRecord(string name, bool delAsset=false)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].name == name)
                {
                    records.RemoveAt(i);
                    break;
                }
            }

            File.WriteAllText(file, JsonConvert.SerializeObject(records));
        }

        public static bool HasRecord(string url)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].url == url)
                {
                    return true;
                }
            }

            return false;
        }

        public static string WrapeUrl(string url)
        {
            return $"{Gameplay.settings.host}{url}";
        }

    }


}

