using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UmbracoDiff.Entities;
using UmbracoDiff.Helpers;

namespace UmbracoCompare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CmsNodeHelper _left;
        private CmsNodeHelper _right;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private readonly ObservableCollection<CmsNode> _dataTypesOnlyLeft = new ObservableCollection<CmsNode>();
        private readonly ObservableCollection<CmsNode> _dataTypesOnlyRight = new ObservableCollection<CmsNode>();

        private readonly ObservableCollection<CmsNode> _docTypesOnlyLeft = new ObservableCollection<CmsNode>();
        private readonly ObservableCollection<CmsNode> _docTypesOnlyRight = new ObservableCollection<CmsNode>();

        private readonly ObservableCollection<string> _templatesOnlyLeft = new ObservableCollection<string>();
        private readonly ObservableCollection<string> _templatesOnlyRight = new ObservableCollection<string>();

        private readonly ObservableCollection<MismatchedDocTypeItemViewModel> _mismatchedProperties = new ObservableCollection<MismatchedDocTypeItemViewModel>();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _left = new CmsNodeHelper(ConnectionString1TextBox.Text);
            _right = new CmsNodeHelper(ConnectionString2TextBox.Text);

            CompareAndBindDataTypes();
            CompareAndBindDocTypes();
            CompareAndBindTemplates();
        }

        private void CompareAndBindDataTypes()
        {

            _dataTypesOnlyLeft.Clear();
            _dataTypesOnlyRight.Clear();

            var l = _left.GetAllDataTypes();
            var r = _right.GetAllDataTypes();

            CompareAndFillCmsNodes(l, r, _dataTypesOnlyLeft, _dataTypesOnlyRight);

            OutputDataTypesLeft.ItemsSource = _dataTypesOnlyLeft;
            OutputDataTypesRight.ItemsSource = _dataTypesOnlyRight;
        }

        private void CompareAndBindDocTypes()
        {
            _docTypesOnlyLeft.Clear();
            _docTypesOnlyRight.Clear();

            var leftData = _left.GetAllDocTypes().ToList();
            var rightData = _right.GetAllDocTypes().ToList();

            CompareAndFillCmsNodes(leftData, rightData, _docTypesOnlyLeft, _docTypesOnlyRight);

            CompareAndBindMismatchedDocTypes(leftData.ToList(), rightData.ToList());

            OutputDocTypesLeft.ItemsSource =    _docTypesOnlyLeft;
            OutputDocTypesRight.ItemsSource =   _docTypesOnlyRight;
        }

        private void CompareAndBindMismatchedDocTypes(IEnumerable<DocType> left, IEnumerable<DocType> right)
        {
            _mismatchedProperties.Clear();

            var rightDocTypes = right.ToList();
            foreach (var docTypeLeft in left)
            {
                var docTypeRight = rightDocTypes.FirstOrDefault(x => x.Text == docTypeLeft.Text);
                if (docTypeRight != null)
                {
                    if (!docTypeLeft.PropertiesAreEqual(docTypeRight))
                    {
                        _mismatchedProperties.Add(new MismatchedDocTypeItemViewModel {Left = docTypeLeft, Right = docTypeRight});
                    }
                }
            }
            OutputDocTypesMismatched.ItemsSource = _mismatchedProperties;
        }

        private void CompareAndBindTemplates()
        {
            _templatesOnlyLeft.Clear();
            _templatesOnlyRight.Clear();

            var leftData =  _left.GetAllTemplates(ConnectionString1TextBox.Text);
            var rightData = _right.GetAllTemplates(ConnectionString2TextBox.Text);

            CompareAndFill(leftData, rightData, _templatesOnlyLeft, _templatesOnlyRight);

            OutputTemplatesLeft.ItemsSource =   _templatesOnlyLeft;
            OutputTemplatesRight.ItemsSource =  _templatesOnlyRight;
        }

        private void CompareAndFill(string[] leftData,
                              string[] rightData,
                              ObservableCollection<string> leftOutput,
                              ObservableCollection<string> rightOutput)
        {
            foreach (var item in leftData)
            {
                if (!rightData.Contains(item))
                {
                    leftOutput.Add(item);
                }
            }

            foreach (var item in rightData)
            {
                if (!leftData.Contains(item))
                {
                    rightOutput.Add(item);
                }
            }
        }

        private void CompareAndFillCmsNodes(IEnumerable<CmsNode> leftData,
                              IEnumerable<CmsNode> rightData,
                              ObservableCollection<CmsNode> leftOutput,
                              ObservableCollection<CmsNode> rightOutput)
        {
            foreach (var item in leftData)
            {
                if (rightData.All(x => x.Text != item.Text))
                {
                    leftOutput.Add(item);
                }
            }

            foreach (var item in rightData)
            {
                if (leftData.All(x => x.Text != item.Text))
                {
                    rightOutput.Add(item);
                }
            }
        }

        public class MismatchedDocTypeItemViewModel
        {
            public string Name { get { return Left.Text; } }
            public DocType Left { get; set; }
            public DocType Right { get; set; }
        }

        public class PropertyViewModel
        {
            public PropertyType Left { get; set; }
            public PropertyType Right { get; set; }

            public bool AreEqual
            {
                get { return new PropertyComparer().Equals(Left, Right); }
            }
        }

        private void OutputDocTypesMismatched_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var dataItem = e.AddedCells.FirstOrDefault().Item as MismatchedDocTypeItemViewModel;
            if (dataItem != null)
            {
                var models = new List<PropertyViewModel>();
                foreach (var leftProp in dataItem.Left.Properties)
                {
                    PropertyType prop = leftProp;
                    var rightProp = dataItem.Right.Properties.FirstOrDefault(x => x.Alias == prop.Alias) ?? new PropertyType();
                    models.Add(new PropertyViewModel {Left = leftProp, Right = rightProp});
                }

                //Let's take what's left and add it to the bottom
                foreach (var rightProp in dataItem.Right.Properties.Where(x => dataItem.Right.Properties.All(y => x.Alias != y.Alias)))
                {
                    models.Add(new PropertyViewModel {Left = new PropertyType(), Right = rightProp});
                }

                OutputDocTypesMismatchedDetail.ItemsSource = models;
            }
        }

        private void OutputDocTypesMismatchedDetail_OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var dataItem = e.AddedCells.FirstOrDefault().Item as PropertyViewModel;
            if (dataItem != null)
            {
                var viewer = new PropertyViewer(dataItem);
                viewer.Show();
            }
        }

        private void OutputDocTypesMismatchedDetail_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var data = e.Row.DataContext as PropertyViewModel;
            if(data != null)
            {
                if (!data.AreEqual)
                {
                    e.Row.Background = new SolidColorBrush(Colors.Red);
                }
            }

        }
    }
}


