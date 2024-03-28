using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace Wugou
{
    public class ZipPacker
    {

        public string entryNameEncoding = "gbk";

        #region   基础参数
        public delegate void UnZipProgressEventHandler(object sender, UnZipProgressEventArgs e);
        public event UnZipProgressEventHandler unZipProgress;

        public delegate void CompressProgressEventHandler(object sender, CompressProgressEventArgs e);
        public event CompressProgressEventHandler compressProgress;

        public string errMessage { get; private set; }

        #endregion

        #region   公有方法

        /// <summary>
        /// 创建 zip 存档，该文档包含指定目录的文件和子目录（单个目录）。
        /// </summary>
        /// <param name="sourceDirectoryName">将要压缩存档的文件目录的路径，可以为相对路径或绝对路径。 相对路径是指相对于当前工作目录的路径。</param>
        /// <param name="destinationArchiveFileName">将要生成的压缩包的存档路径，可以为相对路径或绝对路径。相对路径是指相对于当前工作目录的路径。</param>
        /// <param name="compressionLevel">指示压缩操作是强调速度还是强调压缩大小的枚举值</param>
        /// <param name="includeBaseDirectory">压缩包中是否包含父目录</param>
        /// <returns>返回结果（true：表示成功）</returns>
        public bool CreatZip(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel = CompressionLevel.NoCompression, bool includeBaseDirectory = true)
        {
            int i = 1;
            try
            {
                if (Directory.Exists(sourceDirectoryName))
                    if (!File.Exists(destinationArchiveFileName))
                    {
                        ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory);
                    }
                    else
                    {
                        var toZipFileDictionaryList = GetAllDirList(sourceDirectoryName, includeBaseDirectory);
                        using (var archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Update, Encoding.GetEncoding(entryNameEncoding)))
                        {
                            var count = toZipFileDictionaryList.Keys.Count;
                            foreach (var toZipFileKey in toZipFileDictionaryList.Keys)
                            {
                                if (toZipFileKey != destinationArchiveFileName)
                                {
                                    var toZipedFileName = Path.GetFileName(toZipFileKey);
                                    var toDelArchives = new List<ZipArchiveEntry>();
                                    foreach (var zipArchiveEntry in archive.Entries)
                                    {
                                        if (toZipedFileName != null && (zipArchiveEntry.FullName.StartsWith(toZipedFileName) || toZipedFileName.StartsWith(zipArchiveEntry.FullName)))
                                        {
                                            i++;
                                            compressProgress?.Invoke(this, new CompressProgressEventArgs { Size = zipArchiveEntry.Length, Count = count, Index = i, Path = zipArchiveEntry.FullName, Name = zipArchiveEntry.Name });
                                            toDelArchives.Add(zipArchiveEntry);
                                        }
                                    }

                                    foreach (var zipArchiveEntry in toDelArchives)
                                        zipArchiveEntry.Delete();
                                    archive.CreateEntryFromFile(toZipFileKey, toZipFileDictionaryList[toZipFileKey], compressionLevel);
                                }
                            }
                        }
                    }
                else if (File.Exists(sourceDirectoryName))
                {
                    if (!File.Exists(destinationArchiveFileName))
                    {
                        ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, false);
                    }
                    else
                    {
                        using (var archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Update, Encoding.GetEncoding(entryNameEncoding)))
                        {
                            if (sourceDirectoryName != destinationArchiveFileName)
                            {
                                var toZipedFileName = Path.GetFileName(sourceDirectoryName);
                                var toDelArchives = new List<ZipArchiveEntry>();
                                var count = archive.Entries.Count;
                                foreach (var zipArchiveEntry in archive.Entries)
                                {
                                    if (toZipedFileName != null && (zipArchiveEntry.FullName.StartsWith(toZipedFileName) || toZipedFileName.StartsWith(zipArchiveEntry.FullName)))
                                    {
                                        i++;
                                        compressProgress?.Invoke(this, new CompressProgressEventArgs { Size = zipArchiveEntry.Length, Count = count, Index = i, Path = zipArchiveEntry.FullName, Name = zipArchiveEntry.Name });
                                        toDelArchives.Add(zipArchiveEntry);
                                    }
                                }

                                foreach (var zipArchiveEntry in toDelArchives)
                                    zipArchiveEntry.Delete();
                                archive.CreateEntryFromFile(sourceDirectoryName, toZipedFileName, compressionLevel);
                            }
                        }
                    }
                }
                else
                {
                    return false;

                }

                return true;
            }
            catch (Exception e)
            {
                errMessage = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 创建 zip 存档，该存档包含指定目录的文件和目录（多个目录）
        /// </summary>
        /// <param name="sourceDirectoryName">将要压缩存档的文件目录的路径，可以为相对路径或绝对路径。 相对路径是指相对于当前工作目录的路径。</param>
        /// <param name="destinationArchiveFileName">将要生成的压缩包的存档路径，可以为相对路径或绝对路径。 相对路径是指相对于当前工作目录的路径。</param>
        /// <param name="compressionLevel">指示压缩操作是强调速度还是强调压缩大小的枚举值</param>
        /// <returns>返回结果（true：表示成功）</returns>
        public bool CreatZip(Dictionary<string, string> sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel = CompressionLevel.NoCompression)
        {
            int i = 1;
            try
            {
                using (FileStream zipToOpen = new FileStream(destinationArchiveFileName, FileMode.OpenOrCreate))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update, true, Encoding.GetEncoding(entryNameEncoding)))
                    {
                        foreach (var toZipFileKey in sourceDirectoryName.Keys)
                        {
                            if (toZipFileKey != destinationArchiveFileName)
                            {
                                var toZipedFileName = Path.GetFileName(toZipFileKey);
                                var toDelArchives = new List<ZipArchiveEntry>();
                                var count = archive.Entries.Count;
                                foreach (var zipArchiveEntry in archive.Entries)
                                {
                                    if (toZipedFileName != null && (zipArchiveEntry.FullName.StartsWith(toZipedFileName) || toZipedFileName.StartsWith(zipArchiveEntry.FullName)))
                                    {
                                        i++;
                                        compressProgress?.Invoke(this, new CompressProgressEventArgs { Size = zipArchiveEntry.Length, Count = count, Index = i, Path = toZipedFileName });
                                        toDelArchives.Add(zipArchiveEntry);
                                    }
                                }

                                foreach (var zipArchiveEntry in toDelArchives)
                                    zipArchiveEntry.Delete();
                                archive.CreateEntryFromFile(toZipFileKey, sourceDirectoryName[toZipFileKey], compressionLevel);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 递归删除磁盘上的指定文件夹目录及文件
        /// </summary>
        /// <param name="baseDirectory">需要删除的文件夹路径</param>
        /// <returns>返回结果（true：表示成功）</returns>
        public bool DeleteFolder(string baseDirectory)
        {
            var successed = true;
            try
            {
                if (Directory.Exists(baseDirectory)) //如果存在这个文件夹删除之 
                {
                    foreach (var directory in Directory.GetFileSystemEntries(baseDirectory))
                        if (File.Exists(directory))
                            File.Delete(directory); //直接删除其中的文件  
                        else
                            successed = DeleteFolder(directory); //递归删除子文件夹 
                    Directory.Delete(baseDirectory); //删除已空文件夹     
                }
            }
            catch (Exception)
            {
                successed = false;
            }
            return successed;
        }

        /// <summary>
        /// 递归获取磁盘上的指定目录下所有文件的集合，返回类型是：字典[文件名，要压缩的相对文件名]
        /// </summary>
        /// <param name="strBaseDir">需要递归的目录路径</param>
        /// <param name="includeBaseDirectory">是否包含本目录（false：表示不包含）</param>
        /// <param name="namePrefix">目录前缀</param>
        /// <returns>返回当前递归目录下的所有文件集合</returns>
        public Dictionary<string, string> GetAllDirList(string strBaseDir, bool includeBaseDirectory = false, string namePrefix = "")
        {
            var resultDictionary = new Dictionary<string, string>();
            var directoryInfo = new DirectoryInfo(strBaseDir);
            var directories = directoryInfo.GetDirectories();
            var fileInfos = directoryInfo.GetFiles();
            if (includeBaseDirectory)
                namePrefix += directoryInfo.Name + "\\";
            foreach (var directory in directories)
                resultDictionary = resultDictionary.Concat(GetAllDirList(directory.FullName, true, namePrefix)).ToDictionary(k => k.Key, k => k.Value); //FullName是某个子目录的绝对地址，
            foreach (var fileInfo in fileInfos)
                if (!resultDictionary.ContainsKey(fileInfo.FullName))
                    resultDictionary.Add(fileInfo.FullName, namePrefix + fileInfo.Name);
            return resultDictionary;
        }

        /// <summary>
        /// 解压Zip文件，并覆盖保存到指定的目标路径文件夹下
        /// </summary>
        /// <param name="zipFilePath">将要解压缩的zip文件的路径</param>
        /// <param name="unZipDir">解压后将zip中的文件存储到磁盘的目标路径</param>
        /// <returns>返回结果（true：表示成功）</returns>
        public bool UnZip(string zipFilePath, string unZipDir)
        {
            bool result;
            try
            {
                unZipDir = unZipDir.EndsWith(@"\") ? unZipDir : unZipDir + @"\";
                var directoryInfo = new DirectoryInfo(unZipDir);
                if (!directoryInfo.Exists)
                    directoryInfo.Create();
                var fileInfo = new FileInfo(zipFilePath);
                if (!fileInfo.Exists)
                    return false;
                using (var zipToOpen = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, true, Encoding.GetEncoding(entryNameEncoding)))
                    {
                        var count = archive.Entries.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var entry = archive.Entries[i];
                            if (!entry.FullName.EndsWith("/"))
                            {
                                var entryFilePath = Regex.Replace(entry.FullName.Replace("/", @"\"), @"^\\*", "");
                                var filePath = directoryInfo + entryFilePath; //设置解压路径
                                unZipProgress?.Invoke(this, new UnZipProgressEventArgs { Size = entry.Length, Count = count, Index = i + 1, Path = entry.FullName, Name = entry.Name });
                                var content = new byte[entry.Length];
                                entry.Open().Read(content, 0, content.Length);
                                var greatFolder = Directory.GetParent(filePath);
                                if (!greatFolder.Exists)
                                    greatFolder.Create();
                                File.WriteAllBytes(filePath, content);
                            }
                        }
                    }
                }
                result = true;
            }
            catch (Exception e)
            {
                errMessage = e.Message;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// 获取Zip压缩包中的文件列表
        /// </summary>
        /// <param name="zipFilePath">Zip压缩包文件的物理路径</param>
        /// <returns>返回解压缩包的文件列表</returns>
        public List<string> GetZipFileList(string zipFilePath)
        {
            List<string> fList = new List<string>();
            if (!File.Exists(zipFilePath))
                return fList;
            try
            {
                using (var zipToOpen = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, true, Encoding.GetEncoding(entryNameEncoding)))
                    {
                        foreach (var zipArchiveEntry in archive.Entries)
                            if (!zipArchiveEntry.FullName.EndsWith("/"))
                                fList.Add(Regex.Replace(zipArchiveEntry.FullName.Replace("/", @"\"), @"^\\*", ""));
                    }
                }
            }
            catch (Exception e)
            {
                errMessage = e.Message;
            }
            return fList;
        }

        /// <summary>
        /// 获取主文件夹
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <returns></returns>
        public string GetRootDirectory(string zipFilePath)
        {
            try
            {
                using (var zipToOpen = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, true, Encoding.GetEncoding(entryNameEncoding)))
                    {
                        foreach (var zipArchiveEntry in archive.Entries)
                        {
                            var tmp = zipArchiveEntry.FullName;
                            tmp = tmp.Replace(@"\", "/");
                            tmp = tmp.Substring(0, tmp.IndexOf('/'));
                            return tmp;
                        }

                    }
                }

                return "";
            }
            catch (Exception)
            {
                return "";
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="zipFilePath"></param>
        /// <returns></returns>
        public byte[] Read(string zipFilePath, string entryName)
        {
            using (var zipToOpen = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read, true, Encoding.GetEncoding(entryNameEncoding)))
                {
                    var count = archive.Entries.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var entry = archive.Entries[i];
                        if (!entry.FullName.EndsWith("/") &&  entry.FullName == entryName)
                        {
                            var content = new byte[entry.Length];
                            entry.Open().Read(content, 0, content.Length);
                            return content;
                        }
                    }
                }
            }

            return null;
        }

        #endregion


        #region   私有方法



        #endregion


    }//Class_end

    public class UnZipProgressEventArgs
    {
        public long Size { get; set; }

        public int Index { get; set; }

        public int Count { get; set; }

        public string Path { get; set; }

        public string Name { get; set; }

    }//Class_end

    public class CompressProgressEventArgs
    {
        public long Size { get; set; }

        public int Index { get; set; }

        public int Count { get; set; }

        public string Path { get; set; }

        public string Name { get; set; }
    }

}
