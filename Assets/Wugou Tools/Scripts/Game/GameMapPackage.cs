using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// GameMap包，一个GameMap包包括：
    /// 1. GameMap；
    /// 2. GameMap的依赖资源；
    /// 3. GameMap的其它资源，如icon等；
    /// </summary>
    public class GameMapPackage
    {
        public string path { get; private set; }

        public string name { get; private set; }

        private GameMap gameMap_ = null;
        public GameMap gameMap
        {
            get
            {
                if (gameMap_ == null)
                {
                    ZipPacker packer = new ZipPacker();
                    var content = packer.Read(path, $"{name}/{name}{Gameplay.kGameMapFileSuffix}");
                    string text = System.Text.Encoding.UTF8.GetString(content);

                    gameMap_ = GameMap.Create();
                    gameMap_.Parse(text);
                }

                return gameMap_;
            }
        }

        public const string kSuffix = ".em";

        public GameMapPackage(string path)
        {
            this.path = path;
            this.name = Path.GetFileNameWithoutExtension(path);

            CheckValid();
        }

        private void CheckValid()
        {
            //
        }

        public Texture2D GetThumbnail()
        {
            var packer = new ZipPacker();
            var content = packer.Read(path, $"{name}/{name}.jpg");

            Texture2D texture = new Texture2D(256, 256);
            texture.LoadImage(content);
            return texture;
        }

        /// <summary>
        /// 创建包
        /// </summary>
        /// <param name="path"></param>
        public static void Build(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            var dst = $"{path}{kSuffix}";
            if (File.Exists(dst))
            {
                File.Delete(dst);
            }

            var packer = new ZipPacker();
            packer.CreatZip(path, dst);
        }

        /// <summary>
        /// 提取包中内容
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dst"></param>
        public static void Extract(string path, string dst)
        {
            if(!File.Exists(path) || !path.EndsWith(kSuffix))
            {
                Logger.Error($"Extract {path} fail, file not exist or valid..");
                return;
            }

            // 有缓存就不解压了
            var packer = new ZipPacker();
            var root = packer.GetRootDirectory(path);
            var packageDir = $"{dst}/{root}";
            bool isCached = false;
            var cacheFile = $"{packageDir}/.cache";
            System.DateTime lastModified = File.GetLastWriteTime(path);
            if (File.Exists(cacheFile))
            {
                // 对比时间，有更新则删除重新解压
                var time = System.DateTime.Parse(File.ReadAllText(cacheFile)) ;
                if (time.Year == lastModified.Year
                    && time.Month == lastModified.Month
                    && time.Day == lastModified.Day
                    && time.Hour == lastModified.Hour
                    && time.Minute == lastModified.Minute
                    && time.Second == lastModified.Second)
                {
                    isCached = true;
                }
            }

            if(!isCached)
            {
                if (Directory.Exists(packageDir))
                {
                    Directory.Delete(packageDir, true);    // 删除
                }
                if(!packer.UnZip(path, dst))
                {
                    Logger.Error(packer.errMessage);
                }
                else
                {
                    File.WriteAllText(cacheFile, lastModified.ToString());
                }
            }

        }
    }
}
