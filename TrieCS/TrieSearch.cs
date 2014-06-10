using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
namespace TrieCS
{
    internal class ExampleContet
    {
        public string Name { get; set; }

        public string Content { get; set; }

        public static readonly ExampleContet[] Examples =
        {
            new ExampleContet
            {
                Name = "app list",
                Content = "YouJian Rili DianShi XiangCe Zoe YouTube Car ShiZhong HTCZhiNan LianXiRen PlayShangDian GuPiao BiaoGe ChaoJiYongHu WenDang Keep Seeder Sense6Toolbox XposedInstaller BaiDuNuoMi MeiTuan BaiDu BaiDuDiTu BaiDuLvYou BaiDuTieBa BiYingCiDian BoHao BuKaManHua ChiZi CunQian FanYi XiaMiYinYue"
            },
            new ExampleContet
            {
                Name = "blank",
                Content = ""
            }
        };
    }

    public class PerfInfo
    {
        public double FilterAddMs { get; internal set; }
        public double ConvertMs { get; internal set; }
        public double TotalMs { get; internal set; }
    }

    public class SearchResult : IComparable<SearchResult>
    {
        public int Index { get; set; }
        public SortedList<int, int> Positions { get; private set; }
        public int Priority { get; set; }

        public string Content { get; set; }


        public SearchResult()
        {
            Positions = new SortedList<int, int>();
        }

        public int CompareTo(SearchResult other)
        {
            return other.Priority.CompareTo(Priority);
        }
    }


    public class MatchInfo
    {
        public int Index { get; set; }
        public int Position { get; set; }
        public int Priority { get; set; }
        public char Data { get; set; }

        public override string ToString()
        {
            return String.Format("[{0}.{1}]", Index, Position);
        }
    }

    class Node
    {
        public char Data { get; set; }
        public List<MatchInfo> Infos { get; private set; }
        public Dictionary<char, Node> Next { get; private set; }
        public Node()
        {
            Next = new Dictionary<char, Node>(26);
            Infos = new List<MatchInfo>();
        }
        public override string ToString()
        {
            return Data.ToString(CultureInfo.InvariantCulture);
        }
    }

    class TrieSearch
    {
        const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

        private readonly HiPerfTimer _timer = new HiPerfTimer();

        public PerfInfo LastPerfInfo { get; private set; }

        public Node Root { get; private set; }

        private string[] _keywords;
        public string[] Keywords
        {
            get { return _keywords; }
            set
            {
                _keywords = value;
                Root = new Node();

                for (var i = 0; i < _keywords.Length; i++)
                {
                    BuildTree(Root, _keywords[i], i);
                }
            }
        }

        private static void BuildTree(Node root, string keyword, int index)
        {
            var parents = new Stack<Node>();
            var node = root;
            for (var j = 0; j < keyword.Length; j++)
            {
                parents.Push(node);

                var c = keyword[j];

                var info = new MatchInfo
                {
                    Index = index,
                    Position = j,
                    Priority = j == 0 ? 2 : 1,
                    Data = c,
                };

                if (Char.IsUpper(c))
                    info.Priority += 1;

                c = Char.ToLower(c);

                Node newNode;
                if (!node.Next.TryGetValue(c, out newNode))
                {
                    newNode = new Node { Data = c };
                    node.Next[c] = newNode;
                }
                newNode.Infos.Add(info);

                node = newNode;
            }
        }

        public SearchResult[] Search(string data)
        {
            LastPerfInfo = new PerfInfo();
            var infoList = new Dictionary<int, SearchResult>();
            SearchIter(data, 0, Root, null, infoList);
            foreach (var item in infoList.Values.Where(item => Continues(item.Positions.Values)))
            {
                item.Priority += 2;
            }
            return infoList.Values.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static bool Continues(IEnumerable<int> list)
        {
            var last = list.ElementAt(0);
            foreach (var item in list.Skip(1))
            {
                if (item != last + 1)
                    return false;
                last = item;
            }
            return true;
        }

        private bool SearchIter(string data, int pos, Node node, ILookup<int, int> range, IDictionary<int, SearchResult> infoList)
        {
            if (node == null) return false;

            var result = false;

            for (; pos < data.Length; pos++)
            {
                var c = data[pos];
                var lastNode = node;

                result = node.Next.TryGetValue(c, out node);

                if (result)
                {
                    node.Infos.ForEach(info =>
                    {
                        var index = info.Index;
                        SearchResult sr;
                        if (!infoList.TryGetValue(info.Index, out sr))
                        {
                            sr = new SearchResult();
                            infoList[index] = sr;
                        }
                        sr.Content = Keywords[index];
                        if (!sr.Positions.ContainsKey(info.Position))
                        {
                            sr.Positions.Add(info.Position, info.Position);
                            sr.Priority += info.Priority;
                        }

                    });
                }
                else
                {
                    foreach (var item in lastNode.Infos)
                    {
                        infoList.Remove(item.Index);
                    }

                    node = lastNode;
                }

                Node nextSearchNode = null;

                result = Alphabet.Where(nc => nc != c).Where(nc => lastNode.Next.TryGetValue(nc, out nextSearchNode) && (range == null || nextSearchNode.Infos.Any(_ => range.Contains(_.Index))))
                    .Select(nc => SearchIter(data, pos, nextSearchNode, range, infoList))
                    .Aggregate(result, (current, newResult) => current || newResult);

                range = infoList.Select(_ => _.Key).ToLookup(_ => _);

                if (result) continue;

                break;
            }
            return result;
        }
    }
}
// ReSharper restore MemberCanBePrivate.Global
// ReSharper restore UnusedAutoPropertyAccessor.Global