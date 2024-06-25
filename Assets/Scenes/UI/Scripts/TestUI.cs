using System.Collections.Generic;
using My.DynamicList;
using UnityEngine;

public class TestUI : MonoBehaviour
{
    /// <summary>
    /// 动态列表
    /// </summary>
    public DynamicList dynamicList;
    /// <summary>
    /// 生成数量
    /// </summary>
    public int Count;
    
    protected void Awake()
    {
        var list = new List<DynamicListBaseData>(0);
        for (int i = 0; i < Count; i++)
        {
            var data = new TestItem.TestItemData(i);
            data.idx = i;
            data.tag = "DynamicList:";
            list.Add(data);
        }
        //  全局只需要调用这个接口
        dynamicList.InitDynamicList(list);


        list[0].idx = 1;

        //  刷新
        dynamicList.UpdateStaticData(list[0]);
    }
}