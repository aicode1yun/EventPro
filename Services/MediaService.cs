namespace Ticket.Services
{
    public class MediaService : IMediaService
    {
        public async Task<bool> SaveToGalleryAsync(string filePath, string title)
        {
#if ANDROID
            var context = Android.App.Application.Context;
            var contentValues = new Android.Content.ContentValues();
            contentValues.Put(Android.Provider.MediaStore.Images.Media.InterfaceConsts.DisplayName, title);
            contentValues.Put(Android.Provider.MediaStore.Images.Media.InterfaceConsts.MimeType, "image/png");

            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                contentValues.Put(Android.Provider.MediaStore.Images.Media.InterfaceConsts.RelativePath, "DCIM/EventPro");
            }

            var extUri = Android.Provider.MediaStore.Images.Media.ExternalContentUri!;
            var uri = context.ContentResolver?.Insert(extUri, contentValues);

            if (uri is not null)
            {
                using var inputStream = System.IO.File.OpenRead(filePath);
                using var outputStream = context.ContentResolver?.OpenOutputStream(uri);
                if (outputStream is not null)
                {
                    await inputStream.CopyToAsync(outputStream);
                    return true;
                }
            }
            return false;
#elif IOS
            var data = Foundation.NSData.FromFile(filePath);
            if (data is null) return false;
            var image = UIKit.UIImage.LoadFromData(data);
            if (image is null) return false;
            var tcs = new TaskCompletionSource<bool>();
            image.SaveToPhotosAlbum((img, error) =>
            {
                tcs.SetResult(error is null);
            });
            return await tcs.Task;
#else
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = title,
                File = new ShareFile(filePath)
            });
            return true;
#endif
        }
    }
}
