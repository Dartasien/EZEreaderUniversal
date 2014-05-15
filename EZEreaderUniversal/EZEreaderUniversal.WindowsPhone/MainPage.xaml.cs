using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using EZEreaderUniversal.DataModels;
using Windows.ApplicationModel.Activation;
using EZEreaderUniversal.Common;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EZEreaderUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        public BooksModel LibrarySource;
        BookModel ourBook;

        private FileActivatedEventArgs _fileEventArgs = null;
        public FileActivatedEventArgs FileEvent
        {
            get { return _fileEventArgs; }
            set { _fileEventArgs = value; }
        }


        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        public async void CallRetrieveLibrary()
        {
            await RetrieveLibrary();
        }
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BottomBar.Visibility = Visibility.Collapsed;
            await RetrieveLibrary();
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            Current = this;
        }

        private async System.Threading.Tasks.Task RetrieveLibrary()
        {
            if (LibrarySource == null)
            {
                LibrarySource = new BooksModel();
                if (!LibrarySource.IsDataLoaded)
                {
                    await LibrarySource.LoadData();
                }
                this.DataContext = LibrarySource.Books;
                LibraryListView.ItemsSource = this.DataContext;
            }
            else
            {
                LibrarySource.CallUpdateBooks();
            }
        }

        public void CallUpdateBooks()
        {
            LibrarySource.CallUpdateBooks();
        }

        public void AddBookToLibrary(BooksModel newBook)
        {
            for (int i = 0; i < newBook.Books.Count; i++)
            {
                CallRetrieveLibrary();
                LibrarySource.Books.Add(newBook.Books[i]);
            }
        }

        private void LibraryListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            BottomBar.Visibility = Visibility.Collapsed;
            var listViewItem = sender as ListViewItem;
            if (listViewItem != null)
            {
                ourBook = listViewItem.DataContext as BookModel;
            }
            if (ourBook != null)
            {
                this.Frame.Navigate(typeof(ReadingPage), ourBook);
            }
        }

        internal void NavigateToFilePage()
        {
            this.Frame.Navigate(typeof(LoadingPage));
        }

        private void LibraryListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var listViewItem = sender as ListViewItem;
            if (listViewItem != null)
            {
                ourBook = listViewItem.DataContext as BookModel;
            }
            if (ourBook != null)
            {
                Debug.WriteLine(ourBook.BookID);
            }
            if (this.BottomBar != null)
            {
                BottomBar.Visibility = Visibility.Visible;
            }
        }

        private async void DeleteBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (ourBook != null)
            {
                await RetrieveLibrary();
                await this.LibrarySource.RemoveBook(ourBook);
            }
        }

        private void EditBarButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LibraryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BottomBar.Visibility = Visibility.Collapsed;
        }

        private void LibraryListView_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            if (BottomBar.Visibility == Visibility.Visible)
            {
                BottomBar.Visibility = Visibility.Collapsed;
            }
        }

    }
}
