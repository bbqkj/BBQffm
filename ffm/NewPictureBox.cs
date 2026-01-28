using System;
using System.Drawing;
using System.Windows.Forms;

namespace ffm
{
    public partial class NewPictureBox : PictureBox
    {
        public int newWidth { get; set; }
        public int newHeight { get; set; }

        public NewPictureBox()
        {
            this.SizeMode = PictureBoxSizeMode.Zoom;
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            //base.OnPaint(pe);

            if (Image == null)
                return;

            // 获取PictureBox的客户区大小
            Rectangle rect = this.ClientRectangle;

            // 计算缩放比例
            float ratioX = (float)rect.Width / Image.Width;
            float ratioY = (float)rect.Height / Image.Height;
            float ratio = Math.Min(ratioX, ratioY);

            // 计算缩放后的图像大小
            newWidth = (int)(Image.Width * ratio);
            newHeight = (int)(Image.Height * ratio);

            //this.Size = new Size(newWidth, newHeight);

            // 创建绘制图像的矩形，使其靠左对齐
            Rectangle imageRect = new Rectangle(0, 0, newWidth, newHeight);

            // 绘制图像
            pe.Graphics.DrawImage(Image, imageRect);
        }
    }
}
