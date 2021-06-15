namespace Milki.Extensions.MixPlayer.Utilities
{
    public static class SharedUtils
    {
        public static string CountSize(long size)
        {
            string strSize = "";
            long factSize = size;
            if (factSize < 1024)
                strSize = $"{factSize:F2} B";
            else if (factSize >= 1024 && factSize < 1048576)
                strSize = (factSize / 1024f).ToString("F2") + " KB";
            else if (factSize >= 1048576 && factSize < 1073741824)
                strSize = (factSize / 1024f / 1024f).ToString("F2") + " MB";
            else if (factSize >= 1073741824)
                strSize = (factSize / 1024f / 1024f / 1024f).ToString("F2") + " GB";
            return strSize;
        }
    }
}