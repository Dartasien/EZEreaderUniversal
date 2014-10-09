namespace EZEreaderUniversal
{
    class ChapterToC
    {
        public string ChapterName { get; set; }
        public string ChapterString { get; set; }

        public ChapterToC(string chapterName, string chapterString)
        {
            ChapterName = chapterName;
            ChapterString = chapterString;
        }
    }
}
