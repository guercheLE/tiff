using System;

namespace guercheLE.Drawing.Imaging.ImageFormat.Tiff
{
    public static class ByteArray
    {
        public static byte[] Reverse(byte[] byteArray)
        {
            Array.Reverse(byteArray);
            return byteArray;
        }
    }
}
