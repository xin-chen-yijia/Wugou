
namespace Wugou{

    public static class DownloadManager
    {
        public static IDownloader Download(string name, string uri, System.Action<string> onComplete) {

            var downloader = new HttpDownloader(name, uri);
            downloader.Start(onComplete);

            return downloader;
        }

        public static string WrapeUrl(string url)
        {
            return $"{GameConsole.settings.host}{url}";
        }

    }


}

