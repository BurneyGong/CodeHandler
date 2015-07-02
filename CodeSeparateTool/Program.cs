using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeSeparateTool
{
    public class Program
    {
        #region 变量

        //文件列表
        private static Dictionary<String, String> _FileList = new Dictionary<String, String>();

        private static String _ConfigFilePath = ConfigurationManager.AppSettings["FilePath"];

        #endregion

        #region 主程序入口

        /// <summary>
        /// 主程序入口
        /// </summary>
        /// <param name="args"></param>
        public static void Main(String[] args)
        {
            String filePath = ConfigurationManager.AppSettings["FilePath"];

            //判断文件路径是否存在
            if (!Directory.Exists(filePath))
            {
                Console.WriteLine("文件路径不存在...");
                Console.ReadKey();
                return;
            }

            //判断是否存在项目文件
            if (!File.Exists(filePath + "//" + "BLL.csproj"))
            {
                Console.WriteLine("项目文件不存在...");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("代码分离进行中...");

            DirectoryInfo dirInfo = new DirectoryInfo(filePath);
            //获取
            Dictionary<String, String> file = GetAllFile(dirInfo);

            //分离代码
            SeparateCode(file);

            Console.WriteLine("已完成, 按任意键退出...");
            Console.ReadKey();
        }

        #endregion

        #region 代码分离

        /// <summary>
        /// 分离代码
        /// </summary>
        /// <param name="dic">文件信息</param>
        /// <returns></returns>
        public static void SeparateCode(Dictionary<String, String> dic)
        {
            foreach (var item in dic.Keys)
            {
                String path = dic[item] + "\\" + item;
                //读取文件
                String[] content = File.ReadAllLines(path);

                if (IsNeedHandle(content))
                {
                    //处理单个文件
                    FileHandler(content, dic[item], item);
                }
            }
        }

        #endregion

        #region 获取指定路径下的所有文件

        /// <summary>
        /// 获取指定类型的所有文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>文件列表</returns>
        private static Dictionary<String, String> GetAllFile(DirectoryInfo path)
        {
            FileInfo[] files = path.GetFiles("*.cs");

            foreach (FileInfo fi in files)
            {
                if (!_FileList.ContainsKey(fi.Name)) _FileList.Add(fi.Name, fi.DirectoryName);
            }

            DirectoryInfo[] allDir = path.GetDirectories();

            foreach (DirectoryInfo childFile in allDir)
            {
                GetAllFile(childFile);
            }
            return _FileList;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 文件处理
        /// </summary>
        /// <param name="content">需要处理的文件内容</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="originalFileName">文件名</param>
        private static void FileHandler(String[] content, String filePath, String originalFileName)
        {
            #region 变量

            //需要分离的代码标识列表
            List<MethodReg> methodRegList = new List<MethodReg>();
            methodRegList.Add(new MethodReg() {ForwardReg = " C_", BackReg = "_C"});
            methodRegList.Add(new MethodReg() {ForwardReg = " M_", BackReg = "_M"});
            
            Int32 classIndex = 0;
            StringBuilder titleContent = new StringBuilder();
            List<SeparateCodeIndex> indexList = new List<SeparateCodeIndex>();

            #endregion

            //获取类名
            String className = GetClassName(content, out titleContent, out classIndex);
            String oldFilePath = filePath + "\\" + originalFileName;
            String oldFileString = oldFilePath.Replace(_ConfigFilePath, String.Empty).TrimStart('\\');

            foreach (var regItem in methodRegList)
            {
                List<SeparateCodeIndex> list;
                //处理需要分离的代码（读取 新建文件）
                GetAndCreatNewFile(regItem.ForwardReg, regItem.BackReg, className, content, filePath, oldFileString, out list);
                indexList.AddRange(list);
            }

            if (indexList.Count > 0)
            {
                //更改原文件
                ChangeOriginalFile(indexList, titleContent, classIndex, content, filePath, originalFileName);
            }
        }

        /// <summary>
        /// 更改源文件
        /// </summary>
        /// <param name="indexList">读取列表</param>
        /// <param name="titleContent">文件头</param>
        /// <param name="classIndex">类名所在行</param>
        /// <param name="content">文件内容</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="originalFileName">原文件名</param>
        private static void ChangeOriginalFile(List<SeparateCodeIndex> indexList, StringBuilder titleContent, Int32 classIndex, String[] content, String filePath, String originalFileName)
        {
            String originalContent = titleContent.ToString();
            indexList = indexList.OrderBy(o => o.StartIndex).ToList();
            Int32 startIndex = classIndex;
            Boolean isAddSpace = true;

            foreach (var item in indexList)
            {
                for (int i = startIndex + 1; i < item.StartIndex; i++)
                {
                    if (content[i] != "")
                    {
                        originalContent += content[i] + Environment.NewLine;
                        isAddSpace = true;
                    }

                    if (content[i] == "" && isAddSpace)//去除多余的空行
                    {
                        originalContent += content[i] + Environment.NewLine;
                        isAddSpace = false;
                    }
                }
                startIndex = item.EndIndex;
            }

            //文件尾
            for (Int32 i = indexList.Select(s => s.EndIndex).Max() + 1; i < content.Count(); i++)
            {
                originalContent += content[i] + Environment.NewLine;
            }

            if (originalContent.Length > 0)
            {
                //组装partial类内容
                File.WriteAllText(filePath + "\\" + originalFileName, originalContent);
            }
        }

        /// <summary>
        /// 获取分离代码，并存到新文件
        /// </summary>
        /// <param name="methodReg">方法标识</param>
        /// <param name="methodRegBack">文件名后缀</param>
        /// <param name="className">类名</param>
        /// <param name="content">远文件内容</param>
        /// <param name="indexList">读取索引列表</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="oldFileString">原文件字符串</param>
        private static void GetAndCreatNewFile(String methodReg,String methodRegBack, String className, String[] content, String filePath, String oldFileString, out List<SeparateCodeIndex> indexList)
        {
            String methodContent = GetMethodContent(methodReg, content, out indexList);
            if (!String.IsNullOrEmpty(methodContent))
            {
                var newFileName = className + methodRegBack;

                //组装partial类内容
                String methodCContent = String.Format("{0}    {{\n{1}\n\r\t}}\n}}", TitleWithOutClassRemarks(content), methodContent);
                String path = filePath + "\\" + newFileName + ".cs";

                //创建并写入文件
                File.WriteAllText(path, methodCContent);

                String newFileString = path.Replace(_ConfigFilePath, String.Empty).TrimStart('\\');
                //添加到项目文件
                FileHelper.ManageProjectFile(newFileString, className + ".cs", oldFileString);
            }
        }

        /// <summary>
        /// 获取类名 文件头  以及class所在行
        /// </summary>
        /// <param name="content">全文内容</param>
        /// <param name="tiltleContent">文件头</param>
        /// <param name="classIndex">类名所在行</param>
        /// <returns></returns>
        private static String GetClassName(String[] content, out StringBuilder tiltleContent, out Int32 classIndex)
        {
            StringBuilder sb = new StringBuilder();
            String reg = " class ";
            String ret = String.Empty;
            classIndex = 0;
            tiltleContent = sb;

            for (int i = 0; i < content.Count(); i++)
            {
                if (content[i].Contains(reg))
                {
                    classIndex = i;
                    sb.AppendLine(content[i].Replace("class", "partial class"));
                    tiltleContent = sb;
                    return content[i].Substring(content[i].IndexOf(reg, StringComparison.Ordinal) + 6).Split(':')[0].Trim();
                }
                sb.AppendLine(content[i]);
            }
            return ret;
        }

        /// <summary>
        /// 获取方法内容
        /// </summary>
        /// <param name="reg">方法标识</param>
        /// <param name="content">文件内容</param>
        /// <param name="indexList">方法体位置集合</param>
        /// <returns>方法体</returns>
        private static String GetMethodContent(String reg, String[] content, out List<SeparateCodeIndex> indexList)
        {
            indexList = new List<SeparateCodeIndex>();
            StringBuilder methodContent = new StringBuilder();
            for (int i = 0; i < content.Count(); i++)
            {
                Int32 methodStartIndex = 0;
                Int32 methodEndIndex = 0;
                //定位方法名所在行
                if (content[i].Contains(reg))
                {
                    if (content[i].Contains(" return ")) continue;
                    if (!content[i].Contains(" public ") && !content[i].Contains(" private ") &&
                        !content[i].Contains(" internal ")) continue;
                    //先反向获取该方法开始Index（包含注释）
                    for (int j = i; j > 0; j--)
                    {
                        if (content[j].Contains("/// <summary>"))
                        {
                            methodStartIndex = j;

                            Int32 startIndex = 0;

                            //继续反向遍历    查找是否存在代码段注释
                            for (int k = j; k > j - 6; k--)
                            {
                                if (content[k].Contains("#region") || content[k].Contains("#if"))
                                {
                                    startIndex = k;
                                }
                            }
                            methodStartIndex = startIndex == 0 ? methodStartIndex : startIndex;
                            break;
                        }
                    }

                    //向下获取该方法结束Index
                    for (int j = i + 1; j < content.Count(); j++)
                    {
                        //下个方法开始（存在没有注释的方法........）
                        if (content[j].Contains("/// <summary>") || ((content[j].Contains(" public ") || content[j].Contains(" private ") || content[j].Contains(" internal ")) && content[j].Contains("(")))
                        {
                            //未到文件结尾
                            for (int k = j; k > j - 10; k--)
                            {
                                if (content[k].Contains("}"))
                                {
                                    methodEndIndex = k;

                                    Int32 endIndex = 0;

                                    //查找是否存在代码块结束标识   多个代码块结束标记时   暂定5行
                                    for (int l = k; l < k + 5; l++)
                                    {
                                        if (content[l].Contains("#endregion") || content[l].Contains("#endif"))
                                        {
                                            endIndex = l;
                                        }
                                    }
                                    methodEndIndex = endIndex == 0 ? methodEndIndex : endIndex;
                                    break;
                                }
                            }
                            break;
                        }

                        //已到文件结尾
                        if (j == content.Count() - 1)
                        {
                            Int32 num = 0;
                            //反向找到方法体结束index
                            for (Int32 k = content.Count() - 1; k > 0; k--)
                            {
                                if (content[k].Contains("}")) num += 1;
                                if (num == 2)
                                {
                                    methodEndIndex = k - 1;
                                    break;
                                }
                            }
                            break;
                        }
                    }

                    //获取需要分离的代码Content
                    for (int j = methodStartIndex; j <= methodEndIndex; j++)
                    {
                        methodContent.AppendLine(content[j]);
                    }

                    //记录分离的代码Index
                    indexList.Add(new SeparateCodeIndex()
                    {
                        StartIndex = methodStartIndex,
                        EndIndex = methodEndIndex
                    });
                }
            }
            return methodContent.ToString();
        }

        /// <summary>
        /// 快速扫描是否需要操作
        /// </summary>
        /// <param name="content">文件内容</param>
        /// <returns></returns>
        private static Boolean IsNeedHandle(String[] content)
        {
            for (int i = 0; i < content.Count(); i++)
            {
                if (content[i].Contains("partial class")) return false;
                if (content[i].Contains(" C_") || content[i].Contains(" M_")) return true;
            }
            return false;
        }

        /// <summary>
        /// 去除特定注释
        /// </summary>
        /// <param name="titleContent">文件头</param>
        /// <returns></returns>
        private static String TitleWithOutClassRemarks(String[] titleContent)
        {
            StringBuilder content = new StringBuilder();
            Int32 commentStart = 0;
            String classLine = String.Empty;

            for (int i = 0; i < titleContent.Count(); i++)
            {
                if (titleContent[i].Contains("/// </summary>")) commentStart = i + 1;

                if (titleContent[i].Contains("#region 接口说明")) commentStart = i;//个别文件注释不全............

                if (titleContent[i].Contains(" class "))
                {
                    classLine = titleContent[i].Replace(" class ", " partial class ");
                    break;
                }
            }
            for (int i = 0; i < commentStart; i++)
            {
                content.AppendLine(titleContent[i]);
            }
            content.AppendLine(classLine);
            return content.ToString();
        }
        #endregion
    }
}
