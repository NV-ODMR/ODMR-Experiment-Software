using System;
using System.Collections.Generic;

namespace ODMRLab.AI.DynamicExperiments
{
    /// <summary>
    /// 审核状态枚举
    /// </summary>
    public enum ReviewStatus
    {
        /// <summary>待审核</summary>
        Pending,
        /// <summary>已批准</summary>
        Approved,
        /// <summary>已拒绝</summary>
        Rejected
    }

    /// <summary>
    /// AI动态实验信息
    /// </summary>
    public class AIExperimentInfo
    {
        /// <summary>唯一标识</summary>
        public string Id { get; set; }
        
        /// <summary>实验名称</summary>
        public string Name { get; set; }
        
        /// <summary>实验描述</summary>
        public string Description { get; set; }
        
        /// <summary>源代码</summary>
        public string SourceCode { get; set; }
        
        /// <summary>完整类名（含命名空间）</summary>
        public string ClassName { get; set; }
        
        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>修改时间</summary>
        public DateTime ModifiedAt { get; set; }
        
        /// <summary>审核状态</summary>
        public ReviewStatus Status { get; set; }
        
        /// <summary>审核意见</summary>
        public string ReviewComment { get; set; }
        
        /// <summary>审核时间</summary>
        public DateTime? ReviewedAt { get; set; }
        
        /// <summary>源代码文件路径</summary>
        public string SourceFilePath { get; set; }
        
        /// <summary>编译产物路径</summary>
        public string AssemblyFilePath { get; set; }
        
        /// <summary>编译错误信息（如有）</summary>
        public string CompileErrors { get; set; }
        
        /// <summary>是否已编译成功</summary>
        public bool IsCompiled { get; set; }
    }
}
