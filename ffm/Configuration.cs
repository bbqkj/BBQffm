using System;
using System.Timers;

namespace ffm
{
    class Configuration
    {
        // 日志路径
        public static String LOG_PATH_DEFAULT = "ffm-log.txt";
        public static String LOG_PATH = LOG_PATH_DEFAULT;

        // 图片缓存路径前缀
        public static String PIC_PATH_DEFAULT = "screenffm";
        public static String PIC_PATH = PIC_PATH_DEFAULT;

        public static String CUSTOM_EXTENSIONS = "webp、";
        public static String IMAGE_EXTENSIONS = "bmp、jpg、jpeg、png、webp、";
        public static String VIDEO_EXTENSIONS = "mp4、3gp、avi、flv、mov、rmvb、wmv、mpg、mpeg、rm、ram、swf、gif、";
    }
   
}
