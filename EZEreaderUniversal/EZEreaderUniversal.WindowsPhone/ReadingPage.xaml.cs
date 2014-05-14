using EZEreaderUniversal.Common;
using EZEreaderUniversal.DataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Html;
using HtmlAgilityPack;
using Windows.UI.Xaml.Documents;
using System.Diagnostics;
using Windows.Storage;
using System.Threading.Tasks;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace EZEreaderUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReadingPage : Page
    {
        StorageFolder appFolder = ApplicationData.Current.LocalFolder;
        MainPage rootPage = MainPage.Current;
        BookModel thisBook;
        List<RichTextBlockOverflow> listRTBO = new List<RichTextBlockOverflow>();
        Run myRun;
        string chapterText;
        int pageNumber;
        //int currentPageCount;
        Paragraph para;
        RichTextBlock myRTB;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public ReadingPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {   
            thisBook = ((BookModel)e.NavigationParameter);
            this.DataContext = thisBook;
            await CreateFirstPage();
        }


        /// <summary>
        /// Takes the chapters full html file and loads it, then converts to text, and finally
        /// loads that into a RichTextBox and adds that to the grid
        /// </summary>
        private async Task CreateFirstPage()
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            
            if (thisBook.CurrentChapter == 0)
            {
                //eventually adding image to this page
                chapterText = thisBook.BookName + "\n" + thisBook.AuthorID;
                myRun = new Run();
                myRun.Text = chapterText;
                para = new Paragraph();
                para.Inlines.Add(myRun);
                SetmyRTB();
            }
            else
            {
                if (thisBook.IsoStore)
                {
                    await GetChapterFromStorage(htmlDoc);
                }
                else
                {
                    htmlDoc.Load(thisBook.MainDirectory +
                        thisBook.Chapters[thisBook.CurrentChapter].ChapterString);
                    chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
                }
                
                myRun = new Run();
                if (chapterText == "")
                {
                    chapterText = " ";
                }
                myRun.Text = chapterText;
                para = new Paragraph();
                para.Inlines.Add(myRun);
                SetmyRTB();
            }
            pageNumber = 0;
            myRTB.Blocks.Add(para);
            LayoutRoot.Children.Add(myRTB);
            CreateAdditionalPages();
            ReturnToCurrentPage();
        }

        private async Task GetChapterFromStorage(HtmlDocument htmlDoc)
        {
            StorageFolder chapterFolder;
            string fullChapterString;
            string[] st;
            string contentLoc = thisBook.ContentDirectory;
            if (thisBook.ContentDirectory.Contains('/'))
            {
                st = contentLoc.Split('/');
                contentLoc = "";
                for (int i = 0; i < st.Length - 1; i++)
                {
                    contentLoc += st[i];
                }
                fullChapterString = thisBook.MainDirectory +
            contentLoc + "/" +
                thisBook.Chapters[thisBook.CurrentChapter].ChapterString;
            }
            else
            {
                fullChapterString = thisBook.MainDirectory +
                thisBook.Chapters[thisBook.CurrentChapter].ChapterString;
            }
            
            string[] fullChapterStrings = fullChapterString.Split('/');
            string chapterString = fullChapterStrings[fullChapterStrings.Length - 1];
            string[] chapterStringLoc =
                fullChapterString.Split('/');

            chapterFolder =
                await IO.CreateOrGetFolders(appFolder, chapterStringLoc);
            using (var file = await chapterFolder.OpenStreamForReadAsync(chapterString))
            {
                htmlDoc.Load(file);
                chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
            }
        }

        /// <summary>
        /// Set the first page using RichTextBlock so that we can use the overflow
        /// for going forwards.
        /// </summary>
        private void SetmyRTB()
        {
            myRTB = new RichTextBlock();
            myRTB.IsTextSelectionEnabled = false;
            myRTB.Tapped += myRTB_Tapped;
            myRTB.TextAlignment = TextAlignment.Justify;
            myRTB.FontSize = 20;
            Thickness margin = myRTB.Margin;
            myRTB.Visibility = Visibility.Visible;
            margin.Left = 10;
            margin.Right = 10;
            margin.Top = 10;
            margin.Bottom = 10;

        }

        private void CreateAdditionalPages()
        {
            listRTBO = new List<RichTextBlockOverflow>();
            LayoutRoot.UpdateLayout();
            if (myRTB.HasOverflowContent)
            {
                listRTBO.Add(new RichTextBlockOverflow());
                myRTB.OverflowContentTarget = listRTBO[pageNumber];
                listRTBO[pageNumber].Visibility = Visibility.Visible;
                listRTBO[pageNumber].Tapped += myRTB_Tapped;
                pageNumber++;
                LayoutRoot.Children.Add(listRTBO[pageNumber - 1]);
                myRTB.Visibility = Visibility.Collapsed;
                LayoutRoot.UpdateLayout();

                while (listRTBO[pageNumber - 1].HasOverflowContent)
                {
                    listRTBO.Add(new RichTextBlockOverflow());
                    listRTBO[pageNumber - 1].OverflowContentTarget = listRTBO[pageNumber];
                    listRTBO[pageNumber - 1].Visibility = Visibility.Collapsed;
                    listRTBO[pageNumber].Visibility = Visibility.Visible;
                    listRTBO[pageNumber].Tapped += myRTB_Tapped;
                    LayoutRoot.Children.Add(listRTBO[pageNumber]);
                    pageNumber++;
                    LayoutRoot.UpdateLayout();
                }
            }
            thisBook.Chapters[thisBook.CurrentChapter].PageCount = LayoutRoot.Children.Count();
        }

        /// <summary>
        /// Sets the first page of the chapter to visible
        /// </summary>
        private void ReturnToFirstPage()
        {
            LayoutRoot.Children.Last().Visibility = Visibility.Collapsed;
            LayoutRoot.Children.First().Visibility = Visibility.Visible;
        }

        /// <summary>
        /// sets the page to the previously saved page if the book was opened before
        /// </summary>
        private void ReturnToCurrentPage()
        {
            if (thisBook.CurrentPage == 0)
            {
                ReturnToFirstPage();
            }
            else
            {
                if (thisBook.Chapters[thisBook.CurrentChapter].PageCount > 0)
                {
                    if (LayoutRoot.Children.Count !=
                        thisBook.Chapters[thisBook.CurrentChapter].PageCount)
                    {
                        double pagePercentage = (thisBook.CurrentPage /
                            thisBook.Chapters[thisBook.CurrentChapter].PageCount);
                        thisBook.CurrentPage = (int)(pagePercentage *
                            LayoutRoot.Children.Count);
                    }
                }
                LayoutRoot.Children.Last().Visibility = Visibility.Collapsed;
                LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = 
                    Visibility.Visible;
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            //rootPage.CallUpdateBooks();
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// switches to a new page in the chapter upon tap and switches to
        /// a new chapter if we hit the last page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void myRTB_Tapped(object sender, TappedRoutedEventArgs e)
        {
            
            Point eTap = e.GetPosition(LayoutRoot.Children.ElementAt(thisBook.CurrentPage));

            if (eTap.X > LayoutRoot.ActualWidth * .6)
            {
                LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = Visibility.Collapsed;
                if (thisBook.CurrentPage +1 >= LayoutRoot.Children.Count)
                {
                    if (thisBook.CurrentChapter + 1 >= thisBook.Chapters.Count)
                    {
                        thisBook.CurrentChapter = 0;
                    }
                    else
                    {
                        thisBook.CurrentChapter++;
                    }
                    thisBook.CurrentPage = 0;
                    LayoutRoot.Children.Clear();
                    await CreateFirstPage();
                }
                else
                {
                    LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = Visibility.Collapsed;
                    thisBook.CurrentPage++;
                    LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = Visibility.Visible;
                }
                
                
            }
            else if (eTap.X < LayoutRoot.ActualWidth * .4)
            {
                if (thisBook.CurrentPage > 0)
                {
                    LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility =
                        Visibility.Collapsed;
                    thisBook.CurrentPage--;
                    LayoutRoot.Children.ElementAt(thisBook.CurrentPage).Visibility = 
                        Visibility.Visible;
                }
                else
                {
                    await CreateBackwardsPages();
                    thisBook.CurrentPage = LayoutRoot.Children.Count - 1;
                    thisBook.Chapters[thisBook.CurrentChapter].PageCount 
                        = LayoutRoot.Children.Count;

                }
            }
        }

        /// <summary>
        /// Creates the pages for going backwards to a previous chapter on tap
        /// </summary>
        private async Task CreateBackwardsPages()
        {
            if (thisBook.CurrentChapter > 0)
            {
                thisBook.CurrentChapter--;
                LayoutRoot.Children.Clear();
                HtmlDocument htmlDoc = new HtmlDocument();
                if (thisBook.CurrentChapter == 0)
                {
                    //eventually adding image to this page
                    chapterText = thisBook.BookName + "\n" + thisBook.AuthorID;
                    myRun = new Run();
                    myRun.Text = chapterText;
                    para = new Paragraph();
                    para.Inlines.Add(myRun);
                    SetmyRTB();
                }
                else
                {
                    if (thisBook.IsoStore)
                    {
                        await GetChapterFromStorage(htmlDoc);
                    }
                    else
                    {
                        htmlDoc.Load(thisBook.MainDirectory +
                            thisBook.Chapters[thisBook.CurrentChapter].ChapterString);
                        chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
                    }
                    myRun = new Run();
                    if (chapterText == "")
                    {
                        chapterText = " ";
                    }
                    myRun.Text = chapterText;
                    para = new Paragraph();
                    para.Inlines.Add(myRun);
                    SetmyRTB();
                }
                myRTB.Blocks.Clear();
                myRTB.Blocks.Add(para);
                LayoutRoot.Children.Add(myRTB);
                pageNumber = 0;
                CreateAdditionalPages();
                thisBook.CurrentPage = pageNumber +1;
            }
        }
    }
}
