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

        private readonly DispatcherTimer dispatcherTimer;
        private readonly float DpiX;
        private readonly float DpiY;
        // Time to wait before triggering ink recognition operation
        const double IDLE_WAITING_TIME = 1000;

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

            inkPresenter.StrokeInput.StrokeStarted += InkPresenter_StrokeInputStarted;
            inkPresenter.StrokeInput.StrokeEnded += InkPresenter_StrokeInputEnded;
            inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            inkPresenter.StrokesErased += InkPresenter_StrokesErased;

            inkRecognizer = new InkRecognizer(subscriptionKey, endpoint, inkRecognitionUrl);

            displayInfo = DisplayInformation.GetForCurrentView();
            inkRecognizer.SetDisplayInformation(displayInfo);

            DpiX = displayInfo.RawDpiX;
            DpiY = displayInfo.RawDpiY;
            //dispatcherTimer = new DispatcherTimer();
            //dispatcherTimer.Tick += DispatcherTimer_Tick;
            //dispatcherTimer.Interval = TimeSpan.FromMilliseconds(IDLE_WAITING_TIME);
        }

        private void InkPresenter_StrokeInputStarted(Windows.UI.Input.Inking.InkStrokeInput sender, PointerEventArgs args)
        {
            //StopTimer();
        }

        private void InkPresenter_StrokeInputEnded(Windows.UI.Input.Inking.InkStrokeInput sender, PointerEventArgs args)
        {
            //StartTimer();
        }

        private void InkPresenter_StrokesCollected(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
        {
            //StopTimer();

            foreach (var stroke in args.Strokes)
            {
                inkRecognizer.AddStroke(stroke);
            }

            //StartTimer();
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
            polygon.StrokeThickness = 2;
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
            polygon.StrokeThickness = 2;
            debugCanvas.Children.Add(polygon);
            
            if (shape.Next != null)
            {
                DrawingDebugInfoForShape(shape.Next);
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
                //output.Text = OutputWriter.PrintError(ex.Message);
            }
        }

        private void InkPresenter_StrokesErased(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesErasedEventArgs args)
        {
            //StopTimer();

            foreach (var stroke in args.Strokes)
            {
                inkRecognizer.RemoveStroke(stroke.Id);
            }

            //StartTimer();
        }

        //private async void DispatcherTimer_Tick(object sender, object e)
        //{
        //    StopTimer();

        //    try
        //    {
        //        var status = await inkRecognizer.RecognizeAsync();
        //        if (status == HttpStatusCode.OK)
        //        {
        //            var root = inkRecognizer.GetRecognizerRoot();
        //            if (root != null)
        //            {
        //                output.Text = OutputWriter.Print(root);
        //            }
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        output.Text = OutputWriter.PrintError(ex.Message);
        //    }
        //}

        //public void StartTimer() => dispatcherTimer.Start();
        //public void StopTimer() => dispatcherTimer.Stop();
    }
}
