using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSightBase.GeometryTypes
{
    class Matrix44
    {
        public float[] Data { get; set; } = new float[16];

        public float[,] Mat { get; set; } = new float[4, 4];

        public Matrix44 GetTransposed()
        {
            var matrix = new Matrix44();

            for (var i = 0; i < 4; i++)
                for (var j = 0; j < 4; j++)
                    matrix.Mat[i,j] = this.Mat[j,i];

            return matrix;
        }

        public Matrix44 GetInvertedRT()
        {
            var matrix = Matrix44.Identity();

            for (var col = 0; col < 3; col++)
            {
                for (var row = 0; row < 3; row++)
                {
                    // Transpose rotation component (inversion)
                    matrix.Mat[row,col] = this.Mat[col,row];
                }

                // Inverse translation component
                matrix.Mat[3,col] = -this.Mat[3,col];
            }
            return matrix;
        }

        public static Matrix44 Identity()
        {
            var matrix = new Matrix44();

            for (var i = 0; i < 4; i++)
                for (var j = 0; j < 4; j++)
                    matrix.Mat[i,j] = (i == j ? 1 : 0);

            return matrix;
        }
    }
}
