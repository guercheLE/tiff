namespace guercheLE.Drawing.Imaging.ImageFormat.Tiff
{
    public class ImageFileStrip
    {
        uint _binaryContentOffset;
        public uint BinaryContentOffset
        {
            get { return _binaryContentOffset; }
            internal set { _binaryContentOffset = value; }
        }

        byte[] _binaryContent;
        public byte[] BinaryContent
        {
            get { return _binaryContent; }
            internal set { _binaryContent = value; }
        }
    }
}
