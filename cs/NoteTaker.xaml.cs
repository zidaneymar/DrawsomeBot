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
using Windows.UI;

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

        private Contoso.NoteTaker.JSON.Format.Rectangle GetRectangle(Windows.UI.Input.Inking.InkPoint p, float size = 2.0f)
        {
            var res = new Contoso.NoteTaker.JSON.Format.Rectangle();
            res.TopX = (float)(p.Position.X - size / 2);
            res.TopY = (float)(p.Position.Y - size / 2);
            res.Width = size;
            res.Height = size;
            return res;
        }
        private void DrawStorkes(DrawsomePic pic)
        {
            var lines = pic.AllLines;

            foreach (var line in lines)
            {
                foreach (var rect in line.LittleRects)
                {
                    var poly = GetPolygon(rect, ColorHelper.FromArgb(255, 255, 255, 0));
                    recogCanvas.Children.Add(poly);
                }
            }
        }
        
        private void DrawRect(DrawsomePic pic)
        {
            foreach (var shape in pic.AllShapes)
            {
                debugCanvas.Children.Add(GetPolygon(shape.RecogUnit.BoundingRect, ColorHelper.FromArgb(255, 0, 255, 0)));
            }
        }

        private Polygon GetPolygon(Contoso.NoteTaker.JSON.Format.Rectangle obj, Color color)
        {
            var inchToMillimeterFactor = 25.4f;
            List<Point> points = new List<Point>();
            var scalingX = DpiX / inchToMillimeterFactor;
            var scalingY = DpiY / inchToMillimeterFactor;

            points.Add(new Point(obj.TopX * scalingX, obj.TopY * scalingY));
            points.Add(new Point((obj.TopX + obj.Width) * scalingX, obj.TopY * scalingY));
            points.Add(new Point((obj.TopX + obj.Width) * scalingX, (obj.TopY + obj.Height) * scalingY));
            points.Add(new Point(obj.TopX * scalingX, (obj.TopY + obj.Height) * scalingY));

            Polygon polygon = new Polygon();

            foreach (Point point in points)
            {
                polygon.Points.Add(point);
            }

            var brush = new SolidColorBrush(color);
            polygon.Stroke = brush;
            polygon.StrokeThickness = 1;

            return polygon;
        }

        //private void DrawingDebugInfoForShape(DrawsomeShape shape)
        //{
        //    debugCanvas.Children.Add(GetPolygon(shape.BoundingRect, Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255)));

        //    if (shape.Next != null)
        //    {
        //        DrawingDebugInfoForLine(shape.Next);
        //    }

        //    if (shape.NextFalse != null)
        //    {
        //        DrawingDebugInfoForLine(shape.NextFalse);
        //    }

        //}

        //private void DrawingDebugInfoForLine(DrawsomeLine shape)
        //{
        //    debugCanvas.Children.Add(GetPolygon(shape.BoundingRect, ColorHelper.FromArgb(255, 255, 0, 0)));
            
        //    if (shape.Next != null)
        //    {
        //        DrawingDebugInfoForShape(shape.Next);
        //    }
        //}

        private void DrawAllRecognzied(InkRecognitionRoot root)
        {
            var units = root.GetUnits().ToList();
            foreach (var unit in units)
            {
                recogCanvas.Children.Add(GetPolygon(unit.BoundingRect, ColorHelper.FromArgb(255,0,255,0)));
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
                recogCanvas.Children.Clear();
                debugCanvas.Children.Clear();
                var status = await inkRecognizer.RecognizeAsync();
                if (status == HttpStatusCode.OK)
                {
                    var root = inkRecognizer.GetRecognizerRoot();
                    if (root != null)
                    {
                        var pic = new DrawsomePic(root, inkRecognizer.strokes);

                        if (pic.Root != null)
                        {
                            DrawRect(pic);
                            //DrawingDebugInfoForShape(pic.Root);
                            //DrawAllRecognzied(root);
                            DrawStorkes(pic);
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
