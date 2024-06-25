namespace My.DynamicList
{
    public abstract class DynamicListBaseData
    {
        /// <summary>
        /// 索引
        /// </summary>
        public int idx = default;

        public DynamicListBaseData(int idx)
        {
            this.idx = idx;
        }
    }
    
    public interface IDynamicListItem
    {
        public DynamicListBaseData BaseData { get; set; }

        /// <summary>
        /// 创建
        /// </summary>
        public void OnCreate();

        /// <summary>
        /// 刷新逻辑
        /// </summary>
        /// <param name="baseData">动态数据</param>
        public void OnRender(DynamicListBaseData baseData);

        /// <summary>
        /// 释放
        /// </summary>
        public void OnDispose();
    }
}