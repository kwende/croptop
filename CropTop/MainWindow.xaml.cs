using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CropTop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> _images = null;
        private int _counter = 0;
        private List<ClickCoordinate> _points = null; 

        [Serializable]
        class ClickCoordinate
        {
            public string Name { get; set; }
            public System.Windows.Point Coordinate { get; set; }
            public Rectangle DrawingRectangle { get; set; }
        }

        enum PointType
        {
            LeftEye = 0,
            RightEye = 1,
            LeftMouth = 2,
            MiddleMouth = 3,
            RightMouth = 4
        }

        public MainWindow()
        {
            InitializeComponent();

            this.KeyUp += MainWindow_KeyUp;
        }

        private void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                _points = new List<ClickCoordinate>();
                setImage(_counter);
            }
            else if(e.Key == Key.Enter)
            {
                if(_points.Count == 5)
                {
                    string imagePath = _images[_counter];
                    string jsonFilePath = Path.Join(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath) + ".json");

                    string output = JsonConvert.SerializeObject(_points, Formatting.Indented);

                    File.WriteAllText(jsonFilePath, output); 

                    _points = new List<ClickCoordinate>();

                    this.Title = $"{++_counter}/{_images.Count}"; 
                    setImage(_counter);
                }
            }
            else if(e.Key == Key.Delete)
            {
                File.Delete(_images[_counter]);
                _counter++;

                setImage(_counter);
            }
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _images = new List<string>(); 

                foreach (string file in Directory.GetFiles(dialog.SelectedPath))
                {
                    // if it's an image file and we haven't already tagged it, then add it. 

                    if(!file.EndsWith(".json") && (file.EndsWith(".jpg") || file.EndsWith(".jpeg") || file.EndsWith(".png")) &&
                        !File.Exists(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".json")))
                    {
                        _images.Add(file);
                    }
                }

                setImage(_counter); 
            }
        }

        private void setImage(int index)
        {
            if (_images != null && _images.Count > index)
            {
                using(Bitmap bmp = (Bitmap)Bitmap.FromFile(_images[index]))
                {
                    Image.Source = Convert(bmp);
                    _points = new List<ClickCoordinate>(); 
                }
            }
        }

        public static BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(_points.Count < 5)
            {
                System.Windows.Point mousePoint = e.GetPosition(Image);

                using (Bitmap bmp = (Bitmap)Bitmap.FromFile(_images[_counter]))
                {
                    float xDisplayRatio = (float)(bmp.Width / (Image.ActualWidth * 1.0f));
                    float yDisplayRatio = (float)(bmp.Height / (Image.ActualHeight * 1.0f));

                    ClickCoordinate coordinateToAdd = new ClickCoordinate
                    {
                        Coordinate = new System.Windows.Point((int)Math.Floor(mousePoint.X * xDisplayRatio),
                            (int)Math.Floor(mousePoint.Y * yDisplayRatio)),
                        Name = ((PointType)_points.Count).ToString(),
                        DrawingRectangle = new Rectangle((int)Math.Floor((mousePoint.X-5) * xDisplayRatio),
                            (int)Math.Floor((mousePoint.Y-5) * yDisplayRatio), 10, 10),
                    }; 

                    _points.Add(coordinateToAdd); 

                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        for(int c=0;c<_points.Count;c++)
                        {
                            System.Windows.Point point = _points[c].Coordinate;
                            Rectangle drawingRectangle = _points[c].DrawingRectangle;

                            g.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.Red, 2), drawingRectangle); 
                            g.DrawString(_points[c].Name, new Font(System.Drawing.FontFamily.GenericSansSerif, 15),
                                System.Drawing.Brushes.White, (int)point.X -5, (int)point.Y - 30); 
                        }
                    }

                    Image.Source = Convert(bmp);
                }
            }
        }
    }
}
