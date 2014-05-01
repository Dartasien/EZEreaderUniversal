using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZEreaderUniversal.ViewModels
{
    public class BookModel : INotifyPropertyChanged
    {
        //event handler to notify WP that something has changed
        //and needs to be refreshed on the screen
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string p)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
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

        //Declare a name for each book
        private string _bookname;

        public string BookName
        {
            get
            {
                return _bookname;
            }
            set
            {
                if (value != _bookname)
                {
                    _bookname = value;
                    NotifyPropertyChanged("BookName");
                }
            }
        }

        //Declare an author for each book
        private string _authorid;

        public string AuthorID
        {
            get
            {
                return _authorid;
            }
            set
            {
                if (value != _authorid)
                {
                    _authorid = value;
                    NotifyPropertyChanged("AuthorID");
                }
            }
        }

        //Declare a date upon which each book is added to library
        private string _addedDate;

        public string AddedDate
        {
            get
            {
                return _addedDate;
            }
            set
            {
                if (value != _addedDate)
                {
                    _addedDate = value;
                    NotifyPropertyChanged("AddedDate");
                }
            }
        }

        //Declare the directory of the folder where the content is stored
        private string _contentdirectory;

        public String ContentDirectory
        {
            get
            {
                return _contentdirectory;
            }
            set
            {
                if (value != _contentdirectory)
                {
                    _contentdirectory = value;
                    NotifyPropertyChanged("ContentDirectory");
                }
            }
        }

        //Declare the jpeg that will work as the cover pic's location
        private string _coverpic;

        public String CoverPic
        {
            get
            {
                return _coverpic;
            }
            set
            {
                if (value != _coverpic)
                {
                    _coverpic = value;
                    NotifyPropertyChanged("CoverPic");
                }
            }
        }

        //Declare an array of the pages so that we know where to go when loading
        private List<ChapterModel> _chapters;

        public List<ChapterModel> Chapters
        {
            get
            {
                return _chapters;
            }
            set
            {
                if (value != _chapters)
                {
                    _chapters = value;
                    NotifyPropertyChanged("Chapters");
                }
            }
        }

        //declare an int to count the total pages in a book
        private int _totalPages;

        public int TotalPages
        {
            get
            {
                return _totalPages;
            }
            set
            {
                if (value != _totalPages)
                {
                    _totalPages = value;
                    NotifyPropertyChanged("TotalPages");
                }
            }
        }

        //declare an int to keep track of the current chapter
        private int _currentchapter;

        public int CurrentChapter
        {
            get
            {
                return _currentchapter;
            }
            set
            {
                if (value != _currentchapter)
                {
                    _currentchapter = value;
                    NotifyPropertyChanged("CurrentChapter");
                }
            }
        }

        //declare an int to keep track of the current page
        private int _currentpage;

        public int CurrentPage
        {
            get
            {
                return _currentpage;
            }
            set
            {
                if (value != _currentpage)
                {
                    _currentpage = value;
                    NotifyPropertyChanged("CurrentPage");
                }
            }
        }
    }
}
