using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// 文件夹类型资源的管理封装，一个文件夹代表一个资源； 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FolderAssetsManager<T>
    {
        public string path { get; private set; }
        IAssetParser<T> parser_ = null;

        public FolderAssetsManager(string path, IAssetParser<T> parser = null)
        {
            this.path = path;
            parser_ = parser;
            if (parser_ == null)
            {
                parser_ = new JsonFileParser<T>();
            }
        }

        /// <summary>
        /// 获取资源的完整路径
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetFullPath(string name)
        {
            return $"{path}/{name}";
        }

        /// <summary>
        /// 查找所有的文件夹，以'.'开头的会忽略
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllNames()
        {
            List<string> list = new List<string>();

            DirectoryInfo TheFolder = new DirectoryInfo(path);
            if (TheFolder.Exists)
            {
                foreach (var dir in TheFolder.GetDirectories())
                {
                    if (!dir.Name.StartsWith("."))
                    {
                        list.Add(dir.Name);
                    }
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
                foreach (var dir in TheFolder.GetDirectories())
                {
                    list.Add(parser_.Parse(dir.FullName));
                }
            }

            return list;
        }

        public T Get(string name)
        {
            return parser_.Parse($"{path}/{name}");
        }

        public void Save(string name, T item, bool overwrite = true)
        {
            Directory.CreateDirectory(path);
            parser_.Save($"{path}/{name}", item, overwrite);
        }

        public void Delete(string name)
        {
            string fullPath = $"{path}/{name}";
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
        }

        public bool Exists(string name)
        {
            return Directory.Exists($"{path}/{name}");
        }
    }
}