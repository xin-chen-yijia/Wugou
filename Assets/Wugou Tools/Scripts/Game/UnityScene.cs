using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace Wugou
{
    /// <summary>
    /// 对Unity AB包中的Scene的封装，
    /// </summary>
    public class UnityScene
    {
        public string name;
        public string type;
        public List<string> tags;
        public string thumbnail;
        public string scene;
        public string description;

        [JsonIgnore]
        public string path { get; private set; }

        public UnityScene(string path)
        {
            this.path = path;
        }
    }

    public class UnitySceneParser : IAssetParser<UnityScene>
    {
        public const string kSuffix = ".es";
        public UnityScene Parse(string path)
        {
            var fileName = Path.GetFileName(path);
            var filePath = $"{path}/{fileName}{kSuffix}";
            var scene = new UnityScene(path);
            if(File.Exists(filePath))
            {
                JsonConvert.PopulateObject(File.ReadAllText(filePath), scene);
            }
            else
            {
                Logger.Error($"UnityScene Parse error. {filePath} not exist....");
            }

            return scene;
        }

        public void Save(string path, UnityScene obj, bool overwrite = true)
        {
            throw new System.NotImplementedException();
        }
    }
}

