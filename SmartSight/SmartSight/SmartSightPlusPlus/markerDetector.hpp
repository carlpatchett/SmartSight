#ifndef Example_MarkerBasedAR_MarkerDetector_hpp
#define Example_MarkerBasedAR_MarkerDetector_hpp

#include <opencv2\imgproc\imgproc.hpp>
#include <opencv2\opencv.hpp>
#include <opencv2\highgui.hpp>

#include "CameraCalibration.hpp"


class markerDetector
{
public:
	
	int* templateMarker;
public:
	markerDetector(CameraCalibration calibration);
	void findMarkers(const cv::Mat & input, bool showImg);
	std::vector<std::vector<cv::Point2f>> goodMarkers;	
	// Marker transformation with regards to the camera
	Transformation transformation;
		
protected:
	// Change to grayscale image
	void prepareImage(const cv::Mat& bgrMat, cv::Mat& grayscale) const;
	//Iamge binarization
	void performThreshold(const cv::Mat& grayscale, cv::Mat& thresholdImg) const;
	//Contours detection
	void findContours(const cv::Mat& thresholdImg, std::vector<std::vector<cv::Point>>& contours, int minContourPointsAllowed); 
	// Finds marker candidates among all contours (find polygons with 4 vertices)
	void findCandidates(const std::vector<std::vector<cv::Point>>& contours, std::vector<std::vector<cv::Point2f>>& marks);
	// To verify whether those polygons are markers or not
	std::vector<std::vector<cv::Point2f>> recognizeMarkers(const cv::Mat& gray, std::vector<std::vector<cv::Point2f>>& detectedMarkers, bool showImg);
	int hammDistMarker(cv::Mat bits);
	//! Calculates marker poses in 3D
	void estimatePosition(std::vector<std::vector<cv::Point2f>>& detectedMarkers);
	

private:
	//Parameters
	float m_minContourLengthAllowed;
	cv::Size markerSize;
	int cellSize;
	cv::Mat camMatrix;
	cv::Mat distCoeff;

	//Images
	//cv::Mat bgrImg;
	cv::Mat grayscalse;
	cv::Mat thresholdImg;
	
	//counters
	std::vector<std::vector<cv::Point2f>> detectedMarkers;
	
	std::vector<std::vector<cv::Point>> contours;
	std::vector<cv::Point3f> m_markerCorners3d;
	std::vector<cv::Point2f> m_markerCorners2d;

};

#endif


