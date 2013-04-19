using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace Panda.UI
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private string searchString_;
        private readonly VirtualDirectory parentNode_;
        public string clickedPath;
        private ObservableCollection<string> searchResults_;

        public SearchWindow(VirtualDirectory parentNode)
        {
            InitializeComponent();
            searchTextBox.DataContext = searchString_;
            parentNode_ = parentNode;
            searchResults_ = new ObservableCollection<string>();
        }

        private void _searchButtonClick(object sender, RoutedEventArgs e)
        {
            // a new search was started, so clear the results
            searchResults_.Clear();

            // TODO searchString is not updated, still null, so I experiment with a constant string
            string needle = ".*B";

            // TODO if recursively is set
            Boolean recursive = true;

            // TODO if case sensitive is set
            Boolean caseSens = false;

            // TODO if regex is set
            Boolean regex = true;

            var hayStack = new Stack<VirtualDirectory>();

            hayStack.Push(parentNode_);

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
                            Match match = null;
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
                                searchResults_.Add(vn.FullName);
                            }
                        }
                    }
                    else
                    {
                        // if case sensitive use TryGetNode
                        VirtualNode bla;
                        if (currentElement.TryGetNode(needle, out bla))
                        {
                            searchResults_.Add(bla.FullName);
                        }
                    }
                }
                else
                {
                    if (regex)
                    {
                        foreach (var vn in currentElement)
                        {
                            Match match = null;
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
                                searchResults_.Add(vn.FullName);
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
                                searchResults_.Add(vn.FullName);
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
    }
}
