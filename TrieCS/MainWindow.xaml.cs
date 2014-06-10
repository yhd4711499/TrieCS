using System;
using System.Windows;
using System.Windows.Controls;

namespace TrieCS
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        TrieSearch _trie = new TrieSearch();

        HiPerfTimer _timer = new HiPerfTimer();

        public MainWindow()
        {
            InitializeComponent();

            Initiate();
        }

        public void Initiate()
        {
            ExampleListBox.ItemsSource = ExampleContet.Examples;
        }

        private void content_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            _trie.Keywords = content.Text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Split(' ');
            NodesTreeView.ItemsSource = _trie.Root.Next;
            DisplayResults();
        }

        private void keyword_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            DisplayResults();
        }

        private void DisplayResults()
        {
            _timer.Start();
            var results = _trie.Search(keyword.Text);
            Array.Sort(results, (a, b) => b.Priority.CompareTo(a.Priority));
            _timer.Stop();
            time.Text = String.Format("Total:{0:0.00000}ms, FilterAdd:{1:0.00000}ms, Convert:{2:0.00000}ms, Total words:{3}",
                _timer.Duration * 1000.0,
                _trie.LastPerfInfo.FilterAddMs,
                _trie.LastPerfInfo.ConvertMs,
                _trie.Keywords.Length);
            result.ItemsSource = results;
        }
    }
}