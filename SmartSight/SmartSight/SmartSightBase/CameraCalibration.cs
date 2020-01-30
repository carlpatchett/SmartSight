using SmartSightBase.GeometryTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSightBase
{
    class CameraCalibration
    {
        private static readonly Matrix33 mIntrinsic = new Matrix33();
        private static readonly Vector4 mDistorsion = new Vector4();

        public CameraCalibration()
        {

        }

        public CameraCalibration(float fx, float fy, float cx, float cy)
        {
            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    mIntrinsic.Mat[i,j] = 0;

            mIntrinsic.Mat[0,0] = fx;
            mIntrinsic.Mat[1,1] = fy;
            mIntrinsic.Mat[0,2] = cx;
            mIntrinsic.Mat[1,2] = cy;

            for (int i = 0; i < 4; i++)
                mDistorsion.Data[i] = 0;
        }

        public CameraCalibration(float fx, float fy, float cx, float cy, float[] distorsionCoeff)
        {
            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    mIntrinsic.Mat[i,j] = 0;

            mIntrinsic.Mat[0,0] = fx;
            mIntrinsic.Mat[1,1] = fy;
            mIntrinsic.Mat[0,2] = cx;
            mIntrinsic.Mat[1,2] = cy;

            for (var i = 0; i < 4; i++)
                mDistorsion.Data[i] = distorsionCoeff[i];
        }

        public void GetMatrix34(float[,] cparam)
        {
            for (var j = 0; j < 3; j++)
                for (var i = 0; i < 3; i++)
                    cparam[i,j] = mIntrinsic.Mat[i,j];

            for (var i = 0; i < 4; i++)
                cparam[3,i] = mDistorsion.Data[i];
        }

        public Matrix33 GetIntrinsic()
        {
            return mIntrinsic;
        }

        public Vector4 GetDistorsion()
        {
            return mDistorsion;
        }
    }
}
