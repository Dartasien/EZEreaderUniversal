using System.Collections.Generic;
using System.ComponentModel;


namespace EZEreaderUniversal.DataModels
{
    public class ChapterModel : INotifyPropertyChanged
    {
        //event handler to notify WP that something has changed
        //and needs to be refreshed on the screen
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string p)
        {
            var handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(p));
            }

        }

        //Declare an id for each book
        private string _bookid;

        public string BookID
        {
            get
            {
                return _bookid;
            }
            set
            {
                if (value != _bookid)
                {
                    _bookid = value;
                    NotifyPropertyChanged("BookID");
                }
            }
        }

        //declare a string to identify the page/chapter in the book
        private int _chapterid;

        public int ChapterID
        {
            get
            {
                return _chapterid;
            }
            set
            {
                if (value != _chapterid)
                {
                    _chapterid = value;
                    NotifyPropertyChanged("ChapterID");
                }
            }
        }

        private string _chaptername;

        public string ChapterName
        {
            get
            {
                return _chaptername;
            }
            set
            {
                if (value != _chaptername)
                {
                    _chaptername = value;
                    NotifyPropertyChanged("ChapterName");
                }
            }
        }

        //declare an int to hold the total pages in the chapter
        private int _pageCount;

        public int PageCount
        {
            get
            {
                return _pageCount;
            }
            set
            {
                if (value != _pageCount)
                {
                    _pageCount = value;
                    NotifyPropertyChanged("PageCount");
                }
            }
        }

        //declare a string to identify the page/chapter in the book
        private string _chapterstring;

        public string ChapterString
        {
            get
            {
                return _chapterstring;
            }
            set
            {
                if (value != _chapterstring)
                {
                    _chapterstring = value;
                    NotifyPropertyChanged("ChapterString");
                }
            }
        }

        // declare a string to hold the name of the page/chapter that goes to the id
        private List<PageModel> _chapterpages;

        public List<PageModel> ChapterPages
        {
            get
            {
                return _chapterpages;
            }
            set
            {
                if (value != _chapterpages)
                {
                    _chapterpages = value;
                    NotifyPropertyChanged("ChapterPages");
                }
            }
        }
    }
}
