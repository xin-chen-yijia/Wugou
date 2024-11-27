using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Wugou
{
    public interface IAssetParser<T>
    {
        public T Parse(string path);
        public void Save(string path, T obj, bool overwrite = true);
    }

    public class JsonFileParser<T> : IAssetParser<T>
    {
        public T Parse(string path)
        {
            string content = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(content);
        }

        public void Save(string path, T obj, bool overwrite = true)
        {
            if (File.Exists(path) && !overwrite)
            {
                Wugou.Logger.Error($"{path} exists...");
                return;
            }

            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(obj));
            }
            catch(System.Exception e)
            {
                Logger.Error($"Write {path} fail.. {e.Message}");
            }
        }
    }

    /// <summary>
    /// 文件类型资源的管理封装，一个文件代表一个资源；
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FileAssetsManager<T>
    {
        public string path { get; private set; }
        public string ext { get; private set; }
        IAssetParser<T> parser_ = null;

        /// <summary>
        /// 单个文件类型资产
        /// </summary>
        /// <param name="path">目录</param>
        /// <param name="ext"></param>
        /// <param name="parser">默认使用JsonFileParser</param>
        public FileAssetsManager(string path, string ext, IAssetParser<T> parser = null)
        {
            this.path = path;
            this.ext = ext;
            parser_ = parser;
            if (parser_ == null)
            {
                parser_ = new JsonFileParser<T>();
            }

            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// 获取资源的完整路径
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetFullPath(string name)
        {
            return $"{path}/{name}{ext}";
        }

        public string GetRelativePath(string filePath)
        {
            var tmp = Path.GetRelativePath(path, filePath);
            tmp = tmp.Replace("\\", "/");
            tmp = tmp.Substring(0, tmp.LastIndexOf('.'));

            return tmp;
        }

        public List<string> GetAllNames()
        {
            List<string> list = new List<string>();

            DirectoryInfo TheFolder = new DirectoryInfo(path);
            if (TheFolder.Exists)
            {
                foreach (FileInfo NextFile in TheFolder.GetFiles($"*{ext}",SearchOption.AllDirectories))
                {
                    list.Add(GetRelativePath(NextFile.FullName));
                }
            }

            return list;
        }

        public List<T> GetAll()
        {
            List<T> list = new List<T>();

            DirectoryInfo TheFolder = new DirectoryInfo(path);
            //遍历文件
            if (TheFolder.Exists)
            {
                foreach (FileInfo NextFile in TheFolder.GetFiles($"*{ext}", SearchOption.AllDirectories))
                {
                    //var tmp = parser_.Parse(GetRelativePath(NextFile.FullName));
                    //list.Add(tmp);

                    list.Add(Get(GetRelativePath(NextFile.FullName)));
                }
            }

            return list;
        }

        public T Get(string name)
        {
            return parser_.Parse($"{path}/{name}{ext}");
        }

        public void Save(string name, T item, bool overwrite = true)
        {
            parser_.Save($"{path}/{name}{ext}", item, overwrite);
        }

        public void Delete(string name)
        {
            string fullPath = $"{path}/{name}{ext}";
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public void DeleteAll()
        {
            var dirInfo = new DirectoryInfo(path);
            dirInfo.Delete(true);
            Directory.CreateDirectory(path);
            //try
            //{
            //    DirectoryInfo dir = new DirectoryInfo(path);
            //    FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
            //    foreach (FileSystemInfo i in fileinfo)
            //    {
            //        if (i is DirectoryInfo)            //判断是否文件夹
            //        {
            //            DirectoryInfo subdir = new DirectoryInfo(i.FullName);
            //            subdir.Delete(true);          //删除子目录和文件
            //        }
            //        else
            //        {
            //            File.Delete(i.FullName);      //删除指定文件
            //        }
            //    }
            //}
            //catch (System.Exception e)
            //{
            //    throw e;
            //}
        }

        public bool Exists(string name)
        {
            return File.Exists($"{path}/{name}{ext}");
        }
    }
}
