using System.Collections.Generic;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class ViewDetailDialog : Window
    {
        public class DetailItem
        {
            public string Label { get; set; }
            public string Value { get; set; }
        }

        public string Title { get; set; }
        public List<DetailItem> Details { get; set; }

        public ViewDetailDialog(string title, List<DetailItem> details)
        {
            InitializeComponent();
            Title = title;
            Details = details ?? new List<DetailItem>();
            DataContext = this;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

