// ****************************************
// FileName:FileHelper
// Description:FileHelper
// Tables:
// Author:Burney
// Create Date:2015/5/22 15:24:10
// Revision History:
// ****************************************

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeSeparateTool
{
    /// <summary>
    /// 文件操作
    /// </summary>
    public static class FileHelper
    {
        #region 文件路径配置

        private static readonly String _FilePath = ConfigurationManager.AppSettings["FilePath"] + "\\" + "BLL.csproj";
        
        #endregion

        #region 项目文件管理

        /// <summary>
        /// 将子文件添加到项目中，并依附于原文件
        /// </summary>
        /// <param name="fileString">文件字符串</param>
        /// <param name="dependFileName">原文件名</param>
        /// <param name="oldFileString">原文件字符串</param>
        public static void ManageProjectFile(String fileString, String dependFileName, String oldFileString)
        {
            String[] content = File.ReadAllLines(_FilePath);

            Int32 index = 0;
            StringBuilder startContent = new StringBuilder();
            StringBuilder endContent = new StringBuilder();
            StringBuilder newContent = new StringBuilder();
            Boolean isContainsFile = false;

            //找到添加文件的Index
            for (Int32 i = 0; i < content.Count(); i++)
            {
                //判断原文件是否包含在项目里面 如果本身不包含在内   则不做处理
                if (content[i].Contains(dependFileName))
                {
                    //匹配双引号之间的内容
                    System.Text.RegularExpressions.Match mc = System.Text.RegularExpressions.Regex.Match(content[i],
                       "(?<=\").*?(?=\")");
                    if (mc.ToString() == oldFileString) isContainsFile = true;
                }

                startContent.AppendLine(content[i]);
                if ((content[i].Contains("<Compile Include=") && content[i].Contains("/>") ||
                     content[i].Contains("</Compile>")) && content[i + 1].Contains("</ItemGroup>"))
                {
                    if (isContainsFile)
                    {
                        index = i;
                        break;
                    }
                    return;
                }
            }

            for (int i = index + 1; i < content.Count(); i++)
            {
                endContent.AppendLine(content[i]);
            }

            newContent.AppendLine(String.Format("    <Compile Include=\"{0}\">", fileString));
            newContent.AppendLine(String.Format("    <DependentUpon>{0}</DependentUpon>", dependFileName));
            newContent.AppendLine(String.Format("    </Compile>"));

            File.WriteAllText(_FilePath, startContent.Append(newContent).Append(endContent).ToString());

        }
        
        #endregion

    }
}
