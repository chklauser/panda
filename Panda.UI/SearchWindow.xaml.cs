using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using JetBrains.Annotations;

namespace Panda.UI
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window, INotifyPropertyChanged
    {
        private string _searchString;
        private readonly VirtualDirectory _parentNode;
        private VirtualNode _selectedNode;
        private readonly ObservableCollection<VirtualNode> _searchResults = new ObservableCollection<VirtualNode>();
        private bool _isRecursive = true;
        private bool _isRegularExpression;
        private bool _isCaseSensitive = true;

        public SearchWindow(VirtualDirectory parentNode)
        {
            DataContext = this;
            _parentNode = parentNode;
            InitializeComponent();
        }

        public VirtualNode SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                _selectedNode = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<VirtualNode> SearchResults
        {
            get { return _searchResults; }
        }

        public string SearchString
        {
            get { return _searchString; }
            set 
            { 
                _searchString = value;
                OnPropertyChanged();
            }
        }

        public bool IsCaseSensitive
        {
            get { return _isCaseSensitive; }
            set
            {
                _isCaseSensitive = value;
                OnPropertyChanged();
            }
        }

        public bool IsRegularExpression
        {
            get { return _isRegularExpression; }
            set
            {
                _isRegularExpression = value;
                OnPropertyChanged();
            }
        }

        public bool IsRecursive
        {
            get { return _isRecursive; }
            set
            {
                _isRecursive = value;
                OnPropertyChanged();
            }
        }

        private void _searchButtonClick(object sender, RoutedEventArgs e)
        {
            // a new search was started, so clear the results
            SearchResults.Clear();

            var needle = SearchString;
            if(needle == null)
                return;

            var recursive = IsRecursive;

            var caseSens = IsCaseSensitive;

            var regex = IsRegularExpression;

            var hayStack = new Stack<VirtualDirectory>();

            hayStack.Push(_parentNode);

            while (hayStack.Count > 0)
            {
                // take one element from stack
                var currentElement = hayStack.Pop();
                if (caseSens)
                {
                    if (regex)
                    {
                        foreach (var vn in currentElement)
                        {
                            Match match;
                            try
                            {
                                match = Regex.Match(vn.Name, needle);
                            }
                            catch (ArgumentException)
                            {
                                MessageBox.Show("Syntax error in RegEx!", "Da Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            if (match.Success)
                            {
                                SearchResults.Add(vn);
                            }
                        }
                    }
                    else
                    {
                        // if case sensitive use TryGetNode
                        VirtualNode bla;
                        if (currentElement.TryGetNode(needle, out bla))
                        {
                            SearchResults.Add(bla);
                        }
                    }
                }
                else
                {
                    if (regex)
                    {
                        foreach (var vn in currentElement)
                        {
                            Match match;
                            try
                            {
                                match = Regex.Match(vn.Name, needle, RegexOptions.IgnoreCase);
                            }
                            catch (ArgumentException)
                            {
                                MessageBox.Show("Syntax error in RegEx!", "Da Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            if (match.Success)
                            {
                                SearchResults.Add(vn);
                            }
                        }
                    }
                    else
                    {
                        // if case insensitive do own foreach
                        foreach (var vn in currentElement)
                        {
                            if (vn.Name.ToLower() == needle.ToLower())
                            {
                                SearchResults.Add(vn);
                            }
                        }
                    }

                }
                if (recursive)
                {
                    // if recursive add every subdirectory to stack
                    foreach (var vn in currentElement)
                    {
                        var vd = vn as VirtualDirectory;
                        if (vd != null)
                        {
                            hayStack.Push(vd);
                        }
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");
            
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void _doubleClickedResult(object sender, MouseButtonEventArgs e)
        {
            if (SelectedNode != null)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
