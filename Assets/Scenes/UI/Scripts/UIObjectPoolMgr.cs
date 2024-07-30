

using System;
using UnityEngine;
using UnityEngine.Pool;

public class UIObjectPoolMgr<T> where T : class
{
    private static UIObjectPoolMgr<T> instance;

    private RectTransform[] _rectTransforms;

    public static UIObjectPoolMgr<T> Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UIObjectPoolMgr<T>();
            }
            return instance;
        }
    }

    public  void OnRecycle(ObjectPool<T> opType,RectTransform[] dynamicListItems)
    {
        
    }

    public RectTransform[] OnFetch(ObjectPool<T> opType, RectTransform dynamicListItems, int maxCreateCount,
        RectTransform content)
    {
        RectTransform[] rectTransforms = new RectTransform[maxCreateCount];
        // for (int i = 0; i < maxCreateCount; i++)
        // {
        //     rectTransforms[i] = new RectTransform();
        // }
        // opType.Get();
        return rectTransforms;
    }

}