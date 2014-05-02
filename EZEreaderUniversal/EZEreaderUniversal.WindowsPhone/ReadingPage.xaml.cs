using EZEreaderUniversal.Common;
using EZEreaderUniversal.ViewModels;
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

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace EZEreaderUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReadingPage : Page
    {
        BookModel thisBook;
        List<RichTextBlockOverflow> listRTBO = new List<RichTextBlockOverflow>();
        List<UIElement> uiList;
        bool pageBack = false;
        Run myRun;
        string chapterText;
        int pageNumber = 0;
        Paragraph para;
        RichTextBlock myRTBTest;
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
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {   
            thisBook = ((BookModel)e.NavigationParameter);
            this.DataContext = thisBook;
            //CreateFirstPage();
            CreateFirstPage();
        }


        /// <summary>
        /// Takes the chapters full html file and loads it, then converts to text, and finally
        /// loads that into a RichTextBox and adds that to the grid
        /// </summary>
        private void CreateFirstPage()
        {
            uiList = new List<UIElement>();
            LayoutRoot.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            LayoutRoot.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.Load(thisBook.ContentDirectory +
                thisBook.Chapters[thisBook.CurrentChapter].ChapterString);
            if (thisBook.CurrentChapter == 0)
            {
                chapterText = thisBook.BookName + "\n" + thisBook.AuthorID;
                myRun = new Run();
                myRun.Text = chapterText;
                para = new Paragraph();
                para.Inlines.Add(myRun);
                SetmyRTBTest();
            }
            else
            {
                chapterText = HtmlUtilities.ConvertToText(htmlDoc.DocumentNode.InnerHtml);
                myRun = new Run();
                if (chapterText == "")
                {
                    chapterText = " ";
                }
                myRun.Text = chapterText;
                para = new Paragraph();
                para.Inlines.Add(myRun);
                SetmyRTBTest();
            }
            myRun = new Run();
            if (chapterText == "")
            {
                chapterText = " ";
            }
            myRun.Text = chapterText;
            para = new Paragraph();
            para.Inlines.Add(myRun);
            SetmyRTBTest();
            myRTBTest.Blocks.Add(para);
            this.LayoutRoot.Children.Add(myRTBTest);
            uiList.Add(myRTBTest);
            //Debug.WriteLine(thisBook.Chapters[thisBook.CurrentChapter].ChapterString);
            //Debug.WriteLine(innerString);
            //Debug.WriteLine(chapterText);
        }


        private void SetmyRTBTest()
        {
            myRTBTest = new RichTextBlock();
            myRTBTest.IsTextSelectionEnabled = false;
            myRTBTest.Tapped += myRTB_Tapped;
            myRTBTest.TextAlignment = TextAlignment.Justify;
            myRTBTest.FontSize = 20;
            Thickness margin = myRTBTest.Margin;
            myRTBTest.Visibility = Visibility.Visible;
            margin.Left = 10;
            margin.Right = 10;
            margin.Top = 10;
            margin.Bottom = 10;

        }
        private void SetmyRTBTestBackwards()
        {
            myRTBTest = new RichTextBlock();
            myRTBTest.IsTextSelectionEnabled = false;
            myRTBTest.Tapped += myRTB_Tapped;
            myRTBTest.TextAlignment = TextAlignment.Justify;
            Thickness margin = myRTBTest.Margin;
            myRTBTest.Visibility = Visibility.Collapsed;
            margin.Left = 10;
            margin.Right = 10;
            margin.Top = 10;
            margin.Bottom = 10;

        }

        private void CreateAdditionalPages()
        {
            if (pageNumber == 0)
            {
                if (myRTBTest.HasOverflowContent)
                {

                }
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
        private void myRTB_Tapped(object sender, TappedRoutedEventArgs e)
        {
            
            Point eTap = e.GetPosition(myRTBTest);

            if (eTap.X > LayoutRoot.ActualWidth * .6)
            {
                if (pageNumber == 0)
                {
                    if (myRTBTest.HasOverflowContent)
                    {

                        listRTBO.Add(new RichTextBlockOverflow());
                        myRTBTest.OverflowContentTarget = listRTBO[pageNumber];
                        myRTBTest.Visibility = Visibility.Collapsed;
                        listRTBO[pageNumber].Visibility = Visibility.Visible;
                        pageNumber++;
                        thisBook.CurrentPage = pageNumber;
                        LayoutRoot.Children.Add(listRTBO[pageNumber - 1]);
                        uiList.Add(listRTBO[pageNumber - 1]);
                        listRTBO[pageNumber - 1].Tapped += myRTB_Tapped;

                    }
                    else
                    {
                        if (thisBook.CurrentChapter + 1 >= thisBook.Chapters.Count)
                        {
                            thisBook.CurrentChapter = 0;
                        }
                        else
                        {
                            thisBook.CurrentChapter++;
                        }
                        pageNumber = 0;
                        thisBook.CurrentPage = pageNumber;
                        listRTBO = new List<RichTextBlockOverflow>();
                        this.LayoutRoot.Children.Clear();
                        CreateFirstPage();
                    }
                }
                else
                {
                    if (listRTBO[pageNumber - 1].HasOverflowContent)
                    {
                        listRTBO.Add(new RichTextBlockOverflow());

                        listRTBO[pageNumber - 1].OverflowContentTarget = listRTBO[pageNumber];
                        listRTBO[pageNumber - 1].Visibility = Visibility.Collapsed;
                        listRTBO[pageNumber].Visibility = Visibility.Visible;
                        listRTBO[pageNumber].Tapped += myRTB_Tapped;
                        LayoutRoot.Children.Add(listRTBO[pageNumber]);
                        uiList.Add(listRTBO[pageNumber]);
                        pageNumber++;
                        thisBook.CurrentPage = pageNumber;
                    }
                    else
                    {
                        if (thisBook.CurrentChapter + 1 >= thisBook.Chapters.Count)
                        {
                            thisBook.CurrentChapter = 0;
                        }
                        else
                        {
                            thisBook.CurrentChapter++;
                        }
                        listRTBO[pageNumber - 1].Visibility = Visibility.Collapsed;
                        pageNumber = 0;
                        thisBook.CurrentPage = pageNumber;
                        listRTBO = new List<RichTextBlockOverflow>();
                        this.LayoutRoot.Children.Clear();
                        CreateFirstPage();
                    }
                }
            }
            else if (eTap.X < LayoutRoot.ActualWidth * .4)
            {
                if (pageNumber > 0)
                {
                    LayoutRoot.Children.RemoveAt(pageNumber);
                    listRTBO.RemoveAt(listRTBO.Count - 1);
                    pageNumber--;
                    LayoutRoot.Children.ElementAt(pageNumber).Visibility = Visibility.Visible;
                    
                    thisBook.CurrentPage = pageNumber;
                }
            }
        }   
    }
}
