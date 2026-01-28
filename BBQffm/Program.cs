using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ffm
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) 
        { 
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name + ".Lib." + new AssemblyName(args.Name).Name + ".dll"; 
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyName)) 
            { 
                byte[] buffer = new byte[stream.Length]; 
                stream.Read(buffer, 0, buffer.Length); 
                return Assembly.Load(buffer); 
            } 
        }
    }

    public class ProcessFileDto
    {
        public String processFile // 处理文件
        { get; set; }
        public String targetFile  // 目标文件
        { get; set; }
        public String vf          // 旋转
        { get; set; }
        public String crf         // 固定码率系数
        { get; set; }
        public String beginTime   // 开始
        { get; set; }
        public String endTime     // 结束
        { get; set; }
        public String concatFile  //
        { get; set; }

    }
    public class ExecuteProcessDto
    {
        public String strArg // 执行命令
        { get; set; }
        public String path // 
        { get; set; }
        public String outputConvertVideo  // 输出方法
        { get; set; }
        public TimeSpan timeDifference  // timeDifference
        { get; set; }
        public String state  // state
        { get; set; }

    }
}
