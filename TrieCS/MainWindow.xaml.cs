using System;
using System.Threading.Tasks;
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

        private async void content_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            UpdateMsg("building...");
            var text = content.Text;
            var keywords = await Task.Run(() => text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Split(' ', '.'));
            await _trie.BuildTreeAsync(keywords);
            NodesTreeView.ItemsSource = _trie.Nodes;
            DisplayResults();
        }

        private void keyword_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            DisplayResults();
        }

        private async void DisplayResults()
        {
            UpdateMsg("searching...");
            var results = await _trie.SearchAsync(keyword.Text);

            UpdateMsg("{0}, Total words:{1}",
                _trie.LastPerfInfo,
                _trie.Keywords.Length);

            Array.Sort(results, (a, b) => b.Priority.CompareTo(a.Priority));
            result.ItemsSource = results;
        }

        private void UpdateMsg(string format, params object[] args)
        {
            time.Text = String.Format(format, args);
        }
    }
}