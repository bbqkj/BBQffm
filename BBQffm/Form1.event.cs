using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ffm
{
    public partial class Form1 : Form
    {
        public void EditVideo(String path1, String path2)
        {
            EditVideo(path1, path2, false);
        }
        public void EditVideo(String path1, String path2, Boolean batch)
        {
            if (File.Exists(path2))
            {
                // 批量操作失败放入列表 不弹窗阻塞
                if (batch)
                {
                    listBox2.Items.Add(path1);
                }
                else
                {
                    MessageBox.Show("目标路径存在文件", "警告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    numericUpDown2.Value++;
                }
                return;
            }

            ProcessFileDto processFile = new ProcessFileDto();
            AssignmentProcessFile(path1, path2, processFile);
            ConvertVideo(ConvertCmd(processFile));
            DeleteFileRecycleWithSetting(path1);
        }

        
        public void GetPicAsync(string time)
        {
            // 设置最小线程数（预热）
            //ThreadPool.SetMinThreads(4, 4);

            // 设置最大线程数（默认值通常足够）
            //ThreadPool.SetMaxThreads(16, 16); // 不推荐随意修改

            //if(!spooler.IsPipeExecuting(0))
            //{
            spooler.Set(0, () => bbq(time));
            //}
            
            //ThreadPool.QueueUserWorkItem(bbq, time);
            //pictureBox12.Image = CaptureScreenshot(textBox1.Text, time);

        }
        public void bbq(object time) {
            pictureBox12.Image = CaptureScreenshot3(textBox1.Text, time as string);
        }

        public void bbq2(object time)
        {
            Console.WriteLine("bbq2");
            Console.WriteLine("bbss " + time);

            state = null;
            GetPicBase(time as string, i);
            if (File.Exists(Configuration.PIC_PATH + i + ".jpg"))
            {
                pictureBox12.Image = Image.FromStream(new MemoryStream(File.ReadAllBytes(Configuration.PIC_PATH + i + ".jpg")));
            }
            DeleteFile(Configuration.PIC_PATH + i + ".jpg");
            i = (i + 1) % 20;
        }

        public void GetPicBase(string time, int index)
        {
            GetPicBase(time, Configuration.PIC_PATH + index);
        }

        public void GetPicBase(string time, String path)
        {
            DeleteFile(path + ".jpg");
            string strArg = " -ss " + time + " -i \"" + textBox1.Text + "\""
                + " -vframes 1 -q:v 2 \"" + path + ".jpg\" ";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputGetPicBase));
        }

        public Image CaptureScreenshot2(string videoPath, string timestamp)
        {
            // 配置FFmpeg参数
            string arguments = $"-ss {timestamp:hh\\:mm\\:ss} -i \"{videoPath}\" " +
                               "-vframes 1 -f image2pipe -vcodec png -"; // 输出PNG到管道

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = System.AppDomain.CurrentDomain.BaseDirectory 
                    + "\\ffmpeg.exe", // 确保ffmpeg在系统路径或指定完整路径
                Arguments = arguments,
                RedirectStandardOutput = true, // 重定向标准输出
                RedirectStandardError = true,  // 可选：重定向错误输出
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                // 读取标准输出到内存流
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    //Console.WriteLine(arguments);
                    process.StandardOutput.BaseStream.CopyTo(memoryStream);
                    memoryStream.Position = 0; // 重置流位置

                    // 从内存流创建图像
                    return Image.FromStream(memoryStream);
                }
            }
        }

        public Image CaptureScreenshot3(string videoPath, string timestamp)
        {
            // 配置FFmpeg参数（使用PNG格式避免JPEG解码问题）
            string arguments = $"-ss {timestamp:hh\\:mm\\:ss} -i \"{videoPath}\" " +
                              "-vframes 1 -f image2pipe -vcodec png -";

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = System.AppDomain.CurrentDomain.BaseDirectory
                    + "\\ffmpeg.exe", // 确保ffmpeg在系统路径或指定完整路径
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,  // 必须重定向错误流
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // 启动进程
                process.Start();

                // 异步读取错误流（防止阻塞）
                StringBuilder errorOutput = new StringBuilder();
                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        errorOutput.AppendLine(e.Data);
                };
                process.BeginErrorReadLine();

                // 读取输出流到内存
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // 使用缓冲区读取提高性能
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        memoryStream.Write(buffer, 0, bytesRead);
                    }

                    // 等待进程退出（带超时机制）
                    if (!process.WaitForExit(5000)) // 5秒超时
                    {
                        process.Kill();
                        //throw new TimeoutException("FFmpeg process timed out");
                    }

                    // 检查退出代码
                    if (process.ExitCode != 0)
                    {
                        //throw new Exception($"FFmpeg failed with exit code {process.ExitCode}: {errorOutput}");
                    }

                    // 重置流位置并创建图像
                    memoryStream.Position = 0;
                    if(memoryStream == null)
                    {
                        return null;
                    }
                    try
                    {
                        return Image.FromStream(memoryStream);
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                    
                }
            }
        }

        public Image CaptureScreenshot(string videoPath, string timestamp)
        {
            if(timestamp.Equals("00:43:58.580"))
            {
                timestamp = "00:43:57.580";
            }
            
            // 使用更可靠的JPEG格式（PNG有时在.NET中有兼容性问题）
            string arguments = $"-ss {timestamp:hh\\:mm\\:ss} -i \"{videoPath}\" " +
                              "-vframes 1 -f image2pipe -vcodec mjpeg -q:v 2 -";
            Console.WriteLine("bbq " + arguments);

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = System.AppDomain.CurrentDomain.BaseDirectory
                    + "\\ffmpeg.exe", // 确保ffmpeg在系统路径或指定完整路径
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // 启动进程
                process.Start();

                // 异步读取错误流
                StringBuilder errorOutput = new StringBuilder();
                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        errorOutput.AppendLine(e.Data);
                };
                process.BeginErrorReadLine();

                // 读取输出流到内存
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // 直接复制流（更高效）
                    process.StandardOutput.BaseStream.CopyTo(memoryStream);

                    // 重要：在读取后等待进程结束
                    if (!process.WaitForExit(5000))
                    {
                        process.Kill();
                        throw new TimeoutException("FFmpeg process timed out");
                    }

                    // 检查退出代码
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"FFmpeg failed: {errorOutput}");
                    }

                    // 验证数据长度
                    if (memoryStream.Length == 0)
                        throw new Exception("No image data received from FFmpeg");

                    // 重置流位置
                    memoryStream.Position = 0;

                    // 使用Bitmap代替Image.FromStream（更可靠）
                    try
                    {
                        // 方案1：直接创建Bitmap
                        return new Bitmap(memoryStream);
                    }
                    catch (ArgumentException)
                    {
                        // 方案2：尝试修复无效图像数据
                        memoryStream.Position = 0;
                        return CreateImageFromStream(memoryStream);
                    }
                }
            }
        }

        // 处理损坏的图像数据
        private Bitmap CreateImageFromStream(MemoryStream ms)
        {
            // 将内存流保存为临时文件（仅用于调试）
            string debugPath = Path.Combine(Path.GetTempPath(), "ffmpeg_debug.jpg");
            File.WriteAllBytes(debugPath, ms.ToArray());

            // 尝试使用文件API加载（更健壮）
            using (var tempImg = Image.FromFile(debugPath))
            {
                return new Bitmap(tempImg);
            }

            // 生产环境应删除临时文件，这里简化处理
        }

        public void GetVideo(string path)
        {
            string strArg = "-i \"" + path + "\" ";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputGetVideo));
            AdjustWindow();
            trackBar21.Maximum = millisecondTotal;
            trackBar21.Value2 = millisecondTotal;
            trackBar21.Value1 = 0;
            
        }

        public String GetDuration(string path)
        {
            string strArg = "-i \"" + path + "\" ";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputGetDuration));
            return duration;
        }

        public void AdjustWindow()
        {
            if (widthFrame > heightFrame && checkBox1.Checked == true)
            {
                this.Size = new Size(570 + 220, 375);
                pictureBox12.Size = new Size(181 + 220, 296);
                /* this.Size = new Size(385, 588);
                 label6.Location = new Point(7, 334);
                 pictureBox12.Location = new Point(7, 348);
                 pictureBox12.Size = new Size(350, 200);*/
            }
            else
            {
                this.Size = new Size(570, 375);
                //label6.Location = new Point(361, 9);
                pictureBox12.Location = new Point(363, 25);
                pictureBox12.Size = new Size(181, 296);
                
            }
        }

        public void ConvertVideo(string strArg)
        {
            //textBox9.Text = textBox9.Text + generateLog(strArg);
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputConvertVideo));

            progressBar1.Value = 100;
            label8.Text = "100%";
        }

        //创建合并视频列表的文本
        private void CreateTextFileWithInputs()
        {
            string inputFileListPath = "filelist.txt";
            using (StreamWriter writer = new StreamWriter(inputFileListPath))
            {
                foreach (string inputVideoPath in listBox1.Items)
                {
                    if (!inputVideoPath.Contains(".mp3")) { 
                        writer.WriteLine("file '" + inputVideoPath + "'");
                    }
                }
            }
        }

        private String GetMp3()
        {
            foreach (string inputPath in listBox1.Items)
            {
                if (inputPath.Contains(".mp3"))
                {
                    return inputPath;
                }
            }
            return null;
        }

        public void MergeVideo(String outputPath)
        {
            CreateTextFileWithInputs();

            string strArg = "-f concat -safe 0 -i filelist.txt -c copy \"" + outputPath + "\" ";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputMergeVideo));

            progressBar1.Value = 100;
            label8.Text = "100%";
            DeleteFile("filelist.txt");
        }
         
        public void MergeVideo()
        {
            MergeVideo(textBox5.Text);
        }

        public void MergeAudioVideo()
        {
            String mp4tmp = coverPath(textBox5.Text, "_tmp");
            Console.WriteLine("bbss " + mp4tmp);

            MergeVideo(mp4tmp);

            String audio = GetMp3();
            String audioDuration = GetDuration(audio);

            string strArg = "-stream_loop -1 -i \"" + mp4tmp + "\" -i \"" + audio + "\" -map 0:v -map 1:a -c:v copy -c:a aac -t " + audioDuration + " \"" + textBox5.Text + "\"";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputConvertVideo));

            DeleteFile(mp4tmp);
        }
        public void MergeAudioVideo2()
        {
            String mp4tmp = coverPath(textBox5.Text, "_tmp");
            Console.WriteLine("bbss " + mp4tmp);

            MergeVideo(mp4tmp);

            String audio = GetMp3();
            String mp4tmpDuration = GetDuration(mp4tmp);

            string strArg = "-stream_loop -1 -i \"" + mp4tmp + "\" -i \"" + audio + "\" -map 0:v -map 1:a -c:v copy -c:a aac -t " + mp4tmpDuration + " \"" + textBox5.Text + "\"";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputConvertVideo));

            DeleteFile(mp4tmp);
        }

        public void MergeSliceVideo()
        {
            if (listBox3.Items.Count == 3)
            {
                MessageBox.Show("截点不符规范", "警告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string strArg = "-i \"" + textBox1.Text + "\" -filter_complex \"";
            string strArg2 = "";
            for(int i = 1; i <= listBox3.Items.Count; i=i+2)
            {
                strArg += "[0:v]trim=start=" + ConvertToTotalSeconds(listBox3.Items[i - 1] as string) + ":end=" + ConvertToTotalSeconds(listBox3.Items[i] as string) + ",setpts=PTS-STARTPTS[v" + (i/2 + 1) + "];" +
                    "[0:a]atrim=start=" + ConvertToTotalSeconds(listBox3.Items[i - 1] as string) + ":end=" + ConvertToTotalSeconds(listBox3.Items[i] as string) + ",asetpts=PTS-STARTPTS[a" + (i/2 + 1) + "];";
                strArg2 += "[v" + (i/2 + 1) + "]";
                strArg2 += "[a" + (i/2 + 1) + "]";
                TimeSpan timeDifferencetmp = TimeSpan.Parse(listBox3.Items[i] as string).Subtract(TimeSpan.Parse(listBox3.Items[i - 1] as string));
                if(i == 1)
                {
                    timeDifference2 = timeDifferencetmp;
                }
                else
                {
                    timeDifference2 = timeDifference2.Add(timeDifferencetmp);
                }
            }
            strArg += strArg2 + "concat=n="+ listBox3.Items.Count/2 + ":v=1:a=1[outv][outa]\"";
            strArg += " -map \"[outv]\" -map \"[outa]\" ";
            strArg += "\"" + textBox2.Text + "\"";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputConvertVideo2));

            progressBar1.Value = 100;
            label8.Text = "100%";
        }

        private void OutputConvertVideo(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("Duration: "))
                {
                    total = GetValueBetween(output.Data, "Duration: ", ",");
                    TimeSpan timeTotal = TimeSpan.Parse(total);
                    millisecondTotal = timeTotal.TotalMilliseconds;
                }

                if (output.Data.Contains("time=") && output.Data.Contains("bitrate="))
                {
                    string value = GetValueBetween(output.Data, "time=", " ");
                    try
                    {
                        TimeSpan time = TimeSpan.Parse(value);
                        double milliseconds = time.TotalMilliseconds;
                        TimeSpan timeDifference = TimeSpan.Parse(textBox4.Text).Subtract(TimeSpan.Parse(textBox3.Text));

                        progressBar1.Value = (int)(milliseconds * 100 / timeDifference.TotalMilliseconds);
                        label7.Text = "进度:" + value + " ~ " + timeDifference.ToString("hh\\:mm\\:ss\\.fff") + " / " + total + " ";
                        label8.Text = Math.Round(milliseconds * 100 / timeDifference.TotalMilliseconds, 2) + "%";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("捕获到异常：" + ex.Message);
                    }
                }
            }
        }

        private void OutputConvertVideo2(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("Duration: "))
                {
                    total = GetValueBetween(output.Data, "Duration: ", ",");
                    TimeSpan timeTotal = TimeSpan.Parse(total);
                    millisecondTotal = timeTotal.TotalMilliseconds;
                }

                if (output.Data.Contains("time=") && output.Data.Contains("bitrate="))
                {
                    string value = GetValueBetween(output.Data, "time=", " ");
                    try
                    {
                        TimeSpan time = TimeSpan.Parse(value);
                        double milliseconds = time.TotalMilliseconds;
                        //TimeSpan timeDifference = TimeSpan.Parse(textBox4.Text).Subtract(TimeSpan.Parse(textBox3.Text));

                        progressBar1.Value = (int)(milliseconds * 100 / timeDifference2.TotalMilliseconds);
                        label7.Text = "进度:" + value + " ~ " + timeDifference2.ToString("hh\\:mm\\:ss\\.fff") + " / " + total + " ";
                        label8.Text = Math.Round(milliseconds * 100 / timeDifference2.TotalMilliseconds, 2) + "%";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("捕获到异常：" + ex.Message);
                    }
                }
            }
        }

        private void OutputGetVideo(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("Duration: "))
                {
                    total = GetValueBetween(output.Data, "Duration: ", ",");
                    label7.Text = "进度:" + "0 ~ ?" + " / " + total + " ";
                    TimeSpan timeTotal = TimeSpan.Parse(total);
                    millisecondTotal = timeTotal.TotalMilliseconds;
                   /* trackBar21.Maximum = millisecondTotal;
                    trackBar21.Value1 = 0;
                    trackBar21.Value2 = millisecondTotal;*/
                    //Value1Change();
                    //Value2Change();
                }

                //通过正则表达式获取信息里面的宽度信息
                Regex regex = new Regex("(\\d{2,4})x(\\d{2,4})", RegexOptions.Compiled);
                Match m = regex.Match(output.Data);
                if (m.Success && !label6.Text.Contains("宽:"))
                {
                    widthFrame = int.Parse(m.Groups[1].Value);
                    heightFrame = int.Parse(m.Groups[2].Value);
                    label6.Text = label6.Text + " 宽:" + widthFrame + " 高:" + heightFrame;
                    widthFrameEnd = widthFrame;
                    heightFrameEnd = heightFrame;
                    textBox7.Text = widthFrame + "," + heightFrame;
                    textBox8.Text = widthFrame + "," + heightFrame;
                }
                else
                {
                }

            }
        }

        private void OutputGetDuration(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("Duration: "))
                {
                    duration = GetValueBetween(output.Data, "Duration: ", ",");
                }
            }
        }

        private void OutputMergeVideo(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("time=") && output.Data.Contains("bitrate="))
                {
                    string value = GetValueBetween(output.Data, "time=", " ");
                    try
                    {
                        TimeSpan time = TimeSpan.Parse(value);
                        double milliseconds = time.TotalMilliseconds;
                        TimeSpan timeDifference = TimeSpan.Parse(textBox4.Text).Subtract(TimeSpan.Parse(textBox3.Text));

                        progressBar1.Value = (int)(milliseconds * 100 / timeDifference.TotalMilliseconds);
                        label7.Text = "进度:" + value + " ~ " + timeDifference.ToString("hh\\:mm\\:ss\\.fff") + " / " + total + " ";
                        label8.Text = Math.Round(milliseconds * 100 / timeDifference.TotalMilliseconds, 2) + "%";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("捕获到异常：" + ex.Message);
                    }
                }
            }
        }

        private void OutputGetPicBase(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
            }
        }
    }
}
