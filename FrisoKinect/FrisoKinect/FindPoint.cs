using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace FindPointNS
{
    struct Point
    {
        public float X;
        public float Y;
    }
    class FindPoint
    {
        Matrix<double> HomoMatrix;
        bool isExistHomoMatrix;
        public void Reset()
        {
            isExistHomoMatrix = false;
        }
       
        public  void CalHomoMatrix(Point[] KinectPoints, Point[] ProjPoints)
        {
            Matrix<double> C = new DenseMatrix(8, 1, new double[] { ProjPoints[0].X, ProjPoints[1].X, ProjPoints[2].X, ProjPoints[3].X, ProjPoints[0].Y, ProjPoints[1].Y, ProjPoints[2].Y, ProjPoints[3].Y });
            Matrix<double> A = DenseMatrix.OfArray(new double[,]{{KinectPoints[0].X,  KinectPoints[0].Y,    1, 0,     0,      0,  -KinectPoints[0].X*ProjPoints[0].X,        -KinectPoints[0].Y*ProjPoints[0].X},
                                {KinectPoints[1].X,  KinectPoints[1].Y,    1, 0,     0,      0,  -KinectPoints[1].X*ProjPoints[1].X,        -KinectPoints[1].Y*ProjPoints[1].X},
                                {KinectPoints[2].X,  KinectPoints[2].Y,    1, 0,     0,      0,  -KinectPoints[2].X*ProjPoints[2].X,        -KinectPoints[2].Y*ProjPoints[2].X},
                                { KinectPoints[3].X,  KinectPoints[3].Y,    1, 0,     0,      0,  -KinectPoints[3].X*ProjPoints[3].X,        -KinectPoints[3].Y*ProjPoints[3].X},
                                {0,     0,       0, KinectPoints[0].X,  KinectPoints[0].Y,   1,  -KinectPoints[0].X*ProjPoints[0].Y,        -KinectPoints[0].Y*ProjPoints[0].Y},
                                {0,     0,       0, KinectPoints[1].X,  KinectPoints[1].Y,   1,  -KinectPoints[1].X*ProjPoints[1].Y,        -KinectPoints[1].Y*ProjPoints[1].Y},
                                {0,     0,       0, KinectPoints[2].X,  KinectPoints[2].Y,   1,  -KinectPoints[2].X*ProjPoints[2].Y,        -KinectPoints[2].Y*ProjPoints[2].Y},
                                { 0,     0,       0, KinectPoints[3].X,  KinectPoints[3].Y,   1,  -KinectPoints[3].X*ProjPoints[3].Y,        -KinectPoints[3].Y*ProjPoints[3].Y}});
            A = A.Inverse();
            HomoMatrix = A * C;
            isExistHomoMatrix = true;

            //Matrix<double> matrixB = A * C;
            //return matrixB;
        }
        public  Point Calculate(Point INP)
        {
            Point Output;
            if (isExistHomoMatrix)
            {
            //Matrix<double> B = CalHomoMatrix(KinectPoints, ProjPoints);
                Output.X = (float)((HomoMatrix[0, 0] * INP.X + HomoMatrix[1, 0] * INP.Y + HomoMatrix[2, 0]) / (HomoMatrix[6, 0] * INP.X + HomoMatrix[7, 0] * INP.Y + 1));
                Output.Y = (float)((HomoMatrix[3, 0] * INP.X + HomoMatrix[4, 0] * INP.Y + HomoMatrix[5, 0]) / (HomoMatrix[6, 0] * INP.X + HomoMatrix[7, 0] * INP.Y + 1));
            }
            else
                throw new Exception();
            return Output;
        }
    }
}
