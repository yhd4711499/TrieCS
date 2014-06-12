using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PossibleMultipleEnumeration
namespace TrieCS
{
    class ExampleContet
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
                Name = "ins",
                Content = "s sin dins"
            },
            new ExampleContet
            {
                Name = "ab",
                Content = "abc dab"
            },
            new ExampleContet
            {
                Name = "blank",
                Content = ""
            }
        };
    }

    /// <summary>
    /// Performance info
    /// </summary>
    public class PerfInfo
    {
        public double FilterOutMs { get; internal set; }
        public double TotalMs { get; internal set; }

        public override string ToString()
        {
            return String.Format("Total:{0:0.00000}ms, FilterAdd:{1:0.00000}ms",
                TotalMs,
                FilterOutMs);
        }
    }

    /// <summary>
    /// Grouped result of all <see cref="MatchInfo"/> with the same index.
    /// </summary>
    public class SearchResult : IComparable<SearchResult>
    {
        /// <summary>
        /// index in the <see cref="TrieSearch.Keywords"/> of a <see cref="TrieSearch"/>
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// positions of matched chars in <see cref="Content"/>
        /// </summary>
        public SortedList<int, int> Positions { get; private set; }
        /// <summary>
        /// how excactly it matchs the query
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Content { get; set; }

        // ReSharper disable once UnusedMember.Global
        public string PositionString
        {
            get
            {
                return String.Join(",", Positions.Values);
            }
        }

        public SearchResult()
        {
            Positions = new SortedList<int, int>();
        }

        public int CompareTo(SearchResult other)
        {
            return other.Priority.CompareTo(Priority);
        }
    }

    /// <summary>
    /// Result of an single matched char
    /// </summary>
    class MatchInfo
    {
        /// <summary>
        /// index in the <see cref="TrieSearch.Keywords"/> of a <see cref="TrieSearch"/>
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// position the this char in a string
        /// </summary>
        public int Position { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// the char content
        /// </summary>
        public char Data { get; set; }

        public override string ToString()
        {
            return String.Format("[{0}.{1}]", Index, Position);
        }
    }

    /// <summary>
    /// Trie node
    /// </summary>
    class Node
    {
        /// <summary>
        /// Char content. Only for data visualization.
        /// </summary>
        public char Data { get; set; }
        /// <summary>
        /// All <see cref="MatchInfo"/> of this node.
        /// </summary>
        public List<MatchInfo> Infos { get; private set; }

        public Node()
        {
            Infos = new List<MatchInfo>();
        }
        public override string ToString()
        {
            return Data.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class TrieSearch
    {
        /// <summary>
        /// Performance info of the last search.
        /// </summary>
        public PerfInfo LastPerfInfo { get; private set; }

        /// <summary>
        /// All nodes in this trie.
        /// </summary>
        public Dictionary<char, Node> Nodes { get; private set; }

        private string[] _keywords;
        /// <summary>
        /// the contents from which you may want to search.
        /// <para/>setting this value will immidietly build the trie.
        /// </summary>
        public string[] Keywords
        {
            get { return _keywords; }
        }

        /// <summary>
        /// Build the trie. A simple async wrapper for <see cref="BuildTree"/>
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public Task BuildTreeAsync(string[] contents)
        {
            return Task.Run(() => BuildTree(contents));
        }

        /// <summary>
        /// Search. A simple async wrapper for <see cref="Search"/>
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Matched results</returns>
        public Task<SearchResult[]> SearchAsync(string query)
        {
            return Task.Run(() => Search(query));
        }

        /// <summary>
        /// build the tree.
        /// <param name="contents"></param>
        /// </summary>
        public void BuildTree(string[] contents)
        {
            GC.Collect();
            _keywords = contents;
            Nodes = new Dictionary<char, Node>();
            for (var i = 0; i < contents.Length; i++)
            {
                var keyword = contents[i];
                for (var j = 0; j < keyword.Length; j++)
                {
                    var c = keyword[j];
                    if (!Char.IsLetter(c)) continue;
                    var info = new MatchInfo
                    {
                        Index = i,
                        Position = j,
                        Priority = j == 0 ? 3 : 1,  // initial char has a higher priority
                        Data = c,
                    };

                    // Capital char has a higher priority.
                    if (Char.IsUpper(c))
                        info.Priority += 2;

                    // Only lower char is accepted.
                    c = Char.ToLower(c);

                    Node newNode;
                    if (!Nodes.TryGetValue(c, out newNode))
                    {
                        newNode = new Node { Data = c };
                        Nodes[c] = newNode;
                    }

                    // Record this match info.
                    newNode.Infos.Add(info);
                }
            }
        }

        /// <summary>
        /// Search
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Matched results</returns>
        public SearchResult[] Search(string query)
        {
            LastPerfInfo = new PerfInfo();

            if (Keywords.Length == 0)
                return new SearchResult[0];

            var timer = HiPerfTimer.StartNew();
            var lastMatchedPositions = new Dictionary<int, int>();
            var resultList = new Dictionary<int, SearchResult>();

            for (var pos = 0; pos < query.Length; pos++)
            {
                var c = query[pos];
                Node node;
                if (!Nodes.TryGetValue(c, out node))
                {
                    resultList.Clear();
                    break;
                }

                // Clear items in resultList which do not appears in node.
                FilterOut(resultList, node);

                // whether the result is matched in order.
                var isInOrder = new Dictionary<int, bool>();

                node.Infos.ForEach(info =>
                {
                    var index = info.Index;
                    if (pos == 0)
                    {
                        // This is the start point. Only the isInOrder of matched index will be set to true
                        // to initiate the searching range.
                        // Since that positions with the same index is added in asc order, I take
                        // the first one to add to lastMatchedPositions.
                        isInOrder[index] = true;
                        if (!lastMatchedPositions.ContainsKey(index))
                            lastMatchedPositions[index] = info.Position;
                    }
                    else
                    {
                        if (!resultList.ContainsKey(index))
                        {
                            // Not in the last searching range.
                            resultList.Remove(index);
                            return;
                        }
                        if (isInOrder.ContainsKey(index) && isInOrder[index])
                        {
                            // Skip duplicate matched index.
                            return;
                        }
                        if (info.Position > lastMatchedPositions[index])
                        {
                            // This matched char is behind the last matched char, which means it's in order.
                            // Update lastMatchedPositions for next matching.
                            isInOrder[index] = true;
                            lastMatchedPositions[index] = info.Position;
                        }
                        else
                        {
                            // Not in order, which means this matched char is in front of the
                            // last matched char. So mark it "false".
                            isInOrder[index] = false;
                        }
                    }
                });

                // Add to result list if in order.
                // Otherwise remove from list.
                foreach (var info in node.Infos)
                {
                    bool inOrder;
                    if (isInOrder.TryGetValue(info.Index, out inOrder) && inOrder)
                    {
                        AddToResult(resultList, info);
                    }
                    else
                    {
                        resultList.Remove(info.Index);
                    }
                }
            }

            // other procedures
            var resultArray = PostProcess(resultList);

            timer.Stop();
            LastPerfInfo.TotalMs = timer.Duration;

            return resultArray;
        }

        /// <summary>
        /// Remove items, which not appears in node.Infos, in resultList
        /// </summary>
        /// <param name="resultList"></param>
        /// <param name="node"></param>
        private static void FilterOut(Dictionary<int, SearchResult> resultList, Node node)
        {
            var lookup = node.Infos.ToLookup(_ => _.Index);
            var dif = resultList.Select(_ => _.Key).Where(_ => !lookup.Contains(_)).ToList();
            dif.ForEach(_ => resultList.Remove(_));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultList"></param>
        /// <returns></returns>
        private static SearchResult[] PostProcess(Dictionary<int, SearchResult> resultList)
        {
            var resultArray = resultList.Values.ToArray();
            foreach (var item in resultArray)
            {
                // increase priority if continues.
                if (Continues(item.Positions.Values))
                    item.Priority += 4;
            }
            return resultArray;
        }

        /// <summary>
        /// Add <see cref="MatchInfo"/> to resultList
        /// </summary>
        /// <param name="resultList"></param>
        /// <param name="info"></param>
        private void AddToResult(Dictionary<int, SearchResult> resultList, MatchInfo info)
        {
            var index = info.Index;
            SearchResult sr;
            if (!resultList.TryGetValue(info.Index, out sr))
            {
                sr = new SearchResult
                {
                    Index = index,
                    Content = Keywords[index]
                };
                resultList[index] = sr;
            }
            if (sr.Positions.ContainsKey(info.Position)) return;
            sr.Positions.Add(info.Position, info.Position);
            if (info.Priority > sr.Priority)
                sr.Priority = info.Priority;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static bool Continues(IEnumerable<int> list)
        {
            var last = list.ElementAt(0);
            var threshold = 3;
            foreach (var item in list.Skip(1))
            {
                if (item != last + 1)
                {
                    threshold = 3;
                    last = item;
                    continue;
                }
                last = item;
                threshold--;
                if (threshold == 0)
                    return true;
            }
            return false;
        }
    }
}
// ReSharper restore PossibleMultipleEnumeration
// ReSharper restore MemberCanBePrivate.Global
// ReSharper restore UnusedAutoPropertyAccessor.Global