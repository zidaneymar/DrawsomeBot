using System;
using System.Net;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Contoso.NoteTaker.Services.Ink;
using Windows.Graphics.Display;
using Windows.UI.Core;
using NoteTaker.Model;
using NoteTaker.Helpers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Foundation;
using System.Collections.Generic;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI.Popups;
using System.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NoteTaker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NoteTaker : Page
    {
        InkRecognizer inkRecognizer;
        DisplayInformation displayInfo;

        private readonly float DpiX;
        private readonly float DpiY;

        public NoteTaker()
        {
            this.InitializeComponent();

            // Replace the subscriptionKey string value with your valid subscription key.
            const string subscriptionKey = "ad3f6f1060e54ceaaf7dcae9d74c9fa4";

            // URI information for ink recognition:
            const string endpoint = "https://api.cognitive.microsoft.com";
            const string inkRecognitionUrl = "/inkrecognizer/v1.0-preview/recognize";

            var inkPresenter = inkCanvas.InkPresenter;
            inkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Mouse;

            inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            inkPresenter.StrokesErased += InkPresenter_StrokesErased;

            inkRecognizer = new InkRecognizer(subscriptionKey, endpoint, inkRecognitionUrl);

            displayInfo = DisplayInformation.GetForCurrentView();
            inkRecognizer.SetDisplayInformation(displayInfo);

            DpiX = displayInfo.RawDpiX;
            DpiY = displayInfo.RawDpiY;

            debugCanvas.Visibility = Visibility.Collapsed;
            recogCanvas.Visibility = Visibility.Collapsed;
        }


        private void InkPresenter_StrokesCollected(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
        {
            foreach (var stroke in args.Strokes)
            {
                inkRecognizer.AddStroke(stroke);
            }
        }

        private void DrawingDebugInfoForShape(DrawsomeShape shape)
        {
            var inchToMillimeterFactor = 25.4f;
            List<Point> points = new List<Point>();
            var scalingX = DpiX / inchToMillimeterFactor;
            var scalingY = DpiY / inchToMillimeterFactor;
            points.Add(new Point(shape.BoundingRect.TopX * scalingX, shape.BoundingRect.TopY * scalingY));
            points.Add(new Point((shape.BoundingRect.TopX + shape.BoundingRect.Width) * scalingX, shape.BoundingRect.TopY * scalingY));
            points.Add(new Point((shape.BoundingRect.TopX + shape.BoundingRect.Width) * scalingX, (shape.BoundingRect.TopY + shape.BoundingRect.Height) * scalingY));
            points.Add(new Point(shape.BoundingRect.TopX * scalingX, (shape.BoundingRect.TopY + shape.BoundingRect.Height) * scalingY));

            Polygon polygon = new Polygon();

            foreach (Point point in points)
            {
                polygon.Points.Add(point);
            }

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
            polygon.Stroke = brush;
            polygon.StrokeThickness = 1;
            debugCanvas.Children.Add(polygon);

            if (shape.Next != null)
            {
                DrawingDebugInfoForLine(shape.Next);
            }

            if (shape.NextFalse != null)
            {
                DrawingDebugInfoForLine(shape.NextFalse);
            }

        }

        private void DrawingDebugInfoForLine(DrawsomeLine shape)
        {
            var inchToMillimeterFactor = 25.4f;
            List<Point> points = new List<Point>();
            var scalingX = DpiX / inchToMillimeterFactor;
            var scalingY = DpiY / inchToMillimeterFactor;
            points.Add(new Point(shape.BoundingRect.TopX * scalingX, shape.BoundingRect.TopY * scalingY));
            points.Add(new Point((shape.BoundingRect.TopX + shape.BoundingRect.Width) * scalingX, shape.BoundingRect.TopY * scalingY));
            points.Add(new Point((shape.BoundingRect.TopX + shape.BoundingRect.Width) * scalingX, (shape.BoundingRect.TopY + shape.BoundingRect.Height) * scalingY));
            points.Add(new Point(shape.BoundingRect.TopX * scalingX, (shape.BoundingRect.TopY + shape.BoundingRect.Height) * scalingY));

            Polygon polygon = new Polygon();

            foreach (Point point in points)
            {
                polygon.Points.Add(point);
            }

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 255, 0, 0));
            polygon.Stroke = brush;
            polygon.StrokeThickness = 1;
            debugCanvas.Children.Add(polygon);
            
            if (shape.Next != null)
            {
                DrawingDebugInfoForShape(shape.Next);
            }
        }

        private void DrawAllRecognzied(InkRecognitionRoot root)
        {
            var units = root.GetUnits().ToList();
            foreach (var unit in units)
            {
                var inchToMillimeterFactor = 25.4f;
                List<Point> points = new List<Point>();
                var scalingX = DpiX / inchToMillimeterFactor;
                var scalingY = DpiY / inchToMillimeterFactor;
                points.Add(new Point(unit.BoundingRect.TopX * scalingX, unit.BoundingRect.TopY * scalingY));
                points.Add(new Point((unit.BoundingRect.TopX + unit.BoundingRect.Width) * scalingX, unit.BoundingRect.TopY * scalingY));
                points.Add(new Point((unit.BoundingRect.TopX + unit.BoundingRect.Width) * scalingX, (unit.BoundingRect.TopY + unit.BoundingRect.Height) * scalingY));
                points.Add(new Point(unit.BoundingRect.TopX * scalingX, (unit.BoundingRect.TopY + unit.BoundingRect.Height) * scalingY));

                Polygon polygon = new Polygon();

                foreach (Point point in points)
                {
                    polygon.Points.Add(point);
                }

                var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 255, 0));
                polygon.Stroke = brush;
                polygon.StrokeThickness = 1;
                recogCanvas.Children.Add(polygon);
            }
        }


        private void Debug_Clicked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    debugCanvas.Visibility = Visibility.Visible;   
                }
                else
                {
                    debugCanvas.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ShowAllRecognized_Clicked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    recogCanvas.Visibility = Visibility.Visible;
                }
                else
                {
                    recogCanvas.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var status = await inkRecognizer.RecognizeAsync();
                if (status == HttpStatusCode.OK)
                {
                    var root = inkRecognizer.GetRecognizerRoot();
                    if (root != null)
                    {
                        var pic = new DrawsomePic(root);

                        if (pic.Root != null)
                        {
                            DrawingDebugInfoForShape(pic.Root);
                            DrawAllRecognzied(root);
                            //output.Text = pic.ToString();
                            var composerBot = await BotGenerator.Parse(pic);
                            var composerJson = JsonConvert.SerializeObject(composerBot, Formatting.Indented);

                            FileSavePicker picker = new FileSavePicker();
                            picker.FileTypeChoices.Add("file style", new string[] { ".txt", ".dialog" });
                            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                            picker.SuggestedFileName = "Main.dialog";
                            StorageFile file = await picker.PickSaveFileAsync();

                            if (file != null)
                            {
                                await FileIO.WriteTextAsync(file, composerJson);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message);
                await messageDialog.ShowAsync();
            }
        }

        private void InkPresenter_StrokesErased(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesErasedEventArgs args)
        {
            foreach (var stroke in args.Strokes)
            {
                inkRecognizer.RemoveStroke(stroke.Id);
            }
        }
    }
}
