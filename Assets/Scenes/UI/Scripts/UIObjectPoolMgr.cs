

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
        return _rectTransforms;
    }

}