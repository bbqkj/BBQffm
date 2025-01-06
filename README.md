# BBQffm
c#编写 基于ffmpeg的视频裁剪工具
下载ffmpeg.exe放到同目录即可

@[TOC](c#编写基于ffmpeg的视频裁剪)
## 前言
c#编写的一个基于ffmpeg的视频裁剪小工具，比较粗糙，但是适配自己的需求去编写自己习惯的小工具用的比较舒服。比如现在的裁剪工具感觉就苹果自带的拖动预览比较丝滑，裁剪又不重新乱编码，其它的不是这不合理，就是那不好用，用ffmpeg指令是最干净的，就是缺少可视化操作，效率太差。
## 展示
### ① 压缩裁剪
![请添加图片描述](https://i-blog.csdnimg.cn/direct/479252d6187a48f2bb7c740d55401882.gif)
![请添加图片描述](https://i-blog.csdnimg.cn/direct/7b8fd09bf1b649b1b12b130b6e821456.png)
### ② 批量处理
![在这里插入图片描述](https://i-blog.csdnimg.cn/direct/a683f903a0fc41238ca305e8daf08ecf.png)
### ③ 自定义命令
![在这里插入图片描述](https://i-blog.csdnimg.cn/direct/c040965e63bb440aa0424d4bf2d647d7.png)
### ④ 配置管理
![在这里插入图片描述](https://i-blog.csdnimg.cn/direct/6e079efa6b684b599233eb31ee7bee42.png)
### ⑤ 执行日志
![在这里插入图片描述](https://i-blog.csdnimg.cn/direct/6efbddc5f0a344af94392b77af9bd0ca.png)
## 功能实现思路
### ① 帧预览
游标拖动事件触发，图片框展示该时间戳ffmpeg截图
### ② 框选区域
picturebox用Zoom显示模式，让图片自适应，然后根据像素和帧宽高比例实现鼠标点击的图像框坐标和帧坐标的换算。
### ③ picturebox，Zoom模式，让图片显示靠边显示
[C#中picturebox，Zoom显示模式下，如何让图片显示靠右边显示。](https://ask.csdn.net/questions/239446)
16年的提问没有答案，用ai找到了答案，不得不感慨ai确实开始有些惊喜，一个不存在答案的问题，它会拼凑成有答案的元问题获取答案再组装起来，结果还真实现了。
思路就是重写PictureBox的绘制方法
```csharp
// 创建绘制图像的矩形，使其靠左对齐
Rectangle imageRect = new Rectangle(0, 0, newWidth, newHeight);
```
### ④ 时间区间选择进度条
[C# winform 双头滑块 TrackBar2](https://blog.csdn.net/hhf15980873586/article/details/127128824)
采用这哥们编写的自定义双头滑块控件，做些修改适配自己的需求。
### ⑤ 配置和缓存
注册表在win上起到一个简单数据库的功能，配置和缓存用注册表存储。

## 最后
适配自己习惯的才是最好用的，不如尝试自己编写小工具。
