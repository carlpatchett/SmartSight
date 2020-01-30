#ifndef Example_MarkerBasedAR_OpenGLDraw_hpp
#define Example_MarkerBasedAR_OpenGLDraw_hpp

#include <opencv2\opencv.hpp>
#include <gl/gl.h>
#include "CameraCalibration.hpp"

class AR_Draw
{
public:
	AR_Draw();
	int AR_GL_Draw(const cv::Mat& frame, const Transformation& transMax);
protected:
	void buildProjectionMatrix( int width, int height, Matrix44& projectionMatrix);
	void drawAxes(float length);
private:
	double* cameraMatrix; 
	int width;
	int height;
	Matrix44 projectionMatrix;
};

#endif
