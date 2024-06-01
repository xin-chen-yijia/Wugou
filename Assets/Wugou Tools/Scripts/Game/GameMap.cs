using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wugou;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// GameMap，游戏地图
    /// </summary>
    public class GameMap
    {
        /// <summary>
        /// 当前版本，用于比对
        /// </summary>
        public const int kLatestVersion = 2;

        public long timestamp;     // 时间戳

        public string rawContent { get; private set; } = "{}";

        public int version;     //版本
        public string name;     //名称
        public string author;   //作者
        public string createTime;   //时间
        public string description;  //简介

        public string sceneName;    // 场景的名称，用于显示
        public string scene;    // 场景

        // entity的数量
        public int entityCount;

        // 最大玩家数量 
        public int maxPlayerCount;

        // 游戏脚本
        public string gameScript;

        // 是否模拟天气
        public bool needWeather;

        // 模拟天气
        public WeatherDesc weather = new WeatherDesc();

        // 依赖的assetbundle
        public List<string> assetbunldes = new List<string>();

        // 保留字段
        public string reserve;

        private GameMap() 
        {

        }

        public static GameMap Create()
        {
            GameMap map = new GameMap();

            return map;
        }

        /// <summary>
        /// 注意，这里不会解析entities，解析entities会进行实例化，在不加载gamemap的情况下是不需要的
        /// </summary>
        /// <param name="content"></param>
        public void Parse(string content)
        {
            this.rawContent = content;

            GameMapReader reader = new GameMapReader(content);
            version = reader.Read<int>("version",null, -1);
            timestamp = reader.Read<long>("timestamp", null, -1);
            name = reader.Read("name");
            author = reader.Read("author");
            createTime = reader.Read("createTime");
            description = reader.Read("description");

            scene = reader.Read("gameworld/scene");
            sceneName = reader.Read("gameworld/sceneName");

            maxPlayerCount = reader.Read<int>("maxPlayerCount");

            //
            gameScript = reader.Read<string>("gameworld/gameScript");

            //
            needWeather = reader.Read<bool>("gameworld/needWeather");
            weather = reader.Read<WeatherDesc>("gameworld/weather");

            reserve = reader.Read("reserve");
        }

        private static bool CompareBase(GameMap lhs , GameMap rhs)
        {
            bool flag = (object)lhs == null;
            bool flag2 = (object)rhs == null;
            if (flag2 && flag)
            {
                return true;
            }

            if (flag2)
            {
                return false;
            }

            if (flag)
            {
                return false;
            }

            return lhs.name == rhs.name && lhs.createTime == rhs.createTime && lhs.author == rhs.author;
        }

        public static bool operator ==(GameMap lhs, GameMap rhs)
        {
            return CompareBase(lhs, rhs);
        }

        public static bool operator !=(GameMap lhs, GameMap rhs)
        {
            return !CompareBase(lhs, rhs);
        }

        public override bool Equals(object obj)
        {
            GameMap other = obj as GameMap;
            return CompareBase(this, other);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// 读取地图文件
    /// </summary>
    public class GameMapReader
    {
        private JObject jo_;

        public GameMapReader(string content)
        {
            jo_ = JObject.Parse(content);
        }

        public List<GameEntity> ReadEntities()
        {
            return ReadArray<GameEntity>("gameworld/entities", JsonSerializerGlobal.commonSerializer);
        }

        public Dictionary<string, string> ReadDependencies()
        {
            return Read<Dictionary<string, string>>("dependencies",null,new Dictionary<string, string>());
        }

        //public WeatherDesc ReadWeather()
        //{
        //    var tmp = jo_["gameworld"] == null ? null : jo_["gameworld"]["weather"];
        //    if (tmp == null)
        //    {
        //        return new WeatherDesc();
        //    }
        //    return tmp.ToObject<WeatherDesc>();
        //}

        /// <summary>
        /// 读取"xxx/xxx/xx"属性的内容
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public JToken ReadInternal(string property)
        {
            var parts = property.Split('/');
            JToken tmp = jo_;
            for (int i = 0; tmp != null && i < parts.Length; i++)
            {
                tmp = tmp[parts[i]];
            }
            return tmp;
        }

        /// <summary>
        /// 用于读取额外写入的内容
        /// </summary>
        /// <param name="property"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public string Read(string property, string defaultVal = "")
        {
            var tmp = ReadInternal(property);
            return tmp == null ? defaultVal : (tmp.ToString());
        }

        internal T Read<T>(string property, JsonSerializer jsonSerializer = null, T defaultValue = default(T))
        {
            JToken tmp = ReadInternal(property);
            if (tmp == null)
            {
                return defaultValue;
            }

            if(jsonSerializer == null)
            {
                return tmp.ToObject<T>();
            }
            else
            {
                return tmp.ToObject<T>(jsonSerializer);
            }
        }

        internal List<T> ReadArray<T>(string property, JsonSerializer jsonSerializer=null)
        {
            JToken tmp = ReadInternal(property);
            if (tmp == null)
            {
                return new List<T>();
            }

            if (jsonSerializer != null)
            {
                return (tmp as JArray).ToObject<List<T>>(jsonSerializer);
            }
            else
            {
                return (tmp as JArray).ToObject<List<T>>();
            }
        }
    }

    public class GameMapWriter
    {
        private JObject jo_;

        public GameMapWriter()
        {

        }

        public void Save(string path, GameMap map)
        {
            JObject jo = new JObject();
            jo.Add("version", map.version);

            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.UtcNow - epoch;
            long timestamp = (long)timeSpan.TotalSeconds;   // 计算最新时间戳

            jo.Add("timestamp", timestamp);
            jo.Add("name", map.name);
            jo.Add("maxPlayerCount", GameWorld.GetStartPositionCount());

            // write gameworld
            JObject gameWorldJo = new JObject();
            // scene
            gameWorldJo.Add("scene", JToken.FromObject(map.scene));
            gameWorldJo.Add("sceneName", JToken.FromObject(map.sceneName));
            // 写入gameentity
            gameWorldJo.Add("entities", JArray.FromObject(GameWorld.gameEntities.FindAll((entity) => { return entity.needSerialize; }), JsonSerializerGlobal.commonSerializer));
            // gamescripts
            gameWorldJo.Add("gameScript", JToken.FromObject(map.gameScript ?? ""));
            // 写入天气
            gameWorldJo.Add("needWeather", map.needWeather);
            gameWorldJo.Add("weather", JToken.FromObject(map.weather));

            jo.Add("gameworld", gameWorldJo);
            jo.Add("author", map.author);
            jo.Add("createTime", map.createTime);
            jo.Add("description", map.description);

            // 写入所依赖的Assetbundle
            // 用Assetbundle的md5来验证系统使用的Assetbundle和创建GameMap时用的Assetbundle是否是同一个版本
            JObject abMd5Jo = new JObject();

            // 根据场景和场景中的对象确定所依赖的Assetbundle
            HashSet<string> usedFS = new HashSet<string>();

            // 场景的assetbundle
            usedFS.Add(GetMountPoint(map.scene));

            foreach(var v in GameWorld.gameEntities)
            {
                var asset = v.asset;
                if (!string.IsNullOrEmpty(asset))
                {
                    usedFS.Add(GetMountPoint(asset));
                }
            }
            foreach(var mountPoint in usedFS)
            {
                var fs = GameAssetDatabase.GetFileSystem(mountPoint);
                if (fs != null)
                {
                    var abFS = fs as AssetBundleFileSystem;
                    if(abFS != null)
                    {
                        var loader = abFS.assetLoader;
                        abMd5Jo.Add(Path.GetFileName(loader.assetbundleDir), loader.GetDescFileMD5());
                    }
                }
            }

            //写入依赖
            jo.Add("dependencies", abMd5Jo);    

            // 保留信息
            jo.Add("reserve", map.reserve);
            // 写入文件
            File.WriteAllText(path, jo.ToString());
        }

        private static string GetMountPoint(string path)
        {
            var pos = path.IndexOf('/', 1);
            var mountPoint = path.Substring(0, pos);

            return mountPoint;
        }
    }
}