﻿using System;
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
using CollectionView;

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
        public CollectionViewSource LibraryViewSource;
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

        
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.
            BottomBar.Visibility = Visibility.Collapsed;
            RecentReadsListView.Visibility = Visibility.Collapsed;
            LibraryListView.Visibility = Visibility.Visible;
            await RetrieveLibrary();
            LibraryListView.SelectedItem = null;
            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            Current = this;
        }
        
        /// <summary>
        /// Void method to call RetrieveLibrary from a non-async method
        /// </summary>
        public async void CallRetrieveLibrary()
        {
            await RetrieveLibrary();
        }

        /// <summary>
        /// Loads the library if it isn't already.
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task RetrieveLibrary()
        {
            if (LibrarySource == null)
            {
                LibrarySource = new BooksModel();
                if (!LibrarySource.IsDataLoaded)
                {
                    await LibrarySource.LoadData();
                }
                this.DataContext = LibrarySource;
                LibraryListView.ItemsSource = LibrarySource.SortedBooks;
                RecentReadsListView.ItemsSource = LibrarySource.RecentReads;
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

        /// <summary>
        /// Adds a new book to the library if called
        /// </summary>
        /// <param name="newBook"></param>
        public void AddBookToLibrary(BooksModel newBook)
        {
            for (int i = 0; i < newBook.Library.Count; i++)
            {
                CallRetrieveLibrary();
                LibrarySource.Library.Add(newBook.Library[i]);
            }
        }

        /// <summary>
        /// Opens the reading page on the selected book if bottombar or its associated
        /// items are not also open, if they are, it closes them
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (BottomBar.Visibility == Visibility.Visible ||
                DetailsGrid.Visibility == Visibility.Visible ||
                BookNameBox.Visibility == Visibility.Visible ||
                AuthorNameBox.Visibility == Visibility.Visible)
            {
                CloseAppBarAndDetailsIfOpen();
            }
            else
            {
                var listViewItem = sender as ListViewItem;
                if (listViewItem != null)
                {
                    ourBook = listViewItem.DataContext as BookModel;
                }
                this.LibraryListView.SelectedItem = null;
                if (ourBook != null)
                {
                    if (ourBook.IsStarted != true)
                    {
                        ourBook.IsStarted = true;
                        this.LibrarySource.RecentReads.Add(ourBook);
                    }
                    this.Frame.Navigate(typeof(ReadingPage), ourBook);
                }
            }
        }

        /// <summary>
        /// Closes the appbar and the details grid if called
        /// </summary>
        private void CloseAppBarAndDetailsIfOpen()
        {
            BottomBar.Visibility = Visibility.Collapsed;
            DetailsGrid.Visibility = Visibility.Collapsed;
            BookNameBox.Visibility = Visibility.Collapsed;
            AuthorNameBox.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Navigates to the loading page if a new epub is opened by the phone
        /// to be added to the app library
        /// </summary>
        internal void NavigateToFilePage()
        {
            this.Frame.Navigate(typeof(LoadingPage));
        }

        /// <summary>
        /// Displays the bottom bar on right click or long tap on book
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var listViewItem = sender as ListViewItem;
            if (listViewItem != null)
            {
                ourBook = listViewItem.DataContext as BookModel;
            }
            if (this.BottomBar != null)
            {
                BottomBar.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// deletes the currently selected book
        /// TODO: add confirmation box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (ourBook != null)
            {
                await RetrieveLibrary();
                await this.LibrarySource.RemoveBook(ourBook);
            }
        }

        /// <summary>
        /// Displays the currently selected books author and title for editting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditBarButton_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.Visibility = Visibility.Collapsed;
            DetailsGrid.Visibility = Visibility.Visible;
            BookNameBox.Visibility = Visibility.Visible;
            AuthorNameBox.Visibility = Visibility.Visible;
            BookNameBox.Text = ourBook.BookName;
            AuthorNameBox.Text = ourBook.AuthorID;
        }

        /// <summary>
        /// closes teh bottombar if it sticks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CloseAppBarAndDetailsIfOpen();
        }

        /// <summary>
        /// closes the bottomappbar if it sticks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryListView_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            if (BottomBar.Visibility == Visibility.Visible)
            {
                BottomBar.Visibility = Visibility.Collapsed;
            }
            LibraryListView.SelectedItem = null;
            DetailsGrid.Visibility = Visibility.Collapsed;
            BookNameBox.Visibility = Visibility.Collapsed;
            AuthorNameBox.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Allows the book's name to be editted by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BookNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ourBook.BookName = BookNameBox.Text;
            CallUpdateBooks();
        }

        /// <summary>
        /// Allows the author's name to be editted by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AuthorNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ourBook.AuthorID = AuthorNameBox.Text;
            CallUpdateBooks();
        }

        /// <summary>
        /// Method to allow for sorthing the library by author(first name) from either
        /// ascending or descending order.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (LibrarySource.SortedBooks.SortDescriptions[0].PropertyName != "AuthorID")
            {
                LibrarySource.SortedBooks.SortDescriptions[0].PropertyName = "AuthorID";
                LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Ascending;
            }
            else if (LibrarySource.SortedBooks.SortDescriptions[0].Direction != ListSortDirection.Ascending)
            {
                LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Ascending;
            }
            else
            {
                LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Descending;
            }
            LibrarySource.SortedBooks.Refresh();
        }


        /// <summary>
        /// Method to allow for sorting the library by Title from either
        /// ascending or descending order.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByTitle_Click(object sender, RoutedEventArgs e)
        {
            if (LibrarySource.SortedBooks.SortDescriptions[0].PropertyName != "BookName")
            {
                LibrarySource.SortedBooks.SortDescriptions[0].PropertyName = "BookName";
                LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Ascending;
            }
            else if (LibrarySource.SortedBooks.SortDescriptions[0].Direction != ListSortDirection.Ascending)
            {
                LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Ascending;
            }
            else
            {
                LibrarySource.SortedBooks.SortDescriptions[0].Direction = ListSortDirection.Descending;
            }
            LibrarySource.SortedBooks.Refresh();
        }

        private void LibraryView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (LibraryListView.Visibility != Visibility.Visible)
            {
                LibraryListView.Visibility = Visibility.Visible;
                RecentReadsListView.Visibility = Visibility.Collapsed;
            }
        }

        private void RecentReadsView_Tapped(object sender, TappedRoutedEventArgs e)
        {

            if (RecentReadsListView.Visibility != Visibility.Visible)
            {
                LibraryListView.Visibility = Visibility.Collapsed;
                RecentReadsListView.Visibility = Visibility.Visible;
            }
        }
    }
}
