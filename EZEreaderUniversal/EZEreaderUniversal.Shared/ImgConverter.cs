using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml.Data;
using EZEreaderUniversal;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace EZEreaderUniversal
{
    class ImgConverter : IValueConverter
    {

        /// <summary>
        /// Overriden Convert method for IValueConverter
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string path = value as string;
            if (String.IsNullOrEmpty(path))
                return null;
            if ((path.Length > 9) && (path.ToLower().Substring(0, 9).Equals("isostore:")))
            {
                BitmapImage bmp = new BitmapImage();

                SetSourceOne(bmp, path.Substring(9));

                return bmp;
            }
            return path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method to call the SetSource method and allow async to work
        /// </summary>
        /// <param name="img"></param>
        /// <param name="path"></param>
        public async void SetSourceOne(BitmapImage img, string path)
        {
            await SetSource(img, path);
        }

        /// <summary>
        /// Method to set the source of the pic from windows storage
        /// </summary>
        /// <param name="img">bitmapimage to set a source for</param>
        /// <param name="path">string location of the image in storage</param>
        /// <returns>sets the img.source to a new filestream</returns>
        public async Task SetSource(BitmapImage img, string path)
        {
            StorageFile imageFile = null;
            string[] folders = path.Split('/');
            Debug.WriteLine(path);
            try
            {
                StorageFolder appBaseFolder = ApplicationData.Current.LocalFolder;
                StorageFolder imageFolder = await IO.CreateOrGetFolders(appBaseFolder, folders);
                imageFile = await imageFolder.GetFileAsync(folders[folders.Length - 1]);
            }
            catch (Exception)
            {
                Debug.WriteLine("filenotfound");
            }
            if (imageFile != null)
            {
                using (var fileStream = await imageFile.OpenReadAsync())
                {
                    if (fileStream.CanRead)
                    {
                        await img.SetSourceAsync(fileStream);
                    }
                }
            }
        }
    }
}
