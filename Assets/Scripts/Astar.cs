using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Priority_Queue;

public class Astar : MonoBehaviour
{
    /*
    private static Astar _instance;

    public static Astar Instance { get { return _instance; } }

    private void Awake()
    {
        // if the singleton hasn't been initialized yet
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }

        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    */

    public double heuristicDistance(Vector2Int p1, Vector2Int p2)
    {
        return Math.Sqrt(Math.Pow(Math.Abs(p1.x - p2.x), 2.0) + Math.Pow(Math.Abs(p1.y - p2.y), 2.0));
    }

    public List<Vector2Int> reconstructZigZagPath(Dictionary<Vector2Int, Vector2Int> predecessor, Vector2Int curPoint)
    {
        List<Vector2Int> path = new List<Vector2Int>() { curPoint };
        while(predecessor.ContainsKey(curPoint))
        {
            curPoint = predecessor[curPoint];
            path.Insert(0, curPoint);
        }
        return path;
    }

    public List<Vector2Int> reconstructStraightPath(ref MazeManager maze, ref List<Vector2Int> zig_zag_path)
    {
        List<Vector2Int> straight_path = new List<Vector2Int>();
        int size = zig_zag_path.Count;

        int lastest = 0;
        int count = 1;

        // start
        straight_path.Add(zig_zag_path[0]);

        while (count < size)
        {
            if(!ray_box_intersection_check(ref maze, zig_zag_path[lastest], zig_zag_path[count])){
                // no intersection
                count++;
            }

            else
            {
                if(count - lastest > 1)
                {
                    straight_path.Add(zig_zag_path[count - 1]);
                    lastest = count - 1;
                    count = lastest + 1;
                }
                count++;
            }
        }

        // goal
        straight_path.Add(zig_zag_path[size - 1]);

        return straight_path;
    }
    

    public bool ray_box_intersection_check(ref MazeManager maze, Vector2Int start, Vector2Int end)
    {
        int minX = (int) Math.Min(start.x, end.x);
        int maxX = (int) Math.Max(start.x, end.x);
        int minY = (int) Math.Min(start.y, end.y);
        int maxY = (int) Math.Max(start.y, end.y);

        for(int i = minX; i <= maxX; i++)
        {
            for(int j = minY; j <= maxY; j++)
            {
                Vector2Int pos = new Vector2Int(i, j);
                if (maze.isObstacle(ref pos))
                {
                    if(ray_box_intersection_check_helper(ref start, ref end, ref pos))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool ray_box_intersection_check_helper(ref Vector2Int start, ref Vector2Int end, ref Vector2Int pos)
    {
        // Special case: the ray just cross through the corner of the pos square

        Vector2 line1point1 = new Vector2((float)(start.x + 0.5), (float)(start.y + 0.5));
        Vector2 line1point2 = new Vector2((float)(end.x + 0.5), (float)(end.y + 0.5));
        Vector2 line2point1 = new Vector2((float)(pos.x + 0.5), (float)(pos.y + 0.5));

        int[,] offset = { { 0, 0 }, { 0, 1 }, { 1, 0 }, { 1, 1 } };

        for (int i = 0; i < 4; i++)
        {
            Vector2 line2point2 = new Vector2((float)(pos.x + offset[i, 0]), (float)(pos.y + offset[i, 1]));

            Vector2 a = line1point2 - line1point1;
            Vector2 b = line2point1 - line2point2;
            Vector2 c = line1point1 - line2point1;

            float alphaNumerator = b.y * c.x - b.x * c.y;
            float betaNumerator = a.x * c.y - a.y * c.x;
            float denominator = a.y * b.x - a.x * b.y;

            if (denominator == 0)
            {
                continue;
            }
            else if (denominator > 0)
            {
                if (alphaNumerator < 0 || alphaNumerator > denominator || betaNumerator < 0 || betaNumerator > denominator)
                {
                    continue;
                }
            }
            else if (alphaNumerator > 0 || alphaNumerator < denominator || betaNumerator > 0 || betaNumerator < denominator)
            {
                continue;
            }
            return true;
        }

        return false;
    }

    public List<KeyValuePair<Vector2Int, bool>> getNeighbors(ref MazeManager maze, Vector2Int point)
    {
        List<KeyValuePair<Vector2Int, bool>> neighbors = new List<KeyValuePair<Vector2Int, bool>>();
        //bool reachObstacle = false;

        int[,] offset = new int[8, 2] { {-1, -1}, {-1, 0}, {-1, 1},
                                        { 0, -1}, { 0, 1},
                                        { 1, -1}, { 1, 0}, { 1, 1}};
        for(int i = 0; i < 8; i++)
        {
            int x = point.x + offset[i, 0];
            int y = point.y + offset[i, 1];
            Vector2Int neighbor = new Vector2Int(x, y);

            /*
            if(maze.isObstacle(ref neighbor))
            {
                reachObstacle = true;
            }
            */

            if (x >= 0 && x < maze.rows && y >= 0 && y < maze.cols && !maze.isObstacle(ref neighbor))
            {
                if (i == 1 || i == 3 || i == 4 || i == 6)
                {
                    // Top / Left / Right / Bottom (Short edges)
                    neighbors.Add(new KeyValuePair<Vector2Int, bool>(neighbor, true));
                }
                else
                {
                    // Long edges
                    neighbors.Add(new KeyValuePair<Vector2Int, bool>(neighbor, false));
                }
            }
        }
        return neighbors;
    }

    public class openSetElement : FastPriorityQueueNode
    {
        public Vector2Int pos_ { get; private set; }
        public openSetElement(Vector2Int pos)
        {
            pos_ = pos;
        }
    }

    public List<Vector2Int> search(MazeManager maze)
    {
        Vector2Int start = maze.getStart();


        int openSetSize = maze.rows * maze.cols;
        FastPriorityQueue<openSetElement> openSet = new FastPriorityQueue<openSetElement>(openSetSize);

        //HashSet<Vector2Int> openSet = new HashSet<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();


        List<Vector2Int> goals = maze.getGoals();

        Vector2Int goal = goals[0];        

        if(maze.isObstacle(ref goal))
        {
            // Debug.Log("FAIL!");
            return new List<Vector2Int>();
        }

        
        Dictionary<Vector2Int, Vector2Int> predecessor = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, double> gScore = new Dictionary<Vector2Int, double>();
        Dictionary<Vector2Int, double> fScore = new Dictionary<Vector2Int, double>();

        gScore.Add(start, 0);
        fScore.Add(start, heuristicDistance(start, goal));

        openSetElement startInOpenSet = new openSetElement(start);
        openSet.Enqueue(startInOpenSet, (float)fScore[start]);

        Vector2Int curPoint = new Vector2Int();
        double nei_gscore = 0.0;

        while (openSet.Count > 0)
        {
            /*
            double minfScore = Math.Sqrt(Math.Pow(maze.rows, 2.0) + Math.Pow(maze.cols, 2.0));
            foreach(Vector2Int point in openSet)
            {
                double curfScore = (double)fScore[point];
                if (minfScore > curfScore)
                {
                    curPoint = point;
                    minfScore = curfScore;
                }
            }
            */
            curPoint = openSet.Dequeue().pos_;
            double minfScore = fScore[curPoint];


            if (curPoint == goal)
            {   
                List<Vector2Int> zig_zag_path = reconstructZigZagPath(predecessor, goal);
                //Debug.Log("zig_zag_SUCCESS!!!");

                List<Vector2Int> straight_path = reconstructStraightPath(ref maze, ref zig_zag_path);
                //Debug.Log("straight_SUCCESS!!!");

                return straight_path;
                
            }

            closedSet.Add(curPoint);
            // openSet.Remove(curPoint);

            foreach (KeyValuePair<Vector2Int, bool> neighbor in getNeighbors(ref maze, curPoint))
            {
                if (closedSet.Contains(neighbor.Key)) {
                    continue;
                }

                if (neighbor.Value) {
                    // Short Edges
                    nei_gscore = gScore[curPoint] + 1.0;
                }
                else {
                    // Long Edges
                    nei_gscore = gScore[curPoint] + Math.Sqrt(2.0);
                }

                if(!gScore.ContainsKey(neighbor.Key) || (nei_gscore < gScore[neighbor.Key]))
                {
                    predecessor[neighbor.Key] = curPoint;
                    gScore[neighbor.Key] = nei_gscore;
                    fScore[neighbor.Key] = nei_gscore + heuristicDistance(neighbor.Key, goal);
                    openSetElement neighborInOpenSet = new openSetElement(neighbor.Key);
                    if (!openSet.Contains(neighborInOpenSet))
                    {
                        openSet.Enqueue(neighborInOpenSet, (float)fScore[neighbor.Key]);
                    }
                }
            }
        }

        // Debug.Log("FAIL!!!");
        

        return new List<Vector2Int>();
    }
}
