using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSightBase.GeometryTypes
{
    class Vector3
    {
        public float[] Data { get; set; } = new float[3];

        public static Vector3 Zero()
        {
            return new Vector3()
            {
                Data = new float[] { 0, 0, 0 }
            };
        }

        public Vector3 Negate()
        {
            return new Vector3()
            {
                Data = new float[] { -this.Data[0], -this.Data[1], -this.Data[2] }
            };
        }
    }
}
