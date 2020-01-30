using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSightBase.GeometryTypes
{
    class Matrix33
    {
        public float[] Data { get; set; } = new float[9];

        public float[,] Mat { get; set; } = new float[3, 3];

        public static Matrix33 Identity()
        {
            var matrix = new Matrix33();

            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    matrix.Mat[i,j] = (i == j ? 1 : 0);

            return matrix;
        }

        public Matrix33 GetTransposed()
        {
            var matrix = new Matrix33();

            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    matrix.Mat[i,j] = this.Mat[j,i];

            return matrix;
        }
    }
}
