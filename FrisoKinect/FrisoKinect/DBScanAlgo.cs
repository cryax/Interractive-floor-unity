using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Point
    {
        public const int NOISE = -1;
        public const int UNCLASSIFIED = 0;
        public int X, Y, ClusterId;
        public float ClusterIDMeanX, ClusterIDMeanY;
        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
            this.ClusterIDMeanX = 0;
            this.ClusterIDMeanY = 0;
        }
        public override string ToString()
        {
            return String.Format("({0}, {1})", X, Y);
        }
        public static int DistanceSquared(Point p1, Point p2)
        {
            int diffX = p2.X - p1.X;
            int diffY = p2.Y - p1.Y;
            return diffX * diffX + diffY * diffY;
        }
    }
    class DBScanAlgo
    {
        public static List<List<Point>> GetClusters(List<Point> points, double eps, int minPts)
        {
            if (points == null) return null;
            List<List<Point>> clusters = new List<List<Point>>();
            eps *= eps; // square eps
            int clusterId = 1;
            for (int i = 0; i < points.Count; i++)
            {
                Point p = points[i];
                if (p.ClusterId == Point.UNCLASSIFIED)
                {
                    if (ExpandCluster(points, p, clusterId, eps, minPts)) clusterId++;
                }
            }
            // sort out points into their clusters, if any
            int maxClusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;
            if (maxClusterId < 1) return clusters; // no clusters, so list is empty
            for (int i = 0; i < maxClusterId; i++) clusters.Add(new List<Point>());
            foreach (Point p in points)
            {
                if (p.ClusterId > 0) clusters[p.ClusterId - 1].Add(p);
            }
            return clusters;
        }
        public static List<Point> GetRegion(List<Point> points, Point p, double eps)
        {
            List<Point> region = new List<Point>();
            for (int i = 0; i < points.Count; i++)
            {
                int distSquared = Point.DistanceSquared(p, points[i]);
                if (distSquared <= eps) region.Add(points[i]);
            }
            return region;
        }
        public static bool ExpandCluster(List<Point> points, Point p, int clusterId, double eps, int minPts)
        {
            List<Point> seeds = GetRegion(points, p, eps);
            if (seeds.Count < minPts) // no core point
            {
                p.ClusterId = Point.NOISE;
                return false;
            }
            else // all points in seeds are density reachable from point 'p'
            {
                for (int i = 0; i < seeds.Count; i++) seeds[i].ClusterId = clusterId;
                seeds.Remove(p);
                while (seeds.Count > 0)
                {
                    Point currentP = seeds[0];
                    List<Point> result = GetRegion(points, currentP, eps);
                    if (result.Count >= minPts)
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            Point resultP = result[i];
                            if (resultP.ClusterId == Point.UNCLASSIFIED || resultP.ClusterId == Point.NOISE)
                            {
                                if (resultP.ClusterId == Point.UNCLASSIFIED) seeds.Add(resultP);
                                resultP.ClusterId = clusterId;
                            }
                        }
                    }
                    seeds.Remove(currentP);
                }
                return true;
            }
        }
    }
}
