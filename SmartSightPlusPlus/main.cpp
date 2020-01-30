#include <opencv2\imgproc\imgproc.hpp>
#include <opencv2\highgui\highgui.hpp>
#include <opencv2\opencv.hpp>
#include <iostream>

#include "markerDetector.hpp" 
#include "CameraCalibration.hpp"


cv::VideoCapture *cap = NULL;
int width = 640;
int height = 480;
cv::Mat image;

float cameraMatrix[9] = { 612.84f, 0.0f, 326.46f, 0.0f, 612.84f, 289.42f, 0.0f, 0.0f, 1.0f };

markerDetector detector(CameraCalibration(cameraMatrix[0], cameraMatrix[4], cameraMatrix[2], cameraMatrix[5]));
Transformation transMax;

void openCVcode()
{
	(*cap) >> image;

	detector.findMarkers(image,true);

	transMax = detector.transformation;
}


int main(int argc, char **argv)
{
	cap = new cv::VideoCapture(0);		

	if (cap == NULL || !cap->isOpened()) {
		fprintf(stderr, "could not start video capture\n");
		return 1;
	}

	width = (int)cap->get(CV_CAP_PROP_FRAME_WIDTH);
	height = (int)cap->get(CV_CAP_PROP_FRAME_HEIGHT);

	while (true)
	{
		cap->read(image);
		cv::imshow("Test", image);
		openCVcode();
		cv::waitKey(1);
	}

    return 0;
}