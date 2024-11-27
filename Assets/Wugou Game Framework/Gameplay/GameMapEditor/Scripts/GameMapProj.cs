using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System;

namespace Wugou.Editor
{
    /// <summary>
    /// GameMap的编辑工程
    /// </summary>
    public class GameMapProj
    {
        public string path { get; private set; }

        public string name => System.IO.Path.GetFileName(path);

        public string gamemapFilePath => $"{path}/{Path.GetFileName(path)}{GameConsole.kGameMapFileSuffix}";

        public string dotFolder => $"{path}/.map";

        public string thumbnailPath => $"{dotFolder}/thumbnail.jpg";

        public string projFile => $"{path}/editor.proj";

        public string resourcePath => $"{path}/{GameConsole.resourceName}";

        // 
        private GameMap _gameMap = null;

        // 导入的包
        public List<string> packages = new List<string>();

        /// <summary>
        /// 编辑的地图
        /// </summary>
        public GameMap gameMap {
            get 
            { 
                if( _gameMap == null)
                {
                    GameMapFileParser parser = new GameMapFileParser();
                    _gameMap = parser.Parse(gamemapFilePath);
                }

                return _gameMap;
            } 
            set
            {
                _gameMap = value;
            }
        }

        /// <summary>
        /// 用于记录摄像机最后方位等信息
        /// </summary>
        public class CameraCache
        {
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
        }

        public CameraCache cameraCache { get; set; }


        public GameMapProj(string path) 
        {
            this.path = path;

            Directory.CreateDirectory(dotFolder);

            if(File.Exists(projFile))
            {
                var content = File.ReadAllText(projFile);
                Parse(content);
            }
        }

        /// <summary>
        /// 获取包名
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetPackageName(string path)
        {
            return Path.GetFileName(path); ;
        }

        /// <summary>
        /// 获取工程中的包路径
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public string GetPackagePath(string packageName)
        {
            return $"{resourcePath}/{packageName}";
        }

        /// <summary>
        /// 向工程中导入第三方包
        /// </summary>
        /// <param name="path"></param>
        public void ImportPackage(string path)
        {
            var packageName = GetPackageName(path);
            packages.Add(packageName);

            Directory.CreateDirectory(this.resourcePath);
            var dst = GetPackagePath(packageName);// $"{this.resourcePath}/{packageName}";
            if (!Directory.Exists(dst))
            {
                // 拷贝导入的资产到目标工程
                Utils.CopyDirectory(path, dst);
            }

            Save(nameof(packages), packages);
        }

        /// <summary>
        /// 从已构建的地图导入为工程
        /// </summary>
        /// <param name="content"></param>
        public void ImportFromGameMap(GameMapPackage gameMapPackage)
        {
            //var gameMap = GameMap.Create();
            //gameMap.Parse(gameMapPackage_.gameMap.rawContent);
            //proj.gameMap = gameMap;
            //proj.SetThumbnail(gameMapPackage_.GetThumbnail());

            // 先新建，在替换
            SetThumbnail(gameMapPackage.GetThumbnail());
            gameMap = GameMap.Create();
            Save();

            JObject jo = JObject.Parse(gameMapPackage.gameMap.rawContent);
            jo["name"] = name;
            File.WriteAllText(gamemapFilePath, jo.ToString());

            //// save first
            //Gameplay.gameMapProjManager.Save(projName, proj);
        }

        /// <summary>
        /// 移除包
        /// </summary>
        /// <param name="packageName"></param>
        public void DeleteAssetPackage(string packageName)
        {
            if (packages.Contains(packageName))
            {
                packages.Remove(packageName);
            }

            var packagePath = GetPackagePath(packageName);// $"{resourcePath}/{packageName}";
            if (Directory.Exists(packagePath))
            {
                Directory.Delete(packagePath, true);
            }

            Save(nameof(packages), packages);
        }

        /// <summary>
        /// 工程中是否有这个包
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public bool HasAssetPackage(string packageName)
        {
            return packages.Contains(packageName);
        }

        public void Build()
        {
            // 创建文件夹，GameMap相关资产都放入这个文件夹
            var mapPath = $"{GameConsole.gameMapsPath}/{name}";
            Directory.CreateDirectory(mapPath);

            // GameMap
            File.Copy(gamemapFilePath, $"{mapPath}/{name}{GameConsole.kGameMapFileSuffix}", true);

            // packages
            // TODO: 根据是否引用打包
            if (Directory.Exists(resourcePath))
            {
                Utils.CopyDirectory($"{resourcePath}", $"{mapPath}/{GameConsole.resourceName}");
            }

            //foreach (var v in packages)
            //{
            //    // TODO: 根据是否引用打包
            //    Utils.CopyDirectory($"{path}/{v}", $"{mapPath}/{v}");
            //}

            if (File.Exists(thumbnailPath))
            {
                File.Copy(thumbnailPath, $"{mapPath}/{name}{Path.GetExtension(thumbnailPath)}", true);
            }

            // 打包
            GameMapPackage.Build(mapPath);

            // 删除
            Directory.Delete(mapPath, true );
        }

        public void Parse(string content)
        {
            JObject jo = JObject.Parse(content);
            if (jo[nameof(packages)] != null)
            {
                packages = jo[nameof(packages)].ToObject<List<string>>();
                if (jo["camera"] != null)
                {
                    cameraCache = jo["camera"].ToObject<CameraCache>(JsonSerializerGlobal.commonSerializer);
                }

            }
        }

        public void Save()
        {
            // 开始保存
            Directory.CreateDirectory(path);

            GameMapWriter writer = new GameMapWriter();
            writer.Save(gamemapFilePath, gameMap);

            if(GameMapEditor.instance?.editorCamera)
            {
                // 生成缩略图
                Utils.CreateSceneThumbnail($"{thumbnailPath}", Screen.width, Screen.height, GameMapEditor.instance.editorCamera);
            }


            // 编辑相关状态保存
            JObject jo = new JObject();
            jo.Add(nameof(packages), JToken.FromObject(packages));
            // 记录一些值
            if (GameMapEditor.instance)
            {
                cameraCache = new CameraCache();
                cameraCache.position = GameMapEditor.instance.editorCamera.transform.position;
                cameraCache.rotation = GameMapEditor.instance.editorCamera.transform.rotation;

                jo.Add("camera", JToken.FromObject(cameraCache, JsonSerializerGlobal.commonSerializer));
            }

            File.WriteAllText(projFile, jo.ToString());
        }

        /// <summary>
        /// 单独保存某个值，用于如导入时更改设置
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void Save(string key, object value)
        {
            var jo = JObject.Parse(File.ReadAllText(projFile));
            jo[key] = JToken.FromObject(value);
            File.WriteAllText(projFile, jo.ToString());
        }

        public void SetThumbnail(Texture2D texture)
        {
            if(texture == null)
            {
                return;
            }

            var data = texture.EncodeToJPG();
            File.WriteAllBytes(thumbnailPath, data );
        }
    }

    /// <summary>
    /// 解析GameMapProj
    /// </summary>
    public class GameMapProjAssetParser : IAssetParser<GameMapProj>
    {
        public GameMapProj Parse(string path)
        {
            var proj = new GameMapProj(path);
            return proj;
        }

        public void Save(string path, GameMapProj mapProj, bool overwrite = true)
        {
            mapProj.Save();
        }
    }
}
