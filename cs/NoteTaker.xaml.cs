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
using System.Net.Http;
using Windows.UI.Composition;
using System.Numerics;
using Windows.UI.Input.Inking;
using Windows.Storage.Streams;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NoteTaker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NoteTaker : Page
    {
        Contoso.NoteTaker.Services.Ink.InkRecognizer inkRecognizer;
        DisplayInformation displayInfo;

        private readonly float DpiX;
        private readonly float DpiY;

        public ComposerStep curEditingStep;

        private ComposerBot botInstance;

        private readonly Dictionary<string, int> TypeToIndex = new Dictionary<string, int>()
        {
            { "Microsoft.SendActivity", 0 },
            { "Microsoft.IfCondition", 1 },
            { "Microsoft.HttpRequest", 2 },
            { "Microsoft.TextInput", 3 },
            { "Microsoft.SetProperty", 4 },
        };

        Compositor _compositor = Window.Current.Compositor;
        SpringVector3NaturalMotionAnimation _springAnimation;

        private void CreateOrUpdateSpringAnimation(float finalValue)
        {
            if (_springAnimation == null)
            {
                _springAnimation = _compositor.CreateSpringVector3Animation();
                _springAnimation.Target = "Scale";
            }

            _springAnimation.FinalValue = new Vector3(finalValue);
        }

        private void PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Scale up to 1.5
            CreateOrUpdateSpringAnimation(1.3f);

            (sender as UIElement).StartAnimation(_springAnimation);
        }

        private void PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Scale back down to 1.0
            CreateOrUpdateSpringAnimation(1.0f);

            (sender as UIElement).StartAnimation(_springAnimation);
        }

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

            inkCanvas.RightTapped += InkCanvas_RightTapped;
            inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            inkPresenter.StrokesErased += InkPresenter_StrokesErased;

            inkRecognizer = new Contoso.NoteTaker.Services.Ink.InkRecognizer(subscriptionKey, endpoint, inkRecognitionUrl);

            displayInfo = DisplayInformation.GetForCurrentView();
            inkRecognizer.SetDisplayInformation(displayInfo);
             
            DpiX = displayInfo.RawDpiX;
            DpiY = displayInfo.RawDpiY;

            debugCanvas.Visibility = ShapeButton.IsOn ? Visibility.Visible : Visibility.Collapsed;
            recogCanvas.Visibility = LineButton.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Visibility

        public static Visibility GetVisibilityForActivity(object type)
        {
            if (type == null)
            {
                return Visibility.Collapsed;
            }
            return type.ToString() == "Microsoft." + nameof(SendActivity) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility GetVisibilityForProperty(object type)
        {
            if (type == null)
            {
                return Visibility.Collapsed;
            }
            return type.ToString() != "Microsoft." + nameof(IfCondition) && type.ToString() != "Microsoft." + nameof(SendActivity) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility GetVisibilityForCondition(object type)
        {
            if (type == null)
            {
                return Visibility.Collapsed;
            }
            return type.ToString() == "Microsoft." + nameof(IfCondition) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility GetVisibilityForHttp(object type)
        {
            if (type == null)
            {
                return Visibility.Collapsed;
            }
            return type.ToString() == "Microsoft." + nameof(HttpRequest) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility GetVisibilityForPrompt(object type)
        {
            if (type == null)
            {
                return Visibility.Collapsed;
            }
            return type.ToString() == "Microsoft." + nameof(TextInput) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static Visibility GetVisibilityForValue(object type)
        {
            if (type == null)
            {
                return Visibility.Collapsed;
            }
            return type.ToString() == "Microsoft." + nameof(SetProperty) ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        private void SetForm()
        {
            switch (curEditingStep.Type)
            {
                case "Microsoft.SendActivity":
                    this.activity.Text = (curEditingStep as SendActivity)?.Activity ?? string.Empty;
                    break;
                case "Microsoft.IfCondition":
                    this.condition.Text = (curEditingStep as IfCondition)?.Condition ?? string.Empty;
                    break;
                case "Microsoft.SetProperty":
                    this.property.Text = (curEditingStep as SetProperty)?.Property ?? string.Empty;
                    this.value.Text = (curEditingStep as SetProperty)?.Value ?? string.Empty;
                    break;
                case "Microsoft.HttpRequest":
                    this.url.Text = (curEditingStep as HttpRequest)?.Url ?? string.Empty;
                    this.method.Text = (curEditingStep as HttpRequest)?.Method ?? string.Empty;
                    this.body.Text = (curEditingStep as HttpRequest)?.Body ?? string.Empty;
                    this.property.Text = (curEditingStep as HttpRequest)?.Property ?? string.Empty;

                    break;
                case "Microsoft.TextInput":
                    this.prompt.Text = (curEditingStep as TextInput)?.Prompt ?? string.Empty;
                    this.property.Text = (curEditingStep as TextInput)?.Property ?? string.Empty;
                    break;

            }
        }

        private async void InkCanvas_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var inchToMillimeterFactor = 25.4f;
            List<Point> points = new List<Point>();
            var scalingX = DpiX / inchToMillimeterFactor;
            var scalingY = DpiY / inchToMillimeterFactor;
            var xCord = e.GetPosition(inkCanvas).X / scalingX;
            var yCord = e.GetPosition(inkCanvas).Y / scalingY;
            var step = GetRelatedStep((float)xCord, (float)yCord);
            if (step != null)
            {
                curEditingStep = step;
                var fly = this.myFlyout;

                SetForm();
                
                this.Type.SelectedIndex = TypeToIndex[curEditingStep.Type];
                var options = new Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions()
                {
                    Position = e.GetPosition(sender as UIElement)
                };
                fly.ShowAt(sender as FrameworkElement, options);
            }
            else
            {
                // should say something?
            }
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
                debugCanvas.Children.Add(GetPolyLocationTextBlock(shape));
            }
        }

        private TextBlock GetPolyLocationTextBlock(DrawsomeShape shape)
        {
            TextBlock text = new TextBlock();
            var inchToMillimeterFactor = 25.4f;
            List<Point> points = new List<Point>();
            var scalingX = DpiX / inchToMillimeterFactor;
            var scalingY = DpiY / inchToMillimeterFactor;

            Canvas.SetTop(text, shape.RecogUnit.BoundingRect.TopY * scalingY);
            Canvas.SetLeft(text, shape.RecogUnit.BoundingRect.TopX * scalingX);

            text.Text = string.Format("({0},{1})", shape.RecogUnit.BoundingRect.TopX, shape.RecogUnit.BoundingRect.TopY);
            text.FontSize = 20;

            return text;
        }

        private ComposerStep FindStepInBotInstance(List<ComposerStep> steps, Contoso.NoteTaker.JSON.Format.Rectangle rect)
        {
            foreach (var step in steps)
            {
                if (step.RelatedShape.RecogUnit.BoundingRect.OverlapSize(rect) > 10)
                {
                    return step;
                }
                else if (step is IfCondition)
                {
                    return FindStepInBotInstance((step as IfCondition).Steps, rect) ?? FindStepInBotInstance((step as IfCondition).ElseSteps, rect);
                }
            }
            return null;
        }

        private ComposerStep GetRelatedStep(float x, float y, float size = 10)
        {
            var rect = new Contoso.NoteTaker.JSON.Format.Rectangle();
            rect.TopX = x - size / 2;
            rect.TopY = y - size / 2;
            rect.Width = size;
            rect.Height = size;
            return FindStepInBotInstance(botInstance.Steps, rect);
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

        private async void Analyze_Click(object sender, RoutedEventArgs e)
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
                            DrawStorkes(pic);
                            var composerBot = await BotGenerator.Parse(pic);
                            this.botInstance = composerBot;
                            this.gButton.Visibility = Visibility.Visible;
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

        private void Edit_Clicked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
                }
                else
                {
                    inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
                }
            }
        }

        private void Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            curEditingStep.Type = e.AddedItems[0].ToString();
        }

        private List<ComposerStep> FindInBotInstanceAndGenerateNew(List<ComposerStep> steps, ComposerStep stepToFind)
        {
            var result = new List<ComposerStep>();

            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i].RelatedShape.Equals(stepToFind.RelatedShape))
                {
                    var stepToReplace = steps[i];
                    var newStep = Replace(stepToReplace);
                    result.Add(newStep);
                }
                else if (steps[i].Type == "Microsoft.IfCondition")
                {
                    var ifStep = steps[i] as IfCondition;
                    var newIfStep = new IfCondition(ifStep.Condition, ifStep.RelatedShape);
                    newIfStep.Steps.AddRange(FindInBotInstanceAndGenerateNew(ifStep.Steps, stepToFind));
                    newIfStep.ElseSteps.AddRange(FindInBotInstanceAndGenerateNew(ifStep.ElseSteps, stepToFind));
                    result.Add(newIfStep);
                }
                else
                {
                    result.Add(steps[i]);
                }
            }
            return result;
        }

        private ComposerStep Replace(ComposerStep oldStep)
        {
            switch (curEditingStep.Type)
            {
                case "Microsoft.SendActivity":
                default:
                    return new SendActivity(this.activity.Text, curEditingStep.RelatedShape);
                case "Microsoft.IfCondition":
                    if (oldStep is IfCondition)
                    {
                        (oldStep as IfCondition).Condition = this.condition.Text;
                    }
                    return oldStep;
                    
                case "Microsoft.SetProperty":
                    return new SetProperty(this.property.Text + "=" + this.value.Text, curEditingStep.RelatedShape);
                case "Microsoft.HttpRequest":
                    return new HttpRequest(this.url.Text, this.method.Text, this.body.Text, curEditingStep.RelatedShape);
                case "Microsoft.TextInput":
                    return new TextInput(this.prompt.Text, this.property.Text, curEditingStep.RelatedShape);
                
            }
        }

        private void MyFlyout_Closed(object sender, object e)
        {
            if (curEditingStep != null)
            {
                // ComposerStep oldStep = null;
                this.botInstance.Steps = FindInBotInstanceAndGenerateNew(botInstance.Steps, curEditingStep);
            }

        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var composerBot = this.botInstance;
                var composerJson = JsonConvert.SerializeObject(composerBot, Formatting.Indented);
                using (HttpClient client = new HttpClient())
                {
                    var content = new StringContent(composerJson, System.Text.Encoding.UTF8, "application/json");
                    var result = await client.PostAsync("http://localhost:5000/api/hack/drawsome", content);
                    string resultContent = await result.Content.ReadAsStringAsync();

                    var messageDialog = new MessageDialog(resultContent);
                    await messageDialog.ShowAsync();
                }
                //FileSavePicker picker = new FileSavePicker();
                //picker.FileTypeChoices.Add("file style", new string[] { ".txt", ".dialog" });
                //picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                //picker.SuggestedFileName = "Main.dialog";
                //StorageFile file = await picker.PickSaveFileAsync();

                //if (file != null)
                //{
                //    await FileIO.WriteTextAsync(file, composerJson);
                //}
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message);
                await messageDialog.ShowAsync();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                if (currentStrokes.Count > 0)
                {
                    FileSavePicker savePicker = new FileSavePicker();
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    savePicker.FileTypeChoices.Add(
                        "GIF with embedded ISF",
                        new List<string>() { ".gif" });
                    savePicker.DefaultFileExtension = ".gif";
                    savePicker.SuggestedFileName = "InkSample";

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        Windows.Storage.CachedFileManager.DeferUpdates(file);
                        IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                        using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                        {
                            await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                            await outputStream.FlushAsync();
                        }
                        stream.Dispose();

                        Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

                        if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                        {
                            var messageDialog = new MessageDialog("Save Success!");
                            await messageDialog.ShowAsync();
                        }
                        else
                        {
                            var messageDialog = new MessageDialog("Save Failed");
                            await messageDialog.ShowAsync();
                        }
                    }
                    else
                    {
                        var messageDialog = new MessageDialog("No strokes to save.");
                        await messageDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message);
                await messageDialog.ShowAsync();
            }
        }

        private void ClearCanvas()
        {
            this.recogCanvas.Children.Clear();
            this.debugCanvas.Children.Clear();
            this.inkRecognizer.ClearStrokes();
            this.inkCanvas.InkPresenter.StrokeContainer.Clear();
            this.botInstance = null;
            this.curEditingStep = null;
            this.gButton.Visibility = Visibility.Collapsed;
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearCanvas();
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".gif");

                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    Windows.Storage.CachedFileManager.DeferUpdates(file);
                    IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                    using (IInputStream intputStream = stream.GetInputStreamAt(0))
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(intputStream);
                    }
                    stream.Dispose();

                    Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

                    if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                    {
                        foreach (var stroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
                        {
                            this.inkRecognizer.AddStroke(stroke);
                        }

                        var messageDialog = new MessageDialog("Load Success!");
                        await messageDialog.ShowAsync();
                    }
                    else
                    {
                        var messageDialog = new MessageDialog("Load Failed");
                        await messageDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var messageDialog = new MessageDialog(ex.Message);
                await messageDialog.ShowAsync();
            }


        }
    }
}
