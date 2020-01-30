#include <opencv2\imgproc\imgproc.hpp>
#include <opencv2\highgui\highgui.hpp>
#include <opencv2\opencv.hpp>
#include <iostream>

#include "Monitor.h"
#include "markerDetector.hpp" 
#include "CameraCalibration.hpp"

cv::VideoCapture* cap = NULL;
int width = 640;
int height = 480;
cv::Mat image;
bool monitorRunning = false;

float cameraMatrix[9] = { 612.84f, 0.0f, 326.46f, 0.0f, 612.84f, 289.42f, 0.0f, 0.0f, 1.0f };

markerDetector detector(CameraCalibration(cameraMatrix[0], cameraMatrix[4], cameraMatrix[2], cameraMatrix[5]));
Transformation transMax;


Monitor::Monitor() : mMonitorRunning(false)
{
	cap = new cv::VideoCapture(0);
}

extern "C" Monitor* Monitor::CreateNewMonitor()
{
	return new Monitor();
}

int __stdcall Monitor::StartMonitor()
{
	cap = new cv::VideoCapture(0);

	if (cap == NULL || !cap->isOpened()) {
		fprintf(stderr, "could not start video capture\n");
		return 1;
	}

	width = (int)cap->get(CV_CAP_PROP_FRAME_WIDTH);
	height = (int)cap->get(CV_CAP_PROP_FRAME_HEIGHT);

	mMonitorRunning = true;

	while (mMonitorRunning)
	{
		cap->read(image);
		cv::imshow("Monitoring Window", image);

		// Run OpenCV functionality
		(*cap) >> image;
		detector.findMarkers(image, true);
		transMax = detector.transformation;

		cv::waitKey(1);
	}

	return 0;
}

int __stdcall Monitor::StopMonitor()
{
	mMonitorRunning = false;

	return 0;
}