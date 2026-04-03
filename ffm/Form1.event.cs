using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            ProcessFileDto processFile = new ProcessFileDto();
            AssignmentProcessFile(path1, path2, processFile);
            
            if (radioButton3.Checked)
            {
                SmartCut(processFile);
            }
            else
            {
                ConvertVideo(ConvertCmd(processFile), path1);
            }
            sw.Stop();
            labelStatus.Text = $"耗时：{sw.ElapsedMilliseconds} ms";
            DeleteFileRecycleWithSetting(path1);
        }

        // ==================== 智能剪辑核心方法 ====================
        /// <summary>
        /// 智能剪辑：先快速剪辑关键帧区间，再精确分割
        /// </summary>
        private void SmartCut(ProcessFileDto processFile)
        {
            string input = processFile.processFile;
            string output = processFile.targetFile;
            double startSeconds = TimeSpan.Parse(processFile.beginTime).TotalSeconds;
            double endSeconds = TimeSpan.Parse(processFile.endTime).TotalSeconds;

            // 1. 获取四个关键帧时间
            var startPair = GetKeyframesAround(input, startSeconds);
            var endPair = GetKeyframesAround(input, endSeconds);

            double? kfBeforeStart = startPair.Item1;
            double? kfAfterStart = startPair.Item2;
            double? kfBeforeEnd = endPair.Item1;
            double? kfAfterEnd = endPair.Item2;

            Console.WriteLine($"{kfBeforeStart} {kfAfterStart}");
            Console.WriteLine($"{kfBeforeEnd} {kfAfterEnd}");

            if (!kfBeforeStart.HasValue || !kfAfterStart.HasValue || !kfBeforeEnd.HasValue || !kfAfterEnd.HasValue)
            {

                MessageBox.Show("失败", "警告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 回退到普通剪辑
                // ConvertVideo(ConvertCmd(processFile), output);
                return;
            }

            const double epsilon = 1e-6;

            // 2. 判断是否整个目标片段位于同一个关键帧区间内
            bool sameInterval = Math.Abs(kfBeforeStart.Value - kfBeforeEnd.Value) < epsilon &&
                                Math.Abs(kfAfterStart.Value - kfAfterEnd.Value) < epsilon;

            if (sameInterval)
            {
                // 简化流程：直接切割一个完整区间，然后精确提取
                string tempFull = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_full.mp4");
                string cmdFull = $"-ss {kfBeforeStart.Value.ToString("F9", CultureInfo.InvariantCulture)} -to {kfAfterEnd.Value.ToString("F9", CultureInfo.InvariantCulture)} -i \"{input}\" -c copy \"{tempFull}\"";
                ExecuteProcess(cmdFull, null);
                progressBar1.Value = 30;
                label8.Text = "30%";

                // 从完整区间中精确提取目标片段（重编码）
                double offset = startSeconds - kfBeforeStart.Value;
                double duration = endSeconds - startSeconds;
                string cmdExtract = BuildReencodeSegmentCommand(tempFull, offset, offset + duration, output, processFile);
                ExecuteProcess(cmdExtract, null);
                progressBar1.Value = 100;
                label8.Text = "100%";

                // 清理临时文件
                try { File.Delete(tempFull); } catch { }
                return;
            }


            // 3. 计算三个潜在片段的实际时长，仅当 >0 时才切割
            double part1Duration = kfAfterStart.Value - kfBeforeStart.Value;
            double part2Duration = kfBeforeEnd.Value - kfAfterStart.Value;
            double part3Duration = kfAfterEnd.Value - kfBeforeEnd.Value;

            string part1 = null, part2 = null, part3 = null;
            

            // 切割首段
            if (part1Duration > epsilon)
            {
                part1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_1.mp4");
                string cmd = $"-ss {kfBeforeStart.Value.ToString("F9", CultureInfo.InvariantCulture)} -to {kfAfterStart.Value.ToString("F9", CultureInfo.InvariantCulture)} -i \"{input}\" -c copy \"{part1}\"";
                ExecuteProcess(cmd, null);
                progressBar1.Value = 10;
                label8.Text = "10%";
            }

            // 切割中间段
            if (part2Duration > epsilon)
            {
                part2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_2.mp4");
                string cmd = $"-ss {kfAfterStart.Value.ToString("F9", CultureInfo.InvariantCulture)} -to {kfBeforeEnd.Value.ToString("F9", CultureInfo.InvariantCulture)} -i \"{input}\" -c copy \"{part2}\"";
                ExecuteProcess(cmd, null);
                progressBar1.Value = 40;
                label8.Text = "40%";
            }

            // 切割尾段
            if (part3Duration > epsilon)
            {
                part3 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_3.mp4");
                string cmd = $"-ss {kfBeforeEnd.Value.ToString("F9", CultureInfo.InvariantCulture)} -to {kfAfterEnd.Value.ToString("F9", CultureInfo.InvariantCulture)} -i \"{input}\" -c copy \"{part3}\"";
                ExecuteProcess(cmd, null);
                progressBar1.Value = 50;
                label8.Text = "50%";
            }


            // 4. 处理首段精确裁剪（如果需要）
            string finalPart1 = null;
            if (part1 != null)   // 首段存在
            {
                double startOffset = startSeconds - kfBeforeStart.Value;
                if (startOffset > epsilon && startOffset < part1Duration - epsilon)
                {
                    finalPart1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_1n.mp4");
                    string cmd = BuildReencodeSegmentCommand(part1, startOffset, part1Duration, finalPart1, processFile);
                    ExecuteProcess(cmd, null);
                }
                else
                {
                    finalPart1 = part1;   // 直接使用整段
                    part1 = null;         // 标记为已使用，避免重复删除
                }
            }
            progressBar1.Value = 60;
            label8.Text = "60%";
            // 5. 中间段直接使用（已经验证长度 >0）
            string finalPart2 = part2;
            part2 = null;

            // 6. 处理尾段精确裁剪（如果需要）
            string finalPart3 = null;
            if (part3 != null)   // 尾段存在
            {
                double endOffset = endSeconds - kfBeforeEnd.Value;
                if (endOffset > epsilon && endOffset < part3Duration - epsilon)
                {
                    finalPart3 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_3n.mp4");
                    string cmd = BuildReencodeSegmentCommand(part3, 0.0, endOffset, finalPart3, processFile);
                    ExecuteProcess(cmd, null);
                }
                else
                {
                    finalPart3 = part3;
                    part3 = null;
                }
            }
            progressBar1.Value = 70;
            label8.Text = "70%";
            // 7. 合并最终片段
            List<string> finalParts = new List<string>();
            if (!string.IsNullOrEmpty(finalPart1)) finalParts.Add(finalPart1);
            if (!string.IsNullOrEmpty(finalPart2)) finalParts.Add(finalPart2);
            if (!string.IsNullOrEmpty(finalPart3)) finalParts.Add(finalPart3);

            if (finalParts.Count == 1)
            {
                // 只有一个片段，直接移动或复制
                File.Move(finalParts[0], output);
            }
            else if (finalParts.Count > 1)
            {
                string concatList = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                using (StreamWriter sw = new StreamWriter(concatList))
                {
                    foreach (string f in finalParts)
                        sw.WriteLine($"file '{f.Replace("\\", "\\\\")}'");
                }
                string concatCmd = $"-f concat -safe 0 -i \"{concatList}\" -c copy \"{output}\"";
                ExecuteProcess(concatCmd, null);
                //ExecuteProcess(concatCmd, new DataReceivedEventHandler(OutputMergeVideo_smart));
                try { File.Delete(concatList); } catch { }
            }
            progressBar1.Value = 90;
            label8.Text = "90%";
            // 8. 清理临时文件
            //if (part1 != null && File.Exists(part1)) File.Delete(part1);
            //if (part2 != null && File.Exists(part2)) File.Delete(part2);
            //if (part3 != null && File.Exists(part3)) File.Delete(part3);
            //if (finalPart1 != null && finalPart1 != part1 && File.Exists(finalPart1)) File.Delete(finalPart1);
            //if (finalPart2 != null && finalPart2 != part1 && File.Exists(finalPart2)) File.Delete(finalPart2);
            //if (finalPart3 != null && finalPart3 != part3 && File.Exists(finalPart3)) File.Delete(finalPart3);

            // 清理临时文件（无论成功与否）
            
            try { File.Delete(part1); } catch { }
            try { File.Delete(part2); } catch { }
            try { File.Delete(part3); } catch { }
            try { File.Delete(finalPart1); } catch { }
            try { File.Delete(finalPart2); } catch { }
            try { File.Delete(finalPart3); } catch { }



            progressBar1.Value = 100;
            label8.Text = "100%";
        }


        /// <summary>
        /// 获取给定时间点附近的前后关键帧（原始时间）
        /// 返回：Tuple<之前最近的关键帧, 之后第一个关键帧>
        /// </summary>
        private Tuple<double?, double?> GetKeyframesAround(string inputFile, double targetSeconds)
        {
            string timeSec = targetSeconds.ToString("F9", CultureInfo.InvariantCulture);
            string arguments = $"-skip_frame nokey -select_streams v -show_entries frame=pts_time -of default=noprint_wrappers=1:nokey=1 -read_intervals \"{timeSec}%+30\" \"{inputFile}\"";
            string output = RunFFProbe(arguments);
            if (string.IsNullOrEmpty(output))
                return Tuple.Create<double?, double?>(null, null);

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            double? before = null;
            double? after = null;

            foreach (var line in lines)
            {
                if (double.TryParse(line.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                {
                    if (seconds <= targetSeconds)
                        before = seconds;
                    else if (seconds >= targetSeconds && !after.HasValue)
                        after = seconds;
                }
            }
            return Tuple.Create(before, after);
        }

        /// <summary>
        /// 从 ffmpeg 进度行中提取 time= 后面的时间字符串
        /// </summary>
        private string ExtractTimeFromLine(string line)
        {
            int start = line.IndexOf("time=") + 5;
            int end = line.IndexOf(" ", start);
            if (end == -1) end = line.Length;
            return line.Substring(start, end - start);
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
            string strArg = " -ss " + time + " -i \"" + textBox1.Text + "\""
                    + " -vframes 1 -q:v 2 \"" + path + ".jpg\" ";
            DeleteFile(path + ".jpg");
            if (checkBox4.Checked)
            {
                executeProcessDto = new ExecuteProcessDto();
                LinkedList<String> strArgList = new LinkedList<String>();
                strArgList.AddLast(strArg);
               // LinkedList<String> deleteFileList = new LinkedList<String>();
                executeProcessDto.strArgList = strArgList;      // List<string>
                //executeProcessDto.deleteFileList = deleteFileList; // List<string>
                executeProcessDto.outputConvertVideo = "OutputGetPicBase";
                executeProcessDto.state = state;
                executeProcessDto.path = path;

                listBox4.Items.Add(JsonConvert.SerializeObject(executeProcessDto));
                //listBox4.Items.Add(strArg);
            }
            else
            {
                ExecuteProcess(strArg, new DataReceivedEventHandler(OutputGetPicBase));
            }

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
            trackBar21.Value1 = 0;
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
                pictureBox12.Size = new Size(181 + 220, 285);
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
                pictureBox12.Size = new Size(181, 285);
                
            }
        }

        public void ConvertVideo(string strArg, string delPath, LinkedList<string> deleteFileList = null)
        {
            //Console.WriteLine("bbqq3 " + strArg);
            //textBox9.Text = textBox9.Text + generateLog(strArg);
            executeProcessDto = new ExecuteProcessDto();
            if (!"".Equals(textBox4.Text) && !"".Equals(textBox3.Text))
            {
                executeProcessDto.timeDifference = TimeSpan.Parse(textBox4.Text).Subtract(TimeSpan.Parse(textBox3.Text));
            }

            if (checkBox4.Checked)
            {
                LinkedList<String> strArgList = new LinkedList<String>();
                strArgList.AddLast(strArg);
                executeProcessDto.strArgList = strArgList;      // List<string>
                executeProcessDto.deleteFileList = deleteFileList; // List<string>
                executeProcessDto.outputConvertVideo = "OutputConvertVideo";
                executeProcessDto.state = state;
                executeProcessDto.path = delPath;

                listBox4.Items.Add(JsonConvert.SerializeObject(executeProcessDto));
                //listBox4.Items.Add(strArg);
            }
            else
            {
                Console.WriteLine("bbqq5 " + strArg);
                ExecuteProcess(strArg, new DataReceivedEventHandler(OutputConvertVideo));
            }

            progressBar1.Value = 100;
            label8.Text = "100%";
        }

        public void ConvertVideo(string strArg)
        {
            ConvertVideo(strArg, null, null);
        }

        public void ConvertVideo(string strArg, LinkedList<string> deleteFileList = null)
        {
            ConvertVideo(strArg, null, deleteFileList);
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
            TimeSpan timeDifference = TimeSpan.Parse(textBox4.Text).Subtract(TimeSpan.Parse(textBox3.Text));
            executeProcessDto = new ExecuteProcessDto();
            executeProcessDto.timeDifference = timeDifference;
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

            TimeSpan timeDifference = TimeSpan.Parse(textBox4.Text).Subtract(TimeSpan.Parse(textBox3.Text));
            executeProcessDto = new ExecuteProcessDto();
            executeProcessDto.timeDifference = timeDifference;
            string strArg = "-stream_loop -1 -i \"" + mp4tmp + "\" -i \"" + audio + "\" -map 0:v -map 1:a -c:v copy -c:a aac -t " + mp4tmpDuration + " \"" + textBox5.Text + "\"";
            ExecuteProcess(strArg, new DataReceivedEventHandler(OutputConvertVideo));

            DeleteFile(mp4tmp);
        }

        public void MergeSliceVideoOld()
        {
            if (listBox3.Items.Count % 2 != 0)
            {
                MessageBox.Show("截点不符规范", "警告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string strArg = "-i \"" + textBox1.Text + "\" -filter_complex \"";
            string strArg2 = "";
            for(int i = 1; i <= listBox3.Items.Count; i=i+2)
            {
                strArg += "[0:v]trim=start=" + ConvertToTotalSeconds(listBox3.Items[i - 1] as string) + ":end=" + ConvertToTotalSeconds(listBox3.Items[i] as string) + ",setpts=PTS-STARTPTS";

                if(checkBox6.Checked)
                {
                    strArg += ",scale=" + textBox8.Text.Replace(",", ":") + ":force_original_aspect_ratio=decrease,pad=" + textBox8.Text.Replace(",", ":") + ":(ow-iw)/2:(oh-ih)/2,setsar=1";
                }

                strArg += "[v" + (i/2 + 1) + "];" +
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

            if (checkBox11.Checked)
            {
                strArg += "-c:v h264_nvenc ";
            }

            strArg += "\"" + textBox2.Text + "\"";

            executeProcessDto = new ExecuteProcessDto();
            executeProcessDto.timeDifference = timeDifference2;
            if (checkBox4.Checked)
            {
                LinkedList<String> strArgList = new LinkedList<String>();
                strArgList.AddLast(strArg);
                executeProcessDto.strArgList = strArgList;      // List<string>
                executeProcessDto.outputConvertVideo = "OutputConvertVideo";
                executeProcessDto.state = state;
                executeProcessDto.path = textBox1.Text;
                listBox4.Items.Add(JsonConvert.SerializeObject(executeProcessDto));
                //listBox4.Items.Add(strArg);
            }
            else
            {
                ExecuteProcess(strArg, new DataReceivedEventHandler(OutputConvertVideo));
            }

            sw.Stop();
            labelStatus.Text = $"耗时：{sw.ElapsedMilliseconds} ms";
            progressBar1.Value = 100;
            label8.Text = "100%";
        }

        public void MergeSliceVideo()
        {
            // 1. 校验片段数量为偶数
            if (listBox3.Items.Count % 2 != 0)
            {
                MessageBox.Show("截点不符规范", "警告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            string inputFile = textBox1.Text;
            string outputFile = textBox2.Text;

            if (checkBox4.Checked) {
                MessageBox.Show($"剪辑失败：暂不支持", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 队列模式：如果勾选了“仅记录命令”，则不实际执行，只将每个片段的命令和最终合并命令加入队列
            /*if (checkBox4.Checked)
            {
                // 为每个片段生成命令并记录
                for (int i = 1; i <= listBox3.Items.Count; i += 2)
                {
                    string startTimeStr = listBox3.Items[i - 1] as string;
                    string endTimeStr = listBox3.Items[i] as string;

                    ProcessFileDto dto = new ProcessFileDto
                    {
                        processFile = inputFile,
                        targetFile = Path.GetTempFileName() + ".mp4", // 临时占位，实际队列中不执行
                        beginTime = startTimeStr,
                        endTime = endTimeStr,
                        vf = BuildFilterString()   // 根据checkBox6和textBox8构建滤镜
                    };

                    string strArg = ConvertCmd(dto);
                    // 模拟 ConvertVideo 的记录逻辑
                    executeProcessDto = new ExecuteProcessDto
                    {
                        strArg = strArg,
                        outputConvertVideo = "OutputConvertVideo",
                        state = state,
                        path = dto.targetFile
                    };
                    listBox4.Items.Add(JsonConvert.SerializeObject(executeProcessDto));
                }
                // 可选：记录合并命令（临时文件列表未知，可省略）
                MessageBox.Show("已加入队列，请执行队列", "提示");
                return;
            }*/

            // 2. 存储每个片段生成的临时文件
            List<string> tempFiles = new List<string>();
            TimeSpan totalDuration = TimeSpan.Zero;

            try
            {
                for (int i = 1; i <= listBox3.Items.Count; i += 2)
                {
                    string startTimeStr = listBox3.Items[i - 1] as string;
                    string endTimeStr = listBox3.Items[i] as string;
                    TimeSpan startTs = TimeSpan.Parse(startTimeStr);
                    TimeSpan endTs = TimeSpan.Parse(endTimeStr);
                    totalDuration += (endTs - startTs);

                    // 生成唯一临时文件
                    string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".mp4");

                    // 构造片段参数
                    ProcessFileDto dto = new ProcessFileDto
                    {
                        processFile = inputFile,
                        targetFile = tempFile,
                        beginTime = startTimeStr,
                        endTime = endTimeStr,
                        vf = BuildFilterString()   // 缩放/填充滤镜
                    };

                    // 根据 radioButton3 选择剪辑方式
                    if (radioButton3.Checked)
                    {
                        SmartCut(dto);
                    }
                    else
                    {
                        string strArg = ConvertCmd(dto);
                        ConvertVideo(strArg, tempFile);
                    }

                    // 校验是否生成成功
                    if (!File.Exists(tempFile))
                    {
                        throw new Exception($"片段 {startTimeStr} - {endTimeStr} 剪辑失败");
                    }
                    tempFiles.Add(tempFile);
                }

                // 3. 合并所有临时文件
                if (tempFiles.Count == 1)
                {
                    File.Move(tempFiles[0], outputFile);
                }
                else
                {
                    string concatList = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
                    using (StreamWriter swList = new StreamWriter(concatList))
                    {
                        foreach (string f in tempFiles)
                        {
                            string escapedPath = f.Replace("\\", "\\\\");
                            swList.WriteLine($"file '{escapedPath}'");
                        }
                    }
                    string concatCmd = $"-f concat -safe 0 -i \"{concatList}\" -c copy \"{outputFile}\"";
                    ExecuteProcess(concatCmd, new DataReceivedEventHandler(OutputConvertVideo));
                    File.Delete(concatList);
                }

                sw.Stop();
                labelStatus.Text = $"耗时：{sw.ElapsedMilliseconds} ms";
                progressBar1.Value = 100;
                label8.Text = "100%";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"剪辑失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 清理临时文件
                foreach (string f in tempFiles)
                {
                    try { File.Delete(f); } catch { }
                }
            }
        }

        /// <summary>
        /// 根据界面设置构建缩放/填充滤镜字符串
        /// </summary>
        private string BuildFilterString()
        {
            if (checkBox6.Checked && !string.IsNullOrEmpty(textBox8.Text))
            {
                string resolution = textBox8.Text.Replace(",", ":");
                return $"scale={resolution}:force_original_aspect_ratio=decrease,pad={resolution}:(ow-iw)/2:(oh-ih)/2,setsar=1";
            }
            return null;
        }

        /// <summary>
        /// 普通重编码剪辑（不使用智能关键帧对齐）
        /// </summary>
        private void NormalCut(ProcessFileDto dto, bool useNvenc)
        {
            string strArg = $"-ss {dto.beginTime} -to {dto.endTime} -i \"{dto.processFile}\" ";

            // 添加滤镜
            if (!string.IsNullOrEmpty(dto.vf))
            {
                strArg += $"-vf \"{dto.vf}\" ";
            }

            // 编码器选择
            if (useNvenc)
                strArg += "-c:v h264_nvenc ";
            else
                strArg += "-c:v libx264 ";

            // 音频直接复制（保持原有编码）
            strArg += "-c:a copy ";

            // CRF 质量参数
            if (!string.IsNullOrEmpty(dto.crf) && dto.crf != "-1")
            {
                strArg += $"-crf {dto.crf} ";
            }

            strArg += $"\"{dto.targetFile}\"";
            ExecuteProcess(strArg, null);
        }

        // 注意：SmartCut 方法已存在于您的代码中，无需重复定义。
        // 但需要确保 SmartCut 内部能正确处理 dto.vf 和 dto.crf 等参数。
        // 如果原 SmartCut 未支持滤镜，请按以下方式增强（可选）：
        // 在 BuildReencodeSegmentCommand 中加上 dto.vf 即可。
        private void OutputConvertVideo(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                if (output.Data.Contains("Duration: "))
                {
                    total = GetValueBetween(output.Data, "Duration: ", ",");
                    try
                    {
                        TimeSpan timeTotal = TimeSpan.Parse(total);
                        millisecondTotal = timeTotal.TotalMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("捕获到异常2：" + ex.Message);
                    }
                    
                }

                if (output.Data.Contains("time=") && output.Data.Contains("bitrate="))
                {
                    string value = GetValueBetween(output.Data, "time=", " ");
                    try
                    {
                        TimeSpan time = TimeSpan.Parse(value);
                        double milliseconds = time.TotalMilliseconds;
                        //TimeSpan timeDifference = TimeSpan.Parse(executeProcessDto.textBox4Text).Subtract(TimeSpan.Parse(executeProcessDto.textBox3Text));

                        progressBar1.Value = (int)(milliseconds * 100 / executeProcessDto.timeDifference.TotalMilliseconds);
                        label7.Text = "进度:" + value + " ~ " + executeProcessDto.timeDifference.ToString("hh\\:mm\\:ss\\.fff") + " / " + total + " ";
                        label8.Text = Math.Round(milliseconds * 100 / executeProcessDto.timeDifference.TotalMilliseconds, 2) + "%";
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
                    try
                    {
                        TimeSpan timeTotal = TimeSpan.Parse(total);
                        millisecondTotal = timeTotal.TotalMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("捕获到异常1：" + ex.Message);
                    }
                    
                   /* trackBar21.Maximum = millisecondTotal;
                    trackBar21.Value1 = 0;
                    trackBar21.Value2 = millisecondTotal;*/
                    //Value1Change();
                    //Value2Change();
                }

                //通过正则表达式获取信息里面的宽度信息
                Regex regex = new Regex("(\\d{2,5})x(\\d{2,5})", RegexOptions.Compiled);
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


        private void OutputMergeVideo_smart(object sendProcess, DataReceivedEventArgs output)
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

                        double progressBar1_Value = (milliseconds * 100 / timeDifference.TotalMilliseconds);
                        label7.Text = "进度:" + value + " ~ " + timeDifference.ToString("hh\\:mm\\:ss\\.fff") + " / " + total + " ";
                        double label8_Text_Value = Math.Round(milliseconds * 100 / timeDifference.TotalMilliseconds, 2);

                        progressBar1.Value = (int)(60 + progressBar1_Value/5);
                        label8.Text = 60 + label8_Text_Value/5 + "%"; ;
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
