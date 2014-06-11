using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

    class Node
    {
        public char Data { get; set; }
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
        public PerfInfo LastPerfInfo { get; private set; }
        public Dictionary<char, Node> Nodes { get; private set; }

/*
        Dictionary<int, int> _lastPosition;
*/

        private string[] _keywords;
        /// <summary>
        /// the contents from which you may want to search.
        /// <para/>setting this value will immidietly build the trie.
        /// </summary>
        public string[] Keywords
        {
            get { return _keywords; }
            set
            {
                _keywords = value;
                Nodes = new Dictionary<char, Node>();

                for (var i = 0; i < _keywords.Length; i++)
                {
                    BuildTree(_keywords[i], i);
                }
            }
        }

        /// <summary>
        /// build the tree.
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="index">index of the keyword in <see cref="Keywords"/></param>
        private void BuildTree(string keyword, int index)
        {
            for (var j = 0; j < keyword.Length; j++)
            {
                var c = keyword[j];
                if (!Char.IsLetter(c)) continue;
                var info = new MatchInfo
                {
                    Index = index,
                    Position = j,
                    Priority = j == 0 ? 3 : 1,  // start char has a higher priority
                    Data = c,
                };

                if (Char.IsUpper(c))
                    info.Priority += 2;

                c = Char.ToLower(c);

                Node newNode;
                if (!Nodes.TryGetValue(c, out newNode))
                {
                    newNode = new Node { Data = c };

                    Nodes[c] = newNode;
                }
                newNode.Infos.Add(info);
            }
        }

        public SearchResult[] Search(string query)
        {
            LastPerfInfo = new PerfInfo();
            //_lastPosition = new Dictionary<int, int>();

            if (Keywords.Length == 0)
                return new SearchResult[0];

            var timer = HiPerfTimer.StartNew();
            var resultList = new Dictionary<int, SearchResult>();
            //var found = new Dictionary<int, bool>();

            for (int pos = 0; pos < query.Length; pos++)
            {
                var c = query[pos];
                Node node;
                if (!Nodes.TryGetValue(c, out node))
                {
                    resultList.Clear();
                    break;
                }

                FilterOut(resultList, node);

                node.Infos.ForEach(info =>
                {
                    var index = info.Index;
                    if (pos != 0)
                    {
                        if (!resultList.ContainsKey(index))
                        {
                            resultList.Remove(index);
                            return;
                        }
                        /*else
                        {
                            if (info.Position > _lastPosition[index])
                            {
                                found[index] = true;
                                _lastPosition[index] = info.Position;
                            }
                            else
                            {
                                found[index] = false;
                            }
                        }*/
                    }
                    /*else
                    {
                        if (!_lastPosition.ContainsKey(index))
                            _lastPosition[index] = info.Position;
                        found[index] = true;
                    }*/
                    AddToResult(resultList, info, index);
                });
            }

            /*foreach (var item in found.Where(_=>!_.Value).Select(_=>_.Key))
            {
                resultList.Remove(item);
            }*/

            // increase priority if continues.
            var resultArray = PostProcess(resultList);

            timer.Stop();
            LastPerfInfo.TotalMs = timer.Duration;

            return resultArray;
        }

        private static void FilterOut(Dictionary<int, SearchResult> resultList, Node node)
        {
            var lookup = node.Infos.ToLookup(_ => _.Index);
            var dif = resultList.Select(_ => _.Key).Where(_ => !lookup.Contains(_)).ToList();
            dif.ForEach(_ => resultList.Remove(_));
        }

        private static SearchResult[] PostProcess(Dictionary<int, SearchResult> resultList)
        {
            var resultArray = resultList.Values.ToArray();
            foreach (var item in resultArray)
            {
                if (Continues(item.Positions.Values))
                    item.Priority += 4;
            }
            return resultArray;
        }

        private void AddToResult(Dictionary<int, SearchResult> resultList, MatchInfo info, int index)
        {
            SearchResult sr;
            if (!resultList.TryGetValue(info.Index, out sr))
            {
                sr = new SearchResult
                {
                    Index = index
                };
                resultList[index] = sr;
            }
            sr.Content = Keywords[index];
            if (!sr.Positions.ContainsKey(info.Position))
            {
                sr.Positions.Add(info.Position, info.Position);
                if (info.Priority > sr.Priority)
                    sr.Priority = info.Priority;
            }
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
            var found = false;
            foreach (var item in list.Skip(1))
            {
                if (item != last + 1)
                {
                    threshold = 3;
                    last = item;
                    found = false;
                    continue;
                }
                last = item;
                threshold--;
                found = true;
                if (threshold == 0)
                    return true;
            }
            return found;
        }
    }
}
// ReSharper restore PossibleMultipleEnumeration
// ReSharper restore MemberCanBePrivate.Global
// ReSharper restore UnusedAutoPropertyAccessor.Global