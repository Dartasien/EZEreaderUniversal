namespace EZEreaderUniversal.DataModels
{
    public class ReadingFontsAndFamilies
    {
        private int _readingfontsize;
        public int ReadingFontSize
        {
            get
            {
                return _readingfontsize;
            }
            set
            {
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (value != _readingfontsize)
                {
                    _readingfontsize = value;
                }
            }
        }

        private string _readingfontfamily;
        public string ReadingFontFamily
        {
            get
            {
                return _readingfontfamily;
            }
            set
            {
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (value != _readingfontfamily)
                {
                    _readingfontfamily = value;
                }
            }
        }

        private string _readingfontcolorname;
        public string ReadingFontColorName
        {
            get
            {
                return _readingfontcolorname;
            }
            set
            {
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (value != _readingfontcolorname)
                {
                    _readingfontcolorname = value;
                }
            }
        }
        private string _backgroundreadingcolorname;
        public string BackgroundReadingColorName
        {
            get
            {
                return _backgroundreadingcolorname;
            }
            set
            {
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (value != _backgroundreadingcolorname)
                {
                    _backgroundreadingcolorname = value;
                }
            }
        }
    }
}
