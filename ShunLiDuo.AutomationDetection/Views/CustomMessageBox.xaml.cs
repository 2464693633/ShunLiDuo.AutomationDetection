using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShunLiDuo.AutomationDetection.Views
{
    public enum CustomMessageBoxType
    {
        Information,
        Warning,
        Error,
        Question
    }

    public enum CustomMessageBoxResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No
    }

    public partial class CustomMessageBox : Window
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(CustomMessageBox));

        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register("IconBrush", typeof(Brush), typeof(CustomMessageBox));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public Brush IconBrush
        {
            get => (Brush)GetValue(IconBrushProperty);
            set => SetValue(IconBrushProperty, value);
        }

        public CustomMessageBoxResult Result { get; private set; } = CustomMessageBoxResult.None;

        private CustomMessageBox(string title, string message, CustomMessageBoxType type, MessageBoxButton buttons)
        {
            InitializeComponent();
            Title = title;
            Message = message;
            
            // 设置图标和颜色
            switch (type)
            {
                case CustomMessageBoxType.Information:
                    IconBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    IconTextBlock.Text = "ℹ";
                    break;
                case CustomMessageBoxType.Warning:
                    IconBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                    IconTextBlock.Text = "⚠";
                    break;
                case CustomMessageBoxType.Error:
                    IconBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    IconTextBlock.Text = "✕";
                    break;
                case CustomMessageBoxType.Question:
                    IconBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    IconTextBlock.Text = "?";
                    break;
            }

            // 创建按钮
            CreateButtons(buttons);

            // 如果是确认对话框，不允许点击关闭按钮
            if (buttons == MessageBoxButton.YesNo)
            {
                CloseButton.Visibility = Visibility.Collapsed;
            }
        }

        private void CreateButtons(MessageBoxButton buttons)
        {
            ButtonPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("确定", CustomMessageBoxResult.OK, true);
                    break;
                case MessageBoxButton.OKCancel:
                    AddButton("取消", CustomMessageBoxResult.Cancel, false);
                    AddButton("确定", CustomMessageBoxResult.OK, true);
                    break;
                case MessageBoxButton.YesNo:
                    AddButton("否(N)", CustomMessageBoxResult.No, false);
                    AddButton("是(Y)", CustomMessageBoxResult.Yes, true);
                    break;
                case MessageBoxButton.YesNoCancel:
                    AddButton("取消", CustomMessageBoxResult.Cancel, false);
                    AddButton("否(N)", CustomMessageBoxResult.No, false);
                    AddButton("是(Y)", CustomMessageBoxResult.Yes, true);
                    break;
            }
        }

        private void AddButton(string content, CustomMessageBoxResult result, bool isPrimary)
        {
            var button = new Button
            {
                Content = content,
                MinWidth = 100,
                Height = 38,
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            if (isPrimary)
            {
                button.Style = (Style)FindResource("PrimaryButtonStyle");
            }
            else
            {
                button.Style = (Style)FindResource("SecondaryButtonStyle");
            }

            button.Click += (s, e) =>
            {
                Result = result;
                DialogResult = result == CustomMessageBoxResult.OK || result == CustomMessageBoxResult.Yes;
                Close();
            };

            ButtonPanel.Children.Add(button);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }

        // 静态方法用于显示对话框
        public static CustomMessageBoxResult Show(string message, string title = "提示", CustomMessageBoxType type = CustomMessageBoxType.Information, MessageBoxButton buttons = MessageBoxButton.OK)
        {
            var dialog = new CustomMessageBox(title, message, type, buttons)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
            return dialog.Result;
        }

        // 便捷方法
        public static CustomMessageBoxResult ShowInformation(string message, string title = "提示")
        {
            return Show(message, title, CustomMessageBoxType.Information, MessageBoxButton.OK);
        }

        public static CustomMessageBoxResult ShowWarning(string message, string title = "警告")
        {
            return Show(message, title, CustomMessageBoxType.Warning, MessageBoxButton.OK);
        }

        public static CustomMessageBoxResult ShowError(string message, string title = "错误")
        {
            return Show(message, title, CustomMessageBoxType.Error, MessageBoxButton.OK);
        }

        public static CustomMessageBoxResult ShowQuestion(string message, string title = "确认")
        {
            return Show(message, title, CustomMessageBoxType.Question, MessageBoxButton.YesNo);
        }
    }
}

