using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wugou;
using Wugou.Multiplayer;
using Wugou.UI;

namespace Wugou.Examples
{
    public class SimpleStatisticPage : UIBaseWindow
    {
        public GameObject rowContainer;
        public GameObject rowPrefab;

        public GameObject statsContainer;
        public GameObject statsPrefab;

        private GameObject listPage => transform.Find("ListPage").gameObject;
        private GameObject detailPage => transform.Find("DetailPage").gameObject;

        // Start is called before the first frame update
        void Start()
        {
            transform.Find("DetailPage/Buttons/OkButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                listPage.SetActive(true);
                detailPage.SetActive(false);
            });

            transform.Find("DetailPage/Buttons/SaveButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                listPage.SetActive(true);
                detailPage.SetActive(false);
            });
        }

        // Update is called once per frame
        void Update()
        {

        }

        private float clickTime = -1.0f;
        public void SetTrainingRecord(List<GameStats<SimpleGameSnapshot>> records)
        {
            GameObject lastCheck = null;
            Utils.FillContent<GameStats<SimpleGameSnapshot>>(rowContainer, rowPrefab, records, (item, record) =>
            {
                item.transform.Find("Name").GetComponent<TMP_Text>().text = record.name;
                item.transform.Find("Script").GetComponent<TMP_Text>().text = record.gamemap;
                item.transform.Find("Duration").GetComponent<TMP_Text>().text = new TimeSpan(0, 0, (int)record.duration).ToString(@"hh\:mm\:ss");
                item.transform.Find("Checked").gameObject.SetActive(false);

                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // dublick click check
                    if (Time.time - clickTime < 0.3f)
                    {
                        SetGameStats(record);

                        listPage.SetActive(false);
                        detailPage.SetActive(true);
                    }

                    clickTime = Time.time;

                    lastCheck?.SetActive(false);
                    lastCheck = item.transform.Find("Checked").gameObject;
                    lastCheck.SetActive(true);
                });
            });
        }

        public void SelectLastest()
        {
            // 选择最后一个
            var btn = rowContainer.transform.GetChild(rowContainer.transform.childCount - 1).GetComponent<Button>();
            btn.onClick.Invoke();
            btn.onClick.Invoke();
        }

        public void SetGameStats(GameStats<SimpleGameSnapshot> stats)
        {
            Utils.FillContent<SimpleGameSnapshot>(statsContainer, statsPrefab, stats.playerStats.Values.ToList(), (item, snapshot) =>
            {
                item.transform.Find("Name").GetComponent<TMP_Text>().text = $"{snapshot.name}";
                item.transform.Find("LaunchCount").GetComponent<TMP_Text>().text = $"{snapshot.score}";
                //item.transform.Find("HitsLabel").GetComponent<TMP_Text>().text = $"{b}";
                //item.transform.Find("HitRateLabel").GetComponent<TMP_Text>().text = $"{string.Format("{0:F2}%", c)}";
            });
        }


        public override void Show(bool asTop = false)
        {
            base.Show(asTop);

            SetTrainingRecord(GamePlay.gameStatsManager.GetAllGameStats<GameStats<SimpleGameSnapshot>>());
            listPage.SetActive(true);
            detailPage.SetActive(false);
        }
    }
}
