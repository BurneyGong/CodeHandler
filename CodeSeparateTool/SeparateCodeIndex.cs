// ****************************************
// FileName:SeparateCodeIndex
// Description:SeparateCodeIndex
// Tables:
// Author:Burney
// Create Date:2015/5/25 10:16:41
// Revision History:
// ****************************************

using System;

namespace CodeSeparateTool
{
    /// <summary>
    /// 分离代码索引
    /// </summary>
    public class SeparateCodeIndex
    {
        /// <summary>
        /// 开始位置
        /// </summary>
        public Int32 StartIndex { get; set; }
        /// <summary>
        /// 结束位置
        /// </summary>
        public Int32 EndIndex { get; set; }
    }

    /// <summary>
    /// 方法标识
    /// </summary>
    public class MethodReg
    {
        /// <summary>
        /// 方法前缀（需要分离的方法）
        /// </summary>
        public String ForwardReg { get; set; }
        /// <summary>
        /// 用于标识此类方法的文件名后缀
        /// </summary>
        public String BackReg { get; set; }
    }
}
