using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using Newtonsoft.Json;

namespace ffm
{
    public partial class Form1 : Form
    {
        string total;
        double millisecondTotal;

        // 帧坐标 即视频宽高
        int widthFrame = 0;
        int heightFrame = 0;

        // 起点帧坐标
        int widthFrameStart = 0;
        int heightFrameStart = 0;

        // 终点帧坐标
        int widthFrameEnd = 0;
        int heightFrameEnd = 0;

        // 剪辑后的宽高
        int cutWidthFrame = 0;
        int cutHeightFrame = 0;

        int i = 0;

        public static String state; 
        public static Boolean flag; 

        private bool dragging = false;
        private int dragIndex = -1;
        private Point dragCursorPoint;
        private Point dragStartPoint;

        public Form1()
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();

            AssignmentComboBox1();

            IniProgressBar();

            IniCache();

            textBox9.Text = GetLog();

            pictureBox12.MouseWheel += new MouseEventHandler(pictureBox1_MouseWheel);
        }

        /*private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox12.MouseWheel += new MouseEventHandler(pictureBox1_MouseWheel);
        }*/

        private void button1_Click(object sender, EventArgs e)
        {
            state = "剪辑";
            EditVideo(textBox1.Text, textBox2.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string url = "http://localhost:8080/bbq/cmd/api/cmd";
            Dictionary<string, string> dic = new Dictionary<string, string>();
            AssignmentDictionary(dic);
            string res = Post(url, dic);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string url = "http://localhost:8080/bbq/cmd/api/cmdd";
            Dictionary<string, string> dic = new Dictionary<string, string>();
            AssignmentDictionary(dic);
            string res = Post(url, dic);
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            String addPath = "_";
            if (numericUpDown2.Value != -1)
            {
                addPath += numericUpDown2.Value;
            }
            textBox2.Text = coverPath(textBox1.Text, addPath);
        }

        private void trackBar21_Value1Changed(object sender, EventArgs e)
        {
            Value1Change();
        }

        private void trackBar21_Value2Changed(object sender, EventArgs e)
        {
            Value2Change();
        }

        private void Value1Change()
        {
            DateTime dateTime = new DateTime(1900, 1, 1, 0, 0, 0)
                .AddMilliseconds(trackBar21.Value1);
            String time = dateTime.ToString("HH:mm:ss.fff");
            textBox3.Text = time;
            dateTimePicker1.Value = dateTime;
            GetPic(time);
        }

        private void Value2Change()
        {
            DateTime dateTime = new DateTime(1900, 1, 1, 0, 0, 0)
                .AddMilliseconds(trackBar21.Value2);
            String time = dateTime.ToString("HH:mm:ss.fff");
            textBox4.Text = time;
            dateTimePicker2.Value = dateTime;

            GetPic(time);
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            textBox3.Text = dateTimePicker1.Value.ToString("HH:mm:ss.fff");
            //trackBar21.Value1 = dateTimePicker1.Value.Millisecond;
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            textBox4.Text = dateTimePicker2.Value.ToString("HH:mm:ss.fff");
            //trackBar21.Value2 = dateTimePicker2.Value.Millisecond;
        }

        private void tabPage1_DragDrop(object sender, DragEventArgs e)
        {
            Reset();
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();       //获得路径
            String path2 = coverPath(path, "_");
            textBox1.Text = path;
            textBox2.Text = path2;

            ShowFileSize(path);
            GetVideo(path);
        }

        private void tabPage1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void tabPage2_DragEnter(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData(typeof(string));
            if (data != null)
            {
                listBox1.Items.Remove(data);
            }
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            //获取拖放的数据内容
            object data = e.Data.GetData(typeof(string));
            if (data != null)
            {
                Point point = listBox1.PointToClient(new Point(e.X, e.Y));
                int index = this.listBox1.IndexFromPoint(point);
                if (index < 0)
                {
                    index = this.listBox1.Items.Count - 1;
                }
                //删除元数据
                this.listBox1.Items.Remove(data);
                //插入目标数据
                this.listBox1.Items.Insert(index, data);
            }
            else
            {
                System.Array files = (System.Array)e.Data.GetData(DataFormats.FileDrop);
                string path = files.GetValue(0).ToString();       //获得路径                
                if (listBox1.Items.Count == 0)
                {
                    String path2 = coverPath(path, "_");
                    textBox5.Text = path2;
                }
                listBox1.Items.Add(path);
                for (int k = 1; k < files.Length; k++)
                {
                    listBox1.Items.Add(files.GetValue(k).ToString());
                }
                ShowFileSize(path);
            }
        }
        
        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.listBox1.SelectedItem == null)
            {
                return;
            }

            String path = this.listBox1.SelectedItem.ToString();
            ShowFileSize(path);

            //开始拖放操作，DragDropEffects为枚举类型。
            //DragDropEffects.Move 为将源数据移动到目标数据
            this.listBox1.DoDragDrop(this.listBox1.SelectedItem, DragDropEffects.Move);
        }

        private void listBox1_MouseMove(object sender, MouseEventArgs e)
        {
            /*if (dragging)
            {
                Cursor.Current = Cursors.Hand;
                listBox1.DoDragDrop(listBox1.Items[dragIndex], DragDropEffects.Move);
            }*/
        }

        private void listBox1_MouseUp(object sender, MouseEventArgs e)
        {
           /* if (dragging)
            {
                dragging = false;
                Cursor.Current = Cursors.Default;
            }*/
        }

        private void listBox1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            state = "合并视频";
            MergeVideo();
        }

        private void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (this.listBox1.SelectedItem == null)
            {
                return;
            }
            ShowFileSize(this.listBox1.SelectedItem.ToString());
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            Point coordinates = me.Location;
            //MessageBox.Show("鼠标点击位置为：" + coordinates.ToString());
            try
            {
                if (textBox6.Focused == true)
                {
                    CalculationArea(coordinates.X, coordinates.Y, true);
                   
                }
                else if (textBox7.Focused == true)
                {
                    CalculationArea(coordinates.X, coordinates.Y, false);
                }
            }
            catch(Exception ex)
            {
                //throw ex;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ("System.Collections.DictionaryEntry".Equals(comboBox1.SelectedValue.ToString()) || "-1".Equals(comboBox1.SelectedValue.ToString()) || "6".Equals(comboBox1.SelectedValue.ToString()))
            {
                textBox6.Enabled = true;
                textBox7.Enabled = true;
            } 
            else
            {
                textBox6.Enabled = false;
                textBox7.Enabled = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            state = "加马赛克";
            if (ExistsFile())
            {
                return;
            }
            string strArg = "-i \"";
            strArg += textBox1.Text + "\" -filter_complex \"delogo=x=" +
                widthFrameStart + ":y=" + heightFrameStart + ":w=" + cutWidthFrame + ":h=" + cutHeightFrame +
                  "\" -c:v libx264 -preset veryfast -c:a copy -movflags +faststart \"";
            strArg += textBox2.Text + "\" "; //-y
            ConvertVideo(strArg);
            DeleteFileRecycleWithSetting(textBox1.Text);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            state = "清除封面";
            if (ExistsFile())
            {
                return;
            }
            string strArg = "-i \"";
            strArg += textBox1.Text + "\" -c copy \"";
            strArg += textBox2.Text + "\" ";//-y 
            ConvertVideo(strArg);
            DeleteFileRecycleWithSetting(textBox1.Text);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            state = "设置封面-截图";
            if (ExistsFile())
            {
                return;
            }
            GetPicBase(textBox3.Text, -1);

            string strArg = "-i \"";
            strArg += textBox1.Text + "\" -i " + Configuration.PIC_PATH + "-1.jpg -map 1 -map 0 -c copy -disposition:0 attached_pic \"";
            strArg += textBox2.Text + "\" ";//-y 
            state = "设置封面-合成";
            ConvertVideo(strArg);
            DeleteFile(Configuration.PIC_PATH + "-1.jpg");
            DeleteFileRecycleWithSetting(textBox1.Text);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            state = "截图";
            GetPicBase(textBox3.Text, textBox1.Text + Guid.NewGuid().ToString("N"));
            progressBar1.Value = 100;
            label8.Text = "100%";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            state = "批量压缩";
            listBox2.Items.Clear();
            foreach (string inputVideoPath in listBox1.Items)
            {
                EditVideo(inputVideoPath, coverPath(inputVideoPath, "_"), true);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            textBox5.Clear();
            listBox1.Items.Clear();
            listBox2.Items.Clear();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            SetSettings("CHECKBOX1", checkBox1.Checked);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            DeleteSettings();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            String value = "";

            ArrayList comboBoxList = LoadCombobox2();

            if (comboBoxList.Contains(comboBox2.Text))
            {
                comboBoxList.Remove(comboBox2.Text);
            }
            else
            {
                value = comboBox2.Text;
                comboBoxList.Add(comboBox2.Text);
            }
            SetSettings("COMBOBOX2", JsonConvert.SerializeObject(comboBoxList));
            comboBox2.DataSource = comboBoxList;
            comboBox2.Text = value;
        }

        private void comboBox2_TextChanged(object sender, EventArgs e)
        {
            if (comboBox2.Text == null || "".Equals(comboBox2.Text.Trim()))
            {
                button13.Enabled = false;
            }
            else
            {
                button13.Enabled = true;
            }
            ArrayList comboBoxList = (ArrayList)comboBox2.DataSource;
            if (comboBoxList != null && comboBoxList.Contains(comboBox2.Text))
            {
                button13.Text = "删除";
            }
            else
            {
                button13.Text = "缓存";
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            state = "自定义命令";
            ConvertVideo(comboBox2.Text);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            SetSettings("CHECKBOX2", checkBox2.Checked);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            SetSettings("CHECKBOX3", checkBox3.Checked);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            textBox9.Text = "";
        }

        private void button15_Click(object sender, EventArgs e)
        {
            DeleteFileRecycle(Configuration.LOG_PATH);
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            String value = textBox10.Text.Trim();
            if (Configuration.LOG_PATH_DEFAULT.Equals(value) || "".Equals(value))
            {
                DeleteSetting("TEXTBOX10");
            } 
            else
            {
                SetSettings("TEXTBOX10", value);
            }
            Configuration.LOG_PATH = value;
        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            String value = textBox11.Text.Trim();
            if (Configuration.PIC_PATH_DEFAULT.Equals(value) || "".Equals(value))
            {
                DeleteSetting("TEXTBOX11");
            }
            else
            {
                SetSettings("TEXTBOX11", value);
            }
            Configuration.PIC_PATH = value;
        }
    }
}
