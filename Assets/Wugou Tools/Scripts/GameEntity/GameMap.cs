using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wugou;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.IO;
using System.Xml.Schema;

namespace Wugou
{
    /// <summary>
    /// GameMap，游戏地图
    /// </summary>
    public class GameMap
    {
        public string rawContent { get; private set; }

        public int version;     //版本
        public string name;     //名称
        public string author;   //作者
        public string createTime;   //时间
        public string description;  //简介

        public AssetBundleScene scene = new AssetBundleScene();

        // entity的数量
        public int entityCount;

        // 最大玩家数量 
        public int maxPlayerCount;

        // 模拟天气
        public WeatherDesc weather = new WeatherDesc();

        // 保留字段
        public string reserve;

        /// <summary>
        /// 注意，这里不会解析entities，解析entities会进行实例化，在不加载gamemap的情况下是不需要的
        /// </summary>
        /// <param name="content"></param>
        public void Parse(string content)
        {
            this.rawContent = content;

            GameMapReader reader = new GameMapReader(content);
            version = reader.ReadVersion();
            name = reader.Read("name");
            author = reader.Read("author");
            createTime = reader.Read("createTime");
            description = reader.Read("description");

            scene = reader.ReadScene();

            maxPlayerCount = reader.ReadMaxPlayerCount();
            weather = reader.ReadWeather();
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

        private List<AssetBundleDesc> assetbundles_ = null;

        public GameMapReader(string content)
        {
            jo_ = JObject.Parse(content);
        }

        public int ReadVersion()
        {
            var tmp = jo_["version"];
            int mapVersion = tmp==null ? -1 : (int)tmp;
            return mapVersion;
        }

        public List<AssetBundleDesc> ReadAllAssetbundles()
        {
            var tmp = jo_["gameworld"] == null ? null : jo_["gameworld"]["assetbundles"];
            if(tmp == null)
            {
                return new List<AssetBundleDesc>();
            }

            List<AssetBundleDesc> assetBundles = (tmp as JArray).ToObject<List<AssetBundleDesc>>();// JsonConvert.DeserializeObject<List<AssetBundleDesc>>(tmp.ToString());
            return assetBundles;
        }

        public AssetBundleScene ReadScene()
        {
            if(assetbundles_ == null)
            {
                assetbundles_ = ReadAllAssetbundles();
            }

            var tmp = jo_["gameworld"] == null ? null : jo_["gameworld"]["scene"];
            if (tmp == null)
            {
                Logger.Warning("Game map's not have scene..");
                return new AssetBundleScene();
            }
            return JsonConvert.DeserializeObject<AssetBundleScene>(tmp.ToString(), new AssetBundleDescHashConvert(assetbundles_.ToDictionary(p => p.id)));
        }

        public List<GameEntity> ReadEntities()
        {
            var tmp = jo_["gameworld"] == null ? null : jo_["gameworld"]["entities"];
            if(tmp == null)
            {
                return new List<GameEntity>();
            }

            if (assetbundles_ == null)
            {
                assetbundles_ = ReadAllAssetbundles();
            }
            return (tmp as JArray).ToObject<List<GameEntity>>(new JsonSerializer() { Converters = {new GameEntityConverter(), new AssetBundleDescHashConvert(assetbundles_.ToDictionary(p => p.id)), new Vector3Converter()}});
        }

        public int ReadMaxPlayerCount()
        {
            var tmp = jo_["gameworld"] == null ? null : jo_["gameworld"]["maxPlayerCount"];
            if (tmp == null)
            {
                return 0;
            }
            return (int)tmp;
        }

        public WeatherDesc ReadWeather()
        {
            var tmp = jo_["gameworld"] == null ? null : jo_["gameworld"]["weather"];
            if(tmp == null)
            {
                return new WeatherDesc();
            }
            return tmp.ToObject<WeatherDesc>();
        }

        /// <summary>
        /// 用于读取额外写入的内容
        /// </summary>
        /// <param name="property"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public string Read(string property, string defaultVal = "")
        {
            var tmp = jo_[property];
            return tmp == null ? defaultVal : (tmp.ToString());
        }
    }

    public class GameMapWriter
    {
        private JObject jo_;
        private string path_;

        public GameMapWriter(string path)
        {
            jo_ = new JObject();
            path_ = path;
        }

        public void Write(string name, string value)
        {
            jo_.Add(name, value);
        }

        public void WriteVersion(int version)
        {
            jo_.Add("version", version);
        }

        public void WriteName(string name)
        {
            jo_.Add("name", name);
        }

        public void WriteGameWorld()
        {
            jo_.Add("gameworld", GameWorld.ToJson());
        }

        public void WriteGameMapDetail(GameMap map)
        {
            Write("author", map.author);
            Write("createTime", map.createTime);
            Write("description", map.description);
            Write("reserve", map.reserve);
        }

        public void Save()
        {
            // 写入文件
            File.WriteAllText(path_, jo_.ToString());
        }
    }

}