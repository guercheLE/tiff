using System.Collections.Generic;

namespace guercheLE.Drawing.Imaging.ImageFormat.Tiff
{
    public class ImageFile
    {
        uint _offset;
        public uint Offset
        {
            get { return _offset; }
            internal set { _offset = value; }
        }

        ushort _tagsCount;

        public ushort TagsCount
        {
            get { return _tagsCount; }
            internal set { _tagsCount = value; }
        }

        uint _offsetOfNextImageFile;
        public uint OffsetOfNextImageFile
        {
            get { return _offsetOfNextImageFile; }
            internal set { _offsetOfNextImageFile = value; }
        }

        Dictionary<TagCode, ImageFileTag> _tags = new Dictionary<TagCode, ImageFileTag>();
        public Dictionary<TagCode, ImageFileTag> Tags
        {
            get { return _tags; }
            set { _tags = value; }
        }

        List<ImageFileStrip> _strips = new List<ImageFileStrip>();
        public List<ImageFileStrip> Strips
        {
            get { return _strips; }
            internal set { _strips = value; }
        }

        List<ImageFileTile> _tiles = new List<ImageFileTile>();
        public List<ImageFileTile> Tiles
        {
            get { return _tiles; }
            internal set { _tiles = value; }
        }
    }
}
