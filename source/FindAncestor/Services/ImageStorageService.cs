using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using FindAncestor.Enum;

namespace FindAncestor.Services
{
    public class ImageStorageService
    {
        public void SaveImages(string[] files, ImageFolderType folder, ImageSaveFormat format)
        {
            string baseFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Image",
                folder.ToString());

            Directory.CreateDirectory(baseFolder);

            var existingFiles = Directory.GetFiles(baseFolder)
                .Select(f => int.TryParse(Path.GetFileNameWithoutExtension(f), out int n) ? n : 0);

            int nextNumber = existingFiles.Any() ? existingFiles.Max() + 1 : 1;

            foreach (var file in files)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(file));

                    BitmapEncoder encoder =
                        format == ImageSaveFormat.Jpeg
                            ? new JpegBitmapEncoder { QualityLevel = 90 }
                            : new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(bitmap));

                    string ext = format == ImageSaveFormat.Jpeg ? "jpg" : "png";
                    string savePath = Path.Combine(baseFolder, $"{nextNumber}.{ext}");

                    using var fs = new FileStream(savePath, FileMode.Create);
                    encoder.Save(fs);

                    nextNumber++;
                }
                catch { }
            }
        }

        public void DeleteImages(ImageFolderType folder)
        {
            string baseFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Image",
                folder.ToString());

            if (!Directory.Exists(baseFolder))
            {
                MessageBox.Show("フォルダが存在しません");
                return;
            }

            var files = Directory.GetFiles(baseFolder);

            if (files.Length == 0)
            {
                MessageBox.Show("削除する画像がありません");
                return;
            }

            if (MessageBox.Show("削除しますか？", "確認",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            foreach (var f in files)
                File.Delete(f);

            MessageBox.Show("削除完了");
        }
    }
}