using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model
{
    /// <summary>
    /// 查询结果
    /// </summary>
    public class SearchResult
    {
        public SearchResult()
        {
            this.PaggerData = new Pagger();
        }
        /// <summary>
        /// 查询结果
        /// </summary>
        public object ResultData { get; set; }
        /// <summary>
        /// 分页信息
        /// </summary>
        public Pagger PaggerData { get; set; }
    }

    public class Pagger
    {
        public Pagger()
        {
            this.PageIndex = 1;
            this.PageSize = 50;
            this.Direction = "desc";
        }
        /// <summary>
        /// 当前页码
        /// </summary>
        public int PageIndex { get; set; }
        /// <summary>
        /// 每页显示的记录数
        /// </summary>
        public int PageSize { get; set; }
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// 排序字段
        /// </summary>
        public string OrderField { get; set; }
        /// <summary>
        /// 排序方向（默认降序  升序：asc 降序：desc）
        /// </summary>
        public string Direction { get; set; }
        /// <summary>
        /// 查询起始序号
        /// </summary>
        public int BeginIndex
        {
            get { return (this.PageIndex - 1) * this.PageSize + 1; }
        }
        /// <summary>
        /// 查询结束序号
        /// </summary>
        public int EndIndex
        {
            get { return this.PageIndex * this.PageSize; }
        }
        /// <summary>
        /// 一共多少页
        /// </summary>
        public int PageCount
        {
            get { return TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1); }
        }
        /// <summary>
        /// 是否取所有数据
        /// </summary>
        public bool GetTotal { get; set; }
    }
}
