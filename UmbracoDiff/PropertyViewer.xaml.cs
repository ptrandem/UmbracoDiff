using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UmbracoDiff.Entities;

namespace UmbracoCompare
{
    /// <summary>
    /// Interaction logic for PropertyViewer.xaml
    /// </summary>
    public partial class PropertyViewer : Window
    {
        private readonly MainWindow.PropertyViewModel _model;
        public PropertyViewer(MainWindow.PropertyViewModel viewModel)
        {
            _model = viewModel;
            InitializeComponent();
            var properties = new List<PropertyType> {_model.Left, _model.Right};
            OutputPropertyCompare.ItemsSource = properties;
        }
    }
}
