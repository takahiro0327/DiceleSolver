using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using static Solver;

public class Solver : MonoBehaviour
{
    [SerializeField]
    GameObject m_Box;

    public struct DiceSurface
    {
        public int num;
        public int x, y;

        public (int, int) coord => (x, y);

        public override int GetHashCode()
        {
            return num * 100 + x * 10 + y;
        }

        public override bool Equals(object obj)
        {
            if( obj is DiceSurface rhs )
            {
                return rhs.x == x && rhs.y == y && rhs.num == num;
            }

            return false;
        }
    }

    public class Board
    {
        public int[,] numbers = new int[5, 5];
        public int[] columns = new int[5];
        public int[] rows = new int[5];
        public bool[,] greens = new bool[5, 5];

        //public int[] whiteColumns = new int[5];
        //public int[] whiteRows = new int[5];

        public int[,] actived = new int[5, 5];

        public HashSet<DiceSurface> yellows = new HashSet<DiceSurface>();

        public HashSet<string> answerTexts = new HashSet<string>();

        public List<int[,]> answers = new List<int[,]>();

        public Board()
        {
            for( int x = 0; x < 5; ++x )
            {
                for( int y = 0; y < 5; ++y )
                {
                    actived[x, y] = (1 << 7) - 1;
                }
            }
        }

        public int this[ (int,int) coord ]
        {
            get
            {
                return numbers[coord.Item1, coord.Item2];
            }

            set
            {
                numbers[coord.Item1, coord.Item2] = value;
            }
        }

        public int CalcCandiates()
        {
            List<(int, int)> coords = new List<(int, int)>();
            int[] candiates = new int[7];

            List<int> restoreValues = new List<int>();

            for( int x = 0; x < 5; ++x )
            {
                for( int y = 0; y < 5; ++y )
                {
                    if (greens[x, y] || ((x & 1) != 0 && (y & 1) != 0))
                        continue;

                    int n = numbers[x, y];

                    actived[x, y] &= ~(1 << n);

                    coords.Add((x, y));
                    
                    numbers[x, y] = 0;
                    ++candiates[n];
                    restoreValues.Add(n);
                }
            }

            int ret = _CalcCandiates(coords, 0, candiates);

            for ( int i = 0; i < coords.Count; ++i )
            {
                this[coords[i]] = restoreValues[i];
            }

            return ret;
        }

        private int _CalcCandiates( List<(int,int)> coords, int depth, int[] candiates )
        {
            if (!IsCorrect())
                return 0;

            if (coords.Count == depth)
            {
                foreach( var yellow in yellows )
                {
                    bool ok = false;

                    for( int i = 0; i < 5; ++i )
                    {
                        if( !greens[i, yellow.y] && numbers[i,yellow.y] == yellow.num ||
                            !greens[yellow.x, i] && numbers[yellow.x,i] == yellow.num 
                            )
                        {
                            ok = true;
                            break;
                        }
                    }

                    if (!ok)
                        return 0;
                }


                StringBuilder builder = new StringBuilder();
                for( int y = 0; y < 5; ++y )
                {
                    for( int x = 0; x < 5; ++x )
                    {
                        builder.Append(numbers[x, y]);
                        builder.Append(',');
                    }

                    builder.AppendLine();
                }

                string answer = builder.ToString();

                if( answerTexts.Add(answer) )
                {
                    int[,] copy = new int[5, 5];
                    System.Array.Copy(numbers, copy, numbers.Length);
                    answers.Add(copy);

                    Debug.Log(answer);
                }
                    

                return 1;
            }
                

            int ans = 0;

            for( int x = 1; x <= 6; ++x )
            {
                if (candiates[x] <= 0)
                    continue;

                --candiates[x];                
                this[coords[depth]] = x;

                ans += _CalcCandiates(coords, depth + 1, candiates);

                this[coords[depth]] = 0;
                ++candiates[x];
            }

            return ans;
        }

        public bool IsCorrect()
        {
            for( int s = 0; s < 5; ++s )
            {
                int sum = 0;
                int unknowns = 0;

                for( int i = 0; i < 5; ++i )
                {
                    if ((s & 1) != 0 && (i & 1) != 0)
                        continue;

                    int n = numbers[i, s];
                    int bit = 1 << n;

                    if ( !greens[i,s] && (actived[i, s] & bit) == 0)
                        return false;

                    sum += n;

                    if (n <= 0)
                        ++unknowns;
                }

                if (rows[s] < unknowns + sum || unknowns * 6 + sum < rows[s])
                    return false;

                sum = 0;
                unknowns = 0;

                for (int i = 0; i < 5; ++i)
                {
                    if ((s & 1) != 0 && (i & 1) != 0)
                        continue;

                    int n = numbers[s, i];
                    int bit = 1 << n;

                    if (!greens[s, i] && (actived[s, i] & bit) == 0)
                        return false;

                    sum += n;

                    if (n <= 0)
                        ++unknowns;
                }

                if (columns[s] < unknowns + sum || unknowns * 6 + sum < columns[s])
                    return false;
            }

            return true;
        }
    }

    Dice[,] m_Dices = new Dice[6, 6];
    Board m_Board = new Board();

    public void Reset()
    {
        m_Board = new Board();

        for (int x = 0; x < 6; ++x)
        {
            for (int y = 0; y < 6; ++y)
            {
                var dice = m_Dices[x, y];
                if (dice == null)
                    continue;

                dice.Selected = false;
            }
        }   
    }

    void Start()
    {
        m_Dices[0, 0] = m_Box.GetComponent<Dice>();

        for( int x = 0; x < 6; ++x )
        {
            for( int y = 0; y < 6; ++y ) 
            {
                if ((x & 1) != 0 && (y & 1) != 0 && (x < 5) && (y < 5))
                    continue;

                if ( x == 0 && y == 0 || (x == 5 && y == 5) )
                {
                    continue;
                }

                var box = Instantiate(m_Box);
                var baseTransform = (RectTransform)m_Box.transform;
                var transform = (RectTransform)box.transform;

                m_Dices[x, y] = box.GetComponent<Dice>();

                transform.parent = baseTransform.parent;
                var position = baseTransform.anchoredPosition;
                position.x += (transform.sizeDelta.x + 5f) * x;
                position.y -= (transform.sizeDelta.y + 5f) * y;
                transform.anchoredPosition = position;
            }
        }

        for (int x = 0; x < 6; ++x)
        {
            for (int y = 0; y < 6; ++y)
            {
                var dice = m_Dices[x, y];

                if (x >= 5)
                {
                    if (y >= 5)
                        continue;

                    dice.Number = 6;
                    dice.Color = Dice.ColorType.White;
                    dice.IsBig = true;
                    continue;
                }
                else if (y >= 5)
                {
                    dice.Number = 6;
                    dice.Color = Dice.ColorType.White;
                    dice.IsBig = true;
                    continue;
                }
            }
        }
                
        Reset();

        {
            int[,] numbers = new int[5, 5]
            {
                {4,3,3,4,5 },
                {3,0,1,0,5 },
                {4,6,5,6,6 },
                {2,0,4,0,2 },
                {1,1,3,1,2 },
            };

            int[,] green = new int[5, 5]
            {
                {0,0,0,0,0 },
                {0,0,1,0,0 },
                {1,0,0,0,0 },
                {1,0,1,0,0 },
                {0,0,0,0,0 },
            };

            int[,] white = new int[5, 5]
            {
                {0,0,0,0,0 },
                {0,0,0,0,0 },
                {0,0,1,1,1 },
                {0,0,0,0,0 },
                {1,0,0,0,0 },
            };

            int[] columns = { 17, 10, 18, 9, 17 };
            int[] rows = { 20, 8, 13, 11, 19 };

            for( int x = 0; x < 5; ++x )
            {
                for( int y = 0; y < 5; ++y)
                {
                    if (m_Dices[x, y] == null)
                        continue;

                    m_Dices[x, y].Number = numbers[y, x];

                    if (green[y, x] > 0)
                        m_Dices[x, y].Color = Dice.ColorType.Green;
                    else if (white[y, x] > 0)
                        m_Dices[x, y].Color = Dice.ColorType.White;
                    else
                        m_Dices[x, y].Color = Dice.ColorType.Yellow;
                }

                m_Dices[x, 5].Number = columns[x];
                m_Dices[5, x].Number = rows[x];
            }
            
        }
    }

    void _GUIToBoard()
    {
        m_Board.yellows.Clear();
        m_Board.answers.Clear();
        m_Board.answerTexts.Clear();

        for (int x = 0; x < 5; ++x)
        {
            m_Board.columns[x] = m_Dices[x, 5].Number;
            m_Board.rows[x] = m_Dices[5, x].Number;

            for (int y = 0; y < 5; ++y)
            {
                if ((x & 1) != 0 && (y & 1) != 0)
                    continue;

                int n = m_Dices[x, y].Number;
                var color = m_Dices[x, y].Color;

                m_Board.numbers[x, y] = n;

                if( color == Dice.ColorType.Yellow )
                {
                    m_Board.yellows.Add( new DiceSurface() { num = n, x = x, y = y} );
                    m_Board.greens[x, y] = false;
                }
                else if( color == Dice.ColorType.White )
                {
                    m_Board.greens[x, y] = false;
                }
                else if( color == Dice.ColorType.Green )
                {
                    m_Board.greens[x, y] = true;
                }
            }
        }


        for (int x = 0; x < 5; ++x)
        {
            for (int y = 0; y < 5; ++y)
            {
                if ((x & 1) != 0 && (y & 1) != 0)
                    continue;

                if( m_Dices[x, y].Color == Dice.ColorType.White )
                {
                    int bit = ~(1 << m_Dices[x, y].Number);

                    for (int i = 0; i < 5; ++i)
                    {
                        if( !m_Board.greens[x, i] )
                            m_Board.actived[x, i] &= bit;

                        if (!m_Board.greens[i, y])
                            m_Board.actived[i, y] &= bit;
                    }
                }
            }
        }
    }

    void _UpdateYellowSurface()
    {
        if (m_Board.answers.Count != 1)
            return;

        var answer = m_Board.answers[0];

        for (int x = 0; x < 5; ++x)
        {
            for (int y = 0; y < 5; ++y)
            {
                if (m_Dices[x, y] == null)
                    continue;

                var color = m_Dices[x, y].Color;
                var n = m_Dices[x, y].Number;

                if (color == Dice.ColorType.Green)
                    continue;

                bool isYellow = false;

                for( int i = 0; i < 5; ++i )
                {
                    if (answer[x, i] == n && m_Dices[x, i] != null && m_Dices[x,i].Color != Dice.ColorType.Green)
                    {
                        isYellow = true;
                        break;
                    }

                    if (answer[i, y] == n && m_Dices[i, y] != null && m_Dices[i, y].Color != Dice.ColorType.Green)
                    {
                        isYellow = true;
                        break;
                    }
                }

                if( isYellow )
                {
                    m_Dices[x, y].Color = Dice.ColorType.Yellow;
                }
                else
                {
                    m_Dices[x, y].Color = Dice.ColorType.White;
                }
            }
        }
    }

    public void CalcNextSwap()
    {
        _GUIToBoard();

        var board = m_Board;

        List<(int, int)> candidates = new List<(int, int)>();

        for ( int x = 0; x < 5; ++x ) 
        {
            for( int y = 0; y < 5; ++y )
            {
                if ((x & 1) != 0 && (y & 1) != 0)
                    continue;

                if (!board.greens[x, y])
                    candidates.Add((x, y));

                m_Dices[x, y].Selected = false;
            }
        }

        if (candidates.Count <= 0)
        {
            Debug.Log("Solved!!!");
            return;
        }

        board.CalcCandiates();

        
        var answers = board.answers;
        Debug.Log($"answers {answers.Count}");

        if (answers.Count <= 0)
        {
            Debug.LogError("答え候補が見つかりません。盤面の設定ミスっているか過去の白ダイスの設定にミスがありました。リセットしてください。");
            return;
        }

        List<DiceSurface> fixedSurface = new List<DiceSurface>();

        for (int x = 0; x < 5; ++x)
        {
            for (int y = 0; y < 5; ++y)
            {
                if ((x & 1) != 0 && (y & 1) != 0 || board.greens[x, y])
                    continue;

                int n = answers[0][x, y];

                if( answers.All( m => m[x,y] == n) )
                {
                    fixedSurface.Add(new DiceSurface() { x = x, y = y, num = n });
                }
            }
        }

        Debug.Log($"fixed {fixedSurface.Count}");

        if( fixedSurface.Count > 0 )
        {
            Dictionary<(int, int), (int, int)> swapSurfaces = new Dictionary<(int, int), (int, int)>();
            var numbers = board.numbers;

            foreach ( var surface in fixedSurface )
            {
                (int, int) swapPos;

                if ( swapSurfaces.TryGetValue((surface.num, numbers[surface.x, surface.y]), out swapPos) )
                {
                    var a = m_Dices[swapPos.Item1, swapPos.Item2];
                    var b = m_Dices[surface.x, surface.y];

                    a.Selected = true;
                    b.Selected = true;

                    int an = a.Number;
                    int bn = b.Number;

                    a.Number = bn;
                    b.Number = an;
                    a.Color = Dice.ColorType.Green;
                    b.Color = Dice.ColorType.Green;

                    _UpdateYellowSurface();
                    return;
                }
                else
                {
                    swapSurfaces[(numbers[surface.x, surface.y], surface.num)] = (surface.x, surface.y);
                }
            }

            bool FindCycle(List<int> used, List<int> ans, List<(int,int)> coords, int depth, int begin = 0 )
            {
                if (used.Count == depth)
                {
                    if (used.OrderBy(x => x).SequenceEqual(ans.OrderBy(y => y)))
                    {
                        Debug.Log($"{depth} cycle find.");

                        for( int i = 1; i < coords.Count; ++i )
                        {
                            if( ans[0] == used[i] )
                            {
                                _SwapDice(coords[0], coords[i]);
                                m_Dices[coords[0].Item1, coords[0].Item2].Color = Dice.ColorType.Green;
                                return true;
                            }
                        }
                    }

                    return false;
                }

                for (int i = begin + 1; i < fixedSurface.Count; ++i)
                {
                    int cur = board[fixedSurface[i].coord];
                    int fix = fixedSurface[i].num;

                    used.Add(cur);
                    ans.Add(fix);
                    coords.Add(fixedSurface[i].coord);

                    if (FindCycle(used, ans, coords, depth, i))
                        return true;

                    used.RemoveAt(used.Count-1);
                    ans.RemoveAt(ans.Count-1);
                    coords.RemoveAt(coords.Count - 1);
                }

                return false;
            }

            for( int cycle = 3; cycle <= 7; ++cycle )
            {
                if (FindCycle(new List<int>(), new List<int>(), new List<(int, int)>(), cycle ))
                {
                    _UpdateYellowSurface();
                    return;
                }
                    
            }

            foreach (var surface in fixedSurface)
            {
                for (int x = 0; x < 5; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        if ((x & 1) != 0 && (y & 1) != 0 || board.greens[x, y])
                            continue;

                        if (board.numbers[x, y] == surface.num)
                        {
                            _SwapDice((x,y), (surface.x, surface.y));
                            m_Dices[surface.x, surface.y].Color = Dice.ColorType.Green;
                            _UpdateYellowSurface();
                            return;
                        }
                    }
                }
            }
        }

        //交換して正解が出やすいところを探す

        (int, int) bestCoordA = (0,0), bestCoordB = (0,1);
        int bestFitCount = 0;

        for( int i = 0; i < candidates.Count; ++i )
        {
            for( int j = i+1; j < candidates.Count; ++j )
            {
                var s = candidates[i];
                var t = candidates[j];
                var sn = board[s];
                var tn = board[t];

                int fitCount = 0;

                foreach( var answer in answers )
                {
                    if (answer[s.Item1, s.Item2] == tn)
                        ++fitCount;

                    if (answer[t.Item1, t.Item2] == sn)
                        ++fitCount;
                }

                if(bestFitCount < fitCount)
                {
                    bestFitCount = fitCount;
                    bestCoordA = s;
                    bestCoordB = t;
                }
            }
        }

        _SwapDice(bestCoordA, bestCoordB);
    }

    void _SwapDice( (int,int) s, (int,int) t )
    {
        var a = m_Dices[s.Item1, s.Item2];
        var b = m_Dices[t.Item1, t.Item2];

        var an = a.Number;
        var bn = b.Number;

        a.Number = bn;
        b.Number = an;

        a.Selected = true;
        b.Selected = true;
    }

}
