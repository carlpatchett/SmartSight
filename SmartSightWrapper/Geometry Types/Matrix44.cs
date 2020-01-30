using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSightWrapper.Geometry_Types
{
    class Matrix44
    {
        public float[] Data { get; set; } = new float[16];
        public float[,] Mat { get; set; } = new float[4, 4];

        Matrix44 GetTransposed()
        {
            var newMatrix = new Matrix44();

            for (var i = 0; i < 4; i++)
                for (var j = 0; i < 4; j++)
                    newMatrix.Mat[i, j] = this.Mat[j, i];

            return newMatrix;
        }

        Matrix44 GetInvertedRT()
        {
            var newMatrix = Matrix44.Identity();

            for (var col = 0; col < 3; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    newMatrix.Mat[row, col] = this.Mat[col, row];
                }

                newMatrix.Mat[3, col] = -this.Mat[3, col];
            }

            return newMatrix;
        }

        static Matrix44 Identity()
        {
            var newMatrix = new Matrix44();

            for (var i = 0; i < 4; i++)
                for (var j = 0; j < 4; j++)
                    newMatrix.Mat[i, j] = (float)(i == j ? 1 : 0);

            return newMatrix;
        }
    }
}
