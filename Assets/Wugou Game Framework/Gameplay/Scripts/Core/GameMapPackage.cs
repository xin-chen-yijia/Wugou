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
                    var content = packer.Read(path, $"{name}/{name}{GameConsole.kGameMapFileSuffix}");
                    if (content != null)
                    {
                        string text = System.Text.Encoding.UTF8.GetString(content);

                        gameMap_ = GameMap.Create();
                        gameMap_.Parse(text);
                    }

                }

                return gameMap_;
            }
        }

        /// <summary>
        /// GameMapPackage是否有效
        /// </summary>
        public bool valid { get; private set; }

        public const string kSuffix = ".em";

        public GameMapPackage(string path)
        {
            this.path = path;
            ZipPacker packer = new ZipPacker();
            this.name = packer.GetRootDirectory(path);
            //this.name = Path.GetFileNameWithoutExtension(path);

            CheckValid();
        }

        private void CheckValid()
        {
            //
            valid = true;
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

            // 根据缓存判断是否需要解压
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
                    try
                    {
                        Directory.Delete(packageDir, true);    // 删除
                    }
                    catch(System.Exception e)
                    {
                        Logger.Error($"删除目录：{packageDir} 失败。{e.Message}");
                    }

                }
                if(!packer.UnZip(path, dst))
                {
                    Logger.Error(packer.errMessage);
                }
                else
                {
                    try
                    {
                        // 可能多个进程写，主要是测试环境下
                        File.WriteAllText(cacheFile, lastModified.ToString());
                    }
                    catch (System.Exception e)
                    {
                        Logger.Error($"写入：{cacheFile} 失败。{e.Message}");
                    }
                }
            }

        }
    }

    /// <summary>
    /// GameMapPackage解析
    /// </summary>
    public class GameMapPackageParser : IAssetParser<GameMapPackage>
    {
        public GameMapPackage Parse(string path)
        {
            return new GameMapPackage(path);
        }

        public void Save(string path, GameMapPackage obj, bool overwrite = true)
        {
            throw new System.NotImplementedException();
        }
    }
}
