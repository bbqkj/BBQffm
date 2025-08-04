using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.VisualBasic.FileIO;

namespace ffm
{
    public partial class Form1 : Form
    {
        public static string Post(string url, Dictionary<string, string> dic)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            #region 添加Post 参数
            StringBuilder builder = new StringBuilder();
            int i = 0;
            foreach (var item in dic)
            {
                if (i > 0)
                    builder.Append("&");
                builder.AppendFormat("{0}={1}", item.Key, item.Value);
                i++;
            }
            byte[] data = Encoding.UTF8.GetBytes(builder.ToString());
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }
            #endregion
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            //获取响应内容
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }


        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="filesize">文件传入大小</param>
        /// <returns></returns>
        private static string GetFileSize(long filesize)
        {
            try
            {
                if (filesize < 0)
                {
                    return "0";
                }
                else if (filesize >= 1024 * 1024 * 1024)  //文件大小大于或等于1024MB    
                {
                    return string.Format("{0:0.00} GB", (double)filesize / (1024 * 1024 * 1024));
                }
                else if (filesize >= 1024 * 1024) //文件大小大于或等于1024KB    
                {
                    return string.Format("{0:0.00} MB", (double)filesize / (1024 * 1024));
                }
                else if (filesize >= 1024) //文件大小大于等于1024bytes    
                {
                    return string.Format("{0:0.00} KB", (double)filesize / 1024);
                }
                else
                {
                    return string.Format("{0:0.00} bytes", filesize);
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public static string GetValueBetween(string input, string startSymbol, string endSymbol)
        {
            string pattern = $"{Regex.Escape(startSymbol)}(.*?){Regex.Escape(endSymbol)}";
            Match match = Regex.Match(input, pattern);
            return match.Groups.Count > 1 ? match.Groups[1].Value : "";
        }

        public static string ReplaceLast(string input, string oldValue, string newValue)
        {
            int index = input.LastIndexOf(oldValue);
            if (index < 0)
            {
                return input;
            }
            else
            {
                StringBuilder sb = new StringBuilder(input.Length - oldValue.Length + newValue.Length);
                sb.Append(input.Substring(0, index));
                sb.Append(newValue);
                sb.Append(input.Substring(index + oldValue.Length, input.Length - index - oldValue.Length));
                return sb.ToString();
            }
        }

        public String coverPath(String oldPath, String addPath)
        {
            string[] paths = oldPath.Split('.');
            string path2 = paths[paths.Length - 2];
            //path2 = oldPath.Replace(path2, path2 + addPath);
            path2 = ReplaceLast(oldPath, path2 + ".", path2 + addPath + ".");
            return path2;
        }

        public void ExecuteProcess(string strArg, DataReceivedEventHandler e)
        {
            if(state != null)
            {
                textBox9.Text = textBox9.Text + generateLog(strArg);
            }
            Console.WriteLine(strArg);
            
            Process p = new Process();//建立外部调用线程
            string ffmpegPath = System.AppDomain.CurrentDomain.BaseDirectory
               + "\\ffmpeg.exe";
            Console.WriteLine(ffmpegPath);
            p.StartInfo.FileName = ffmpegPath;//要调用外部程序的绝对路径
            p.StartInfo.Arguments = strArg;
            p.StartInfo.UseShellExecute = false;//不使用操作系统外壳程序启动线程(一定为FALSE,详细的请看MSDN)
            p.StartInfo.RedirectStandardError = true;//把外部程序错误输出写到StandardError流中(这个一定要注意,FFMPEG的所有输出信息,都为错误输出流,用StandardOutput是捕获不到任何消息的...这是我耗费了2个多月得出来的经验...mencoder就是用standardOutput来捕获的)
            p.StartInfo.CreateNoWindow = true;//不创建进程窗口
            p.ErrorDataReceived += e;//外部程序(这里是FFMPEG)输出流时候产生的事件,这里是把流的处理过程转移到下面的方法中,详细请查阅MSDN
            p.Start();//启动线程
            p.BeginErrorReadLine();//开始异步读取
            p.WaitForExit();//阻塞等待进程结束
            p.Close();//关闭进程
            p.Dispose();//释放资源
        }

        public void DeleteFile(String path)
        {
            if(File.Exists(path))
            {
                File.Delete(path);
            }       
        }

        void DeleteFileRecycleWithSetting(String path)
        {
            if (File.Exists(path) && checkBox3.Checked)
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
        }

        void DeleteFileRecycle(String path)
        {
            if (File.Exists(path))
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
        }

        static void SetSettings(String Setting, object value)
        {
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey("ffmSettings");
            regKey.SetValue(Setting, value);
        }

        static object GetSettings(String Setting)
        {
            // 读取注册表
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("ffmSettings");
            if (regKey != null)
            {
                object value = regKey.GetValue(Setting);
                // 使用获取的值...
                return value;
            }

            return null;
        }

        static void LoadSettings()
        {
            // 读取注册表
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("ffmSettings");
            if (regKey != null)
            {
                object value1 = regKey.GetValue("Setting1");
                object value2 = regKey.GetValue("Setting2");
                // 使用获取的值...
            }
        }

        static void DeleteSetting(String Setting)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("ffmSettings",true);
            if (regKey != null)
            {
                if(regKey.GetValue(Setting) != null)
                {
                    regKey.DeleteValue(Setting);
                }
            }
        }

        static void DeleteSettings()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("ffmSettings");
            if (regKey != null)
            {
                Registry.CurrentUser.DeleteSubKey("ffmSettings");
            }
        }

        String generateLog(String arg)
        {
            DateTime now = DateTime.Now; // 获取当前时间
            //Console.WriteLine(now.ToString());
            String log = now.ToString() + " 【" + state + "】 \r\n" + arg + " \r\n";
            if(checkBox2.Checked)
            {
                CreateLog(log);
            }
            return log;
        }

        static void CreateLog(String arg)
        {
            string filePath = Configuration.LOG_PATH;
            using (FileStream fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.WriteLine(arg);
                }
            }
        }

        static String GetLog()
        {
            String str = "";
            string filePath = Configuration.LOG_PATH;
            if (File.Exists(filePath))
            {
                using (StreamReader sr = new StreamReader(filePath))
                {

                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        str += line + "\r\n";
                    }
                }
            }
            return str;
        }

        public static string ConvertToTotalSeconds(string timeString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(timeString))
                    return null;

                // 支持多种分隔符（:/.）
                char[] separators = { ':', '.' };
                var parts = timeString.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                // 验证格式（至少需要时:分:秒）
                if (parts.Length < 3 || parts.Length > 4)
                    return null;

                // 使用TryParse更安全
                if (!int.TryParse(parts[0], out int hours) ||
                    !int.TryParse(parts[1], out int minutes) ||
                    !int.TryParse(parts[2], out int seconds))
                    return null;

                // 处理毫秒部分
                int milliseconds = 0;
                if (parts.Length == 4 && !int.TryParse(parts[3], out milliseconds))
                    return null;

                // 验证数值范围
                if (minutes >= 60 || seconds >= 60 || milliseconds >= 1000)
                    return null;

                return (hours * 3600 + minutes * 60 + seconds + milliseconds / 1000.0) + "";
            }
            catch
            {
                return null;
            }
        }
    }
}
