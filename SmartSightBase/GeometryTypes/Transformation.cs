using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSightBase.GeometryTypes
{
    public class Transformation
    {
        private readonly Matrix33 m_rotation;
        private readonly Vector3 m_translation;

        public Transformation()
        {
            m_rotation = Matrix33.Identity();
            m_translation = Vector3.Zero();
        }

        public Transformation(Matrix33 rotation, Vector3 translation)
        {
            m_rotation = rotation;
            m_translation = translation;
        }

        public Matrix33 Rotation()
        {
            return m_rotation;
        }

        public Vector3 Translation()
        {
            return m_translation;
        }

        public Matrix44 GetMatrix44()
        {
            var matrix = Matrix44.Identity();

            for (var col = 0; col < 3; col++)
            {
                for (var row = 0; row < 3; row++)
                {
                    // Copy rotation component
                    matrix.Mat[row,col] = m_rotation.Mat[row,col];
                }

                // Copy translation component
                matrix.Mat[3,col] = m_translation.Data[col];
            }

            return matrix;
        }

        public Transformation GetInverted()
        {
            return new Transformation(m_rotation.GetTransposed(), m_translation.Negate());
        }

        internal Transformation getInverted()
        {
            throw new NotImplementedException();
        }
    }
}
