using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml.Data;
using EZEreaderUniversal;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace EZEreaderUniversal
{
    class ImgConverter : IValueConverter
    {
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

        public async void SetSourceOne(BitmapImage img, string path)
        {
            await SetSource(img, path);
        }

        public async Task SetSource(BitmapImage img, string path)
        {
            Debug.WriteLine(path);
            string[] folders = path.Split('/');
            StorageFolder appBaseFolder = ApplicationData.Current.LocalFolder;
            StorageFolder imageFolder = await IO.CreateOrGetFolders(appBaseFolder, folders);
            StorageFile imageFile = await imageFolder.GetFileAsync(folders[folders.Length - 1]);
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
