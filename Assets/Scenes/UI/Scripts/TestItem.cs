using My.DynamicList;
using TMPro;
using UnityEngine;

public class TestItem : MonoBehaviour, IDynamicListItem
{
    public class TestItemData : DynamicListBaseData
    {
        public string tag;

        public TestItemData(int idx) : base(idx)
        {
        }
    }
    
    public TextMeshProUGUI txt;

    public DynamicListBaseData BaseData { get; set; }

    public void OnCreate()
    {
    }

    public void OnRender(DynamicListBaseData baseData)
    {
        var data = baseData as TestItemData;
        BaseData = data;

        txt.text = data.tag + data.idx;
        
    }

    public void OnDispose()
    {
        BaseData = null;
    }
}