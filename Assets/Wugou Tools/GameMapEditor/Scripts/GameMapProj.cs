using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Wugou.Editor
{
    /// <summary>
    /// GameMap的编辑工程
    /// </summary>
    public class GameMapProj
    {
        public string path { get; private set; }

        public string name => System.IO.Path.GetFileName(path);

        public string gamemapFilePath => $"{path}/{Path.GetFileName(path)}{Gameplay.kGameMapFileSuffix}";

        public string dotFolder => $"{path}/.map";

        public string thumbnailPath => $"{dotFolder}/thumbnail.jpg";

        public string projFile => $"{path}/editor.proj";

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

        public GameMapProj(string path) 
        {
            this.path = path;

            Directory.CreateDirectory(dotFolder);

            if(File.Exists(projFile))
            {
                var content = File.ReadAllText(projFile);
                JObject jo = JObject.Parse(content);
                if (jo["packages"] != null)
                {
                    packages = jo["packages"].ToObject<List<string>>();
                }
            }
        }

        /// <summary>
        /// 添加导入包的记录
        /// </summary>
        /// <param name="path"></param>
        public void Import(string path)
        {
            packages.Add(path);
        }

        public void Build()
        {
            // 先保存
            Save();

            // 创建文件夹，GameMap相关资产都放入这个文件夹
            var mapPath = $"{Gameplay.gameMapsPath}/{name}";
            Directory.CreateDirectory(mapPath);

            // GameMap
            File.Copy(gamemapFilePath, $"{mapPath}/{name}{Gameplay.kGameMapFileSuffix}", true);

            // packages
            foreach (var v in packages)
            {
                // TODO: 根据是否引用打包
                Utils.CopyDirectory($"{path}/{v}", $"{mapPath}/{v}");
            }

            if (File.Exists(thumbnailPath))
            {
                File.Copy(thumbnailPath, $"{mapPath}/{name}{Path.GetExtension(thumbnailPath)}", true);
            }

            // 打包
            GameMapPackage.Build(mapPath);

            // 删除
            Directory.Delete(mapPath, true );
        }

        public void Save()
        {
            Directory.CreateDirectory(path);

            GameMapWriter writer = new GameMapWriter();
            writer.Save(gamemapFilePath, gameMap);

            if(GameMapEditor.instance?.previewCamera)
            {
                // 生成缩略图
                Utils.CreateSceneThumbnail($"{thumbnailPath}", Screen.width, Screen.height, GameMapEditor.instance.previewCamera);
            }


            // 编辑相关状态保存
            JObject jo = new JObject();
            jo.Add("packages",JToken.FromObject(packages));

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
}
