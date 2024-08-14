using System;
using System.Collections.Generic;
using My.DynamicList;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace My.DynamicList
{
    public class DynamicList : MonoBehaviour
    {
        /// <summary>
        /// 方向
        /// </summary>
        public enum Direction
        {
            None,
            Horizontal,
            Vertical,
        }

        /// <summary>
        /// 对齐
        /// </summary>
        public enum AlignMode
        {
            HorizontalTop,
            HorizontalMid,
            HorizontalBottom,
            HorizontalCutom,
            VerticalLeft,
            VerticalMid,
            VerticalRight,
            VerticalCustom,
        }

        /// <summary>
        /// 子类型
        /// </summary>
        public enum SibilingType
        {
            None,
            First,
            Last,
        }

        [Serializable]
        public struct Padding
        {
            public float left;
            public float right;
            public float top;
            public float bottom;
        }


        [Header("滑动方向")] [SerializeField] private Direction dir;

        [Header("对齐方向")] [SerializeField] private AlignMode mode;

        [Header("取对象的时候设置物体在父物体的哪个位置")] [SerializeField]
        private SibilingType siblingType;

        [Header("对象池类型")] [SerializeField] private ObjectPool<TestItem> poolType;

        [Header("内边距")] [SerializeField] private Padding padding;

        [Header("间距")] [SerializeField] private float space;

        [Header("滑动条")] [SerializeField] private ScrollRect scroll;

        [Header("实例化item的content")] [SerializeField]
        private RectTransform content;

        [Header("需要动态的Item")] [SerializeField] private RectTransform dynamicListObj;

        [Header("多克隆的个数，效果不好可以动态扩充")] [SerializeField]
        private int MoreCloneCount = 3;

        [Header("左右两边需要提前刷新的个数，<=MoreCloneCount / 2")] [SerializeField]
        private int MoreRefreshCount = 1;


        private List<DynamicListBaseData> datas = new(0);

        private RectTransform[] dynamicListItems = new RectTransform[0];

        private bool isInitData;

        #region 信息

        private int lastContentStartInfo = 0;
        private int lastContentEndInfo = 0;
        private Vector2 itemSize;
        private Vector2 scrollRectSize;
        private Vector2 scrollRange;
        private float posFix = default;

        private Dictionary<int, bool> itemShowState = new(0);
        private List<int> showIdx = new(0);

        #endregion

        private void Awake()
        {
            itemSize = dynamicListObj.rect.size;
            scrollRectSize = scroll.GetComponent<RectTransform>().rect.size;
        }

        /// <summary>
        /// 初始化列表
        /// </summary>
        public void InitDynamicList(List<DynamicListBaseData> initDatas, bool needReposTranform = true)
        {
            isInitData = false;
            datas.Clear();
            datas.AddRange(initDatas);
            posFix = 0;

            SelScrollMoveType();
            //  计算创建的Item数量和位置
            switch (dir)
            {
                case Direction.Horizontal:
                    OnCalcHorizontal(needReposTranform);
                    break;
                case Direction.Vertical:
                    OnCalcVertical(needReposTranform);
                    break;
                default:
                    Debug.LogError("没有设置方向或者两个方向都勾了，请检查");
                    break;
            }

            lastContentStartInfo = -1;
            lastContentEndInfo = -1;
            isInitData = true;
        }

        /// <summary>
        /// 更新数据(数据不变)
        /// 默认数据里面是有索引的，请勿忘记赋值
        /// </summary>
        /// <param name="updateData">数据</param>
        public void UpdateStaticData(DynamicListBaseData updateData)
        {
            isInitData = false;
            if (dynamicListItems.Length == 0)
            {
                return;
            }

            for (int i = 0; i < datas.Count; i++)
            {
                if (datas[i].idx == updateData.idx)
                {
                    datas[i] = updateData;
                    break;
                }
            }

            for (int i = 0; i < dynamicListItems.Length; i++)
            {
                var dyItem = dynamicListItems[i].GetComponent<IDynamicListItem>();
                if (dyItem.BaseData != null && dyItem.BaseData.idx == updateData.idx)
                {
                    SetItemPos(dynamicListItems[i], updateData.idx);
                    dyItem.OnRender(updateData);
                    break;
                }
            }
            isInitData = true;
        }

        /// <summary>
        /// 计算水平的
        /// </summary>
        void OnCalcHorizontal(bool needReposTranform = true)
        {
            //  content长度
            content.sizeDelta = new Vector2(
                padding.left + (space + itemSize.x) * (datas.Count - 1) + itemSize.x + padding.right,
                content.sizeDelta.y);
            var scrollRangeMax = content.rect.width - scrollRectSize.x;
            if (scrollRangeMax < 0)
            {
                scrollRangeMax = 0;
            }

            scrollRange = new Vector2(0, scrollRangeMax);
            if (needReposTranform)
            {
                content.anchoredPosition = Vector2.zero;
            }
            int maxCreateCount = Mathf.CeilToInt(scrollRectSize.x / (itemSize.x + space)) + 2 + MoreCloneCount;
            dynamicListItems = UIObjectPoolMgr<TestItem>.Instance.OnFetch(poolType, dynamicListObj, maxCreateCount, content);
            
            foreach (var item in dynamicListItems)
            {
                switch (siblingType)
                {
                    case SibilingType.First:
                        item.SetAsFirstSibling();
                        break;
                    case SibilingType.Last:
                        item.SetAsLastSibling();
                        break;
                }

                var dyItem = item.GetComponent<IDynamicListItem>();
                if (dyItem != null)
                {
                    dyItem.OnCreate();
                }
            }

            posFix += padding.bottom - padding.top;
            switch (mode)
            {
                case AlignMode.HorizontalTop:
                    break;
                case AlignMode.HorizontalMid:
                    posFix -= content.rect.size.y / 2;
                    break;
                case AlignMode.HorizontalBottom:
                    posFix -= content.rect.size.y - itemSize.y;
                    break;
                case AlignMode.HorizontalCutom:
                    posFix = dynamicListObj.GetComponent<RectTransform>().anchoredPosition.y;
                    break;
            }

            for (int i = 0; i < dynamicListItems.Length; i++)
            {
                var item = dynamicListItems[i];
                if (i < datas.Count)
                {
                    SetItemPos(item, i);
                    var dyItem = item.GetComponent<IDynamicListItem>();
                    if (dyItem != null)
                    {
                        dyItem.OnRender(datas[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 计算垂直的
        /// </summary>
        void OnCalcVertical(bool needReposTranform = true)
        {
            //  content长度
            content.sizeDelta = new Vector2(content.sizeDelta.x
                , padding.top + (space + itemSize.y) * (datas.Count - 1) + itemSize.y + padding.bottom);
            var scrollRangeMax = content.rect.height - scrollRectSize.y;
            if (scrollRangeMax < 0)
            {
                scrollRangeMax = 0;
            }

            scrollRange = new Vector2(0, scrollRangeMax);
            if (needReposTranform)
            {
                content.anchoredPosition = Vector2.zero;
            }
            int maxCreateCount = Mathf.CeilToInt(scrollRectSize.y / (itemSize.y + space)) + 2 + MoreCloneCount;
            dynamicListItems = UIObjectPoolMgr<TestItem>.Instance.OnFetch(poolType, dynamicListObj, maxCreateCount, content);
            
            foreach (var item in dynamicListItems)
            {
                switch (siblingType)
                {
                    case SibilingType.First:
                        item.SetAsFirstSibling();
                        break;
                    case SibilingType.Last:
                        item.SetAsLastSibling();
                        break;
                }

                var dyItem = item.GetComponent<IDynamicListItem>();
                if (dyItem != null)
                {
                    dyItem.OnCreate();
                }
            }
            posFix += padding.left - padding.right;
            switch (mode)
            {
                case AlignMode.VerticalLeft:
                    break;
                case AlignMode.VerticalMid:
                    posFix += content.rect.size.x / 2;
                    break;
                case AlignMode.VerticalRight:
                    posFix += content.rect.size.x - itemSize.x;
                    break;
                case AlignMode.VerticalCustom:
                    posFix = dynamicListObj.GetComponent<RectTransform>().anchoredPosition.x;
                    break;
            }

            for (int i = 0; i < dynamicListItems.Length; i++)
            {
                var item = dynamicListItems[i];
                if (i < datas.Count)
                {
                    SetItemPos(item, i);
                    var dyItem = item.GetComponent<IDynamicListItem>();
                    if (dyItem != null)
                    {
                        dyItem.OnRender(datas[i]);
                    }
                }
            }
        }

        private void OnDisable()
        {
            OnDispose();
        }

        private void LateUpdate()
        {
            if (!isInitData)
            {
                return;
            }

            OnComputeContent();
        }

        /// <summary>
        /// 改变的时候调用
        /// </summary>
        void OnComputeContent()
        {
            var idxInfo = GetStartPos(dir);
            RefreshItemInfo(idxInfo.Item1, idxInfo.Item2);
        }

        /// <summary>
        /// 获得位置
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public (int, int) GetStartPos(Direction dir)
        {
            float curStartMoveDis = default;
            float curEndMoveDis = default;
            int showStartDataIdx = 0;
            int showEndDataIdx = 0;
            switch (dir)
            {
                case Direction.Horizontal:
                    curStartMoveDis = Mathf.Clamp(-content.anchoredPosition.x - padding.left, scrollRange.x,
                        scrollRange.y);
                    curEndMoveDis = curStartMoveDis + scrollRectSize.x;

                    showStartDataIdx = (int)(curStartMoveDis / (itemSize.x + space)) - MoreRefreshCount;
                    showEndDataIdx = (int)(curEndMoveDis / (itemSize.x + space)) + MoreRefreshCount;
                    break;
                case Direction.Vertical:
                    curStartMoveDis = Mathf.Clamp(content.anchoredPosition.y + padding.top, scrollRange.x,
                        scrollRange.y);
                    curEndMoveDis = curStartMoveDis + scrollRectSize.y;

                    showStartDataIdx = (int)(curStartMoveDis / (itemSize.y + space)) - MoreRefreshCount;
                    showEndDataIdx = (int)(curEndMoveDis / (itemSize.y + space)) + MoreRefreshCount;
                    break;
            }

            return (showStartDataIdx, showEndDataIdx);
        }

        /// <summary>
        /// 计算Item刷新
        /// </summary>
        /// <param name="showStartDataIdx">起始索引</param>
        /// <param name="showEndDataIdx">终点索引</param>
        void RefreshItemInfo(int showStartDataIdx, int showEndDataIdx)
        {
            if (lastContentStartInfo == showStartDataIdx && lastContentEndInfo == showEndDataIdx)
            {
                return;
            }

            lastContentStartInfo = showStartDataIdx;
            lastContentEndInfo = showEndDataIdx;

            RefreshItem(showStartDataIdx, showEndDataIdx);

        }

        /// <summary>
        /// 刷新
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        void RefreshItem(int showStartDataIdx, int showEndDataIdx)
        {
            showIdx.Clear();
            itemShowState.Clear();

            //  设置隐藏显示
            for (int i = 0; i < dynamicListItems.Length; i++)
            {
                var dyItem = dynamicListItems[i].GetComponent<IDynamicListItem>();
                bool canShow = false;
                if (dyItem.BaseData != null)
                {
                    if (dyItem.BaseData.idx >= showStartDataIdx && dyItem.BaseData.idx <= showEndDataIdx)
                    {
                        canShow = true;
                        showIdx.Add(dyItem.BaseData.idx);
                    }
                }

                dynamicListItems[i].gameObject.SetActive(canShow);
                itemShowState[i] = canShow;
            }
            
            //  刷新数据
            int dataStart = Mathf.Clamp(showStartDataIdx, 0, datas.Count);
            int dataEnd = Mathf.Clamp(showEndDataIdx, 0, datas.Count);

            //  showIdx装原item的BaseData的Idx=>data的idx，itemShowState的key是dynamicItems的下标
            for (int i = dataStart; i < dataEnd; i++)
            {
                if (showIdx.Contains(i))
                {
                    continue;
                }

                int findIdx = -1;
                foreach (var kvp in itemShowState)
                {
                    if (!kvp.Value)
                    {
                        findIdx = kvp.Key;
                        break;
                    }
                }

                if (findIdx >= 0)
                {
                    itemShowState.Remove(findIdx);

                    var item = dynamicListItems[findIdx];
                    SetItemPos(item, i);
                    item.gameObject.SetActive(true);
                    item.GetComponent<IDynamicListItem>().OnRender(datas[i]);
                }
            }
        }

        /// <summary>
        /// 获得item的位置
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        void SetItemPos(RectTransform rect, int idx)
        {
            switch (dir)
            {
                case Direction.None:
                    break;
                case Direction.Horizontal:
                    rect.anchoredPosition = new Vector2(padding.left + (itemSize.x + space) * idx, posFix);
                    break;
                case Direction.Vertical:
                    rect.anchoredPosition = new Vector2(posFix, -padding.top - (itemSize.y + space) * idx);
                    break;
            }
        }

        /// <summary>
        /// 获取位置
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public Vector2 GetPos(int idx)
        {
            switch (dir)
            {
                case Direction.None:
                    break;
                case Direction.Horizontal:
                    return new Vector2(padding.left + (itemSize.x + space) * idx, posFix);
                case Direction.Vertical:
                    return new Vector2(posFix, -padding.top - (itemSize.y + space) * idx);
            }

            return Vector2.zero;
        }

        /// <summary>
        /// 选择滑动条滑动的类型
        /// </summary>
        void SelScrollMoveType()
        {
            switch (dir)
            {
                case Direction.Horizontal:
                    scroll.horizontal = true;
                    scroll.vertical = false;
                    break;
                case Direction.Vertical:
                    scroll.horizontal = false;
                    scroll.vertical = true;
                    break;
            }
        }

        public DynamicListBaseData GetItemData(int idx)
        {
            if (idx < 0 || idx >= datas.Count)
            {
                return null;
            }

            return datas[idx];
        }

        /// <summary>
        /// 重新设置位置
        /// </summary>
        public void ResetPos()
        {
            if (dynamicListItems.Length == 0)
            {
                return;
            }

            for (int i = lastContentStartInfo; i <= lastContentEndInfo; i++)
            {
                if (i < 0 || i > dynamicListItems.Length)
                {
                    continue;
                }

                var dyItem = dynamicListItems[i].GetComponent<IDynamicListItem>();
                if (dyItem.BaseData != null)
                {
                    SetItemPos(dynamicListItems[i], dyItem.BaseData.idx);
                }
            }
        }

        /// <summary>
        /// 释放，OnDisable的时候使用
        /// </summary>
        public void OnDispose()
        {
            isInitData = false;
            datas.Clear();
            foreach (var item in dynamicListItems)
            {
                var dyItem = item.GetComponent<IDynamicListItem>();
                if (dyItem != null)
                {
                    dyItem.OnDispose();
                }
            }

            UIObjectPoolMgr<TestItem>.Instance.OnRecycle(poolType, dynamicListItems);
            dynamicListItems = new RectTransform[0];
        }
    }
}