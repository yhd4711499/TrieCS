using System;
using System.Windows.Controls;

namespace TrieCS
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        readonly TrieSearch _trie = new TrieSearch();

        public MainWindow()
        {
            InitializeComponent();

            Initiate();
        }

        private void Initiate()
        {
            ExampleListBox.ItemsSource = ExampleContet.Examples;
        }

        private void content_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            _trie.Keywords = content.Text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Split(' ', '.');
            NodesTreeView.ItemsSource = _trie.Nodes;
            DisplayResults();
        }

        private void keyword_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            DisplayResults();
        }

        private void DisplayResults()
        {
            var results = _trie.Search(keyword.Text);

            time.Text = String.Format("{0}, Total words:{1}",
                _trie.LastPerfInfo,
                _trie.Keywords.Length);

            Array.Sort(results, (a, b) => b.Priority.CompareTo(a.Priority));
            result.ItemsSource = results;
        }
    }
}