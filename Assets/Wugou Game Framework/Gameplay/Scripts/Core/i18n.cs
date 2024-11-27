using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// 用于国际化
    /// </summary>
    public static class Localization
    {
        public class TextItem
        {
            public string id;   // id
            public string cn;   // 中文  
            public string en;   // 英文
        }

        public enum Language
        {
            kChinese=0,
            kEnglish=1,
        }

        public static Language language { get; set; } = Language.kChinese;

        /// <summary>
        /// 所有的文本定义
        /// </summary>
        private static Dictionary<string, string> textItems_ = new Dictionary<string, string>();

        /// <summary>
        /// 加载定义文件
        /// </summary>
        /// <param name="path"></param>
        public static void Load(string path)
        {
            Debug.Assert(File.Exists(path));

            var content = File.ReadAllText(path);
            var items = JsonConvert.DeserializeObject<List<TextItem>>(content);
            textItems_.Clear(); // 先清理
            foreach (var item in items)
            {
                textItems_[item.id] = GetText(item);
            }
        }

        private static string GetText(TextItem item)
        {
            switch (language)
            {
                case Language.kChinese:
                    return item.cn;
                case Language.kEnglish:
                    return item.en;
                default:
                    Debug.Assert(false);
                    break;
            }

            return item.cn;
        }

        public static string GetString(string id)
        {
            if (textItems_.ContainsKey(id))
            {
                return textItems_[id];
            }

            return id;
        }
    }
}
