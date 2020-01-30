#include "markerDetector.hpp"
#include <opencv2\core\types_c.h>
#include <opencv2\imgproc\types_c.h>

markerDetector::markerDetector(CameraCalibration calibration) :m_minContourLengthAllowed(100), markerSize(100, 100), cellSize(100 / 7)
{
	cv::Mat(3, 3, CV_32F, const_cast<float*>(&calibration.getIntrinsic().data[0])).copyTo(camMatrix);
	cv::Mat(4, 1, CV_32F, const_cast<float*>(&calibration.getDistorsion().data[0])).copyTo(distCoeff);

	m_markerCorners3d.push_back(cv::Point3f(-0.5f, -0.5f, 0));
	m_markerCorners3d.push_back(cv::Point3f(+0.5f, -0.5f, 0));
	m_markerCorners3d.push_back(cv::Point3f(+0.5f, +0.5f, 0));
	m_markerCorners3d.push_back(cv::Point3f(-0.5f, +0.5f, 0));

	m_markerCorners2d.push_back(cv::Point2f(0, 0));
	m_markerCorners2d.push_back(cv::Point2f(static_cast<float>(markerSize.width - 1), 0));
	m_markerCorners2d.push_back(cv::Point2f(static_cast<float>(markerSize.width - 1), static_cast<float>(markerSize.height - 1)));
	m_markerCorners2d.push_back(cv::Point2f(0, static_cast<float>(markerSize.height - 1)));

	templateMarker = new int[25]{ 1, 1, 1, 0, 1,
								  0, 0, 0, 1, 0,
								  1, 1, 1, 1, 0,
								  1, 1, 1, 1, 0,
								  1, 1, 1, 0, 0 };
}

void markerDetector::findMarkers(const cv::Mat& input, bool showImg)
{
	prepareImage(input, grayscalse);

	performThreshold(grayscalse, thresholdImg);

	findContours(thresholdImg, contours, grayscalse.cols / 5);

	findCandidates(contours, detectedMarkers);

	goodMarkers = recognizeMarkers(grayscalse, detectedMarkers, showImg);

	estimatePosition(goodMarkers);
}

void markerDetector::prepareImage(const cv::Mat& bgrMat, cv::Mat& grayscale) const
{
	cv::cvtColor(bgrMat, grayscale, CV_BGR2GRAY);
}

void markerDetector::performThreshold(const cv::Mat& grayscale, cv::Mat& thresholdImg) const
{
	cv::adaptiveThreshold(grayscale, thresholdImg, 255, cv::ADAPTIVE_THRESH_GAUSSIAN_C, cv::THRESH_BINARY_INV, 7, 7);
}

void markerDetector::findContours(const cv::Mat& thresholdImg, std::vector<std::vector<cv::Point>>& contours, int minContourPointsAllowed)
{
	std::vector<std::vector<cv::Point>> allContours;
	cv::findContours(thresholdImg, allContours, CV_RETR_LIST, CV_CHAIN_APPROX_NONE);

	contours.clear();

	for (size_t i = 0; i < allContours.size(); i++)
	{
		size_t contourSize = allContours[i].size();
		if (contourSize > minContourPointsAllowed)
		{
			contours.push_back(allContours[i]);
		}
	}
}

float perimeter(const std::vector<cv::Point2f>& a)
{
	float sum = 0, dx, dy;

	for (size_t i = 0; i < a.size(); i++)
	{
		size_t i2 = (i + 1) % a.size();

		dx = a[i].x - a[i2].x;
		dy = a[i].y - a[i2].y;

		sum += sqrt(dx * dx + dy * dy);
	}

	return sum;
}


void markerDetector::findCandidates(const std::vector<std::vector<cv::Point>>& contours, std::vector<std::vector<cv::Point2f>>& marks)
{
	std::vector<cv::Point> approxCurve;
	std::vector<std::vector<cv::Point2f>> possibleMarkers;

	// For each contour, analyze if it is a parallelepiped likely to be the	marker
	for (size_t i = 0; i < contours.size(); i++)
	{
		//Approximate to a polygon
		double eps = contours[i].size() * 0.05;
		cv::approxPolyDP(contours[i], approxCurve, eps, true);

		// We interested only in polygons that contains only four points
		if (approxCurve.size() != 4)
			continue;

		// And they have to be convex
		if (!cv::isContourConvex(approxCurve))
			continue;

		// Ensure that the distance between consecutive points is large enough
		float minDist = std::numeric_limits<float>::max();
		for (int i = 0; i < 4; i++)
		{
			cv::Point side = approxCurve[i] - approxCurve[(i + 1) % 4];
			float squaredSideLength = static_cast<float>(side.dot(side));
			minDist = std::min(minDist, squaredSideLength);
		}

		// Check that distance is not very small
		if (minDist < m_minContourLengthAllowed)
			continue;

		// All tests are passed. Save marker candidate:
		std::vector<cv::Point2f> m;
		for (int i = 0; i < 4; i++)
			m.push_back(cv::Point2f(static_cast<float>(approxCurve[i].x), static_cast<float>(approxCurve[i].y)));

		// Sort the points in anti-clockwise order
		// Trace a line between the first and second point.
		// If the third point is at the right side, then the points are anticlockwise
		cv::Point v1 = m[1] - m[0];
		cv::Point v2 = m[2] - m[0];

		double o = (v1.x * v2.y) - (v1.y * v2.x);
		if (o < 0.0) //if the third point is in the left side,	then sort in anti - clockwise order
			std::swap(m[1], m[3]);
		possibleMarkers.push_back(m);
	}

	// Remove these elements which corners are too close to each other.
	// First detect candidates for removal:
	std::vector< std::pair<int, int> > tooNearCandidates;

	for (size_t i = 0; i < possibleMarkers.size(); i++) {

		const std::vector<cv::Point2f>& m1 = possibleMarkers[i];

		//calculate the average distance of each corner to the nearest corner of the other marker candidate
		for (size_t j = i + 1; j < possibleMarkers.size(); j++)
		{
			const std::vector<cv::Point2f>& m2 = possibleMarkers[j];
			float distSquared = 0;

			for (int c = 0; c < 4; c++)
			{
				cv::Point v = m1[c] - m2[c];
				distSquared += v.dot(v);
			}
			distSquared /= 4;

			if (distSquared < 100)
			{
				tooNearCandidates.push_back(std::pair<int, int>(i, j));
			}
		}
	}

	// Mark for removal the element of the pair with smaller perimeter
	std::vector<bool> removalMask(possibleMarkers.size(), false);

	for (size_t i = 0; i < tooNearCandidates.size(); i++)
	{
		float p1 = perimeter(possibleMarkers[tooNearCandidates[i].first]);
		float p2 = perimeter(possibleMarkers[tooNearCandidates[i].second]);

		size_t removalIndex;

		if (p1 > p2)
			removalIndex = tooNearCandidates[i].second;
		else
			removalIndex = tooNearCandidates[i].first;

		removalMask[removalIndex] = true;
	}

	// Return candidates
	detectedMarkers.clear();
	for (size_t i = 0; i < possibleMarkers.size(); i++)
	{
		if (!removalMask[i])
			detectedMarkers.push_back(possibleMarkers[i]);
	}

}

int markerDetector::hammDistMarker(cv::Mat bits)
{
	int dist = 0;

	for (int y = 0; y < 5; y++)
	{
		int minSum = static_cast<int>(1e5); //hamming distance to each possible word

		for (int p = 0; p < 5; p++)
		{
			int sum = 0;

			//now, count
			for (int x = 0; x < 5; x++)
				sum += bits.at<uchar>(y, x) == templateMarker[p * 5 + x] ? 0 : 1;

			if (minSum > sum)
				minSum = sum;
		}

		//do the and
		dist += minSum;
	}

	return dist;
}

void markerDetector::estimatePosition(std::vector<std::vector<cv::Point2f>>& detectedMarkers)
{
	for (size_t i = 0; i < detectedMarkers.size(); i++)
	{
		std::vector<cv::Point2f>& m = detectedMarkers[i];

		cv::Mat Rvec;
		cv::Mat_<float> Tvec;
		cv::Mat raux, taux;
		cv::solvePnP(m_markerCorners3d, m, camMatrix, distCoeff, raux, taux);
		raux.convertTo(Rvec, CV_32F);
		taux.convertTo(Tvec, CV_32F);

		cv::Mat_<float> rotMat(3, 3);
		cv::Rodrigues(Rvec, rotMat);

		// Copy to transformation matrix
		for (int col = 0; col < 3; col++)
		{
			for (int row = 0; row < 3; row++)
			{
				transformation.r().mat[row][col] = rotMat(row, col); // Copy rotation component
			}
			transformation.t().data[col] = Tvec(col); // Copy translation component
		}

		// Since solvePnP finds camera location, w.r.t to marker pose, to get marker pose w.r.t to the camera we invert it.
		transformation = transformation.getInverted();
	}
}

cv::Mat rotate(cv::Mat in)
{
	cv::Mat out;
	in.copyTo(out);
	for (int i = 0; i < in.rows; i++)
	{
		for (int j = 0; j < in.cols; j++)
		{
			out.at<uchar>(i, j) = in.at<uchar>(in.cols - j - 1, i);
		}
	}

	return out;
}

std::vector<std::vector<cv::Point2f>> markerDetector::recognizeMarkers(const cv::Mat& gray, std::vector<std::vector<cv::Point2f>>& detectedMarkers, bool show)
{
	std::vector<std::vector<cv::Point2f>> goodMarkers;

	for (size_t i = 0; i < detectedMarkers.size(); i++) {

		cv::Mat canonicalMarker;
		std::vector<cv::Point2f>& marker = detectedMarkers[i];

		// Find the perspective transfomation that brings current marker to rectangular form
		cv::Mat M = cv::getPerspectiveTransform(marker, m_markerCorners2d);

		// Transform image to get a canonical marker image
		cv::warpPerspective(gray, canonicalMarker, M, markerSize);

		//threshold image
		cv::threshold(canonicalMarker, canonicalMarker, 125, 255, cv::THRESH_BINARY | cv::THRESH_OTSU);

		//Read the code
		cv::Mat bitMatrix = cv::Mat::zeros(5, 5, CV_8UC1);

		//get information (for each innner square, determine if it is black or white)
		for (int y = 0; y < 5; y++)
			for (int x = 0; x < 5; x++)
			{
				int cellX = (x + 1) * cellSize;
				int cellY = (y + 1) * cellSize;
				cv::Mat cell = canonicalMarker(cv::Rect(cellX, cellY, cellSize, cellSize));

				int nZ = cv::countNonZero(cell);
				if (nZ > (cellSize * cellSize / 2))
					bitMatrix.at<uchar>(y, x) = 1;
			}

		//check all possible rotations
		cv::Mat rotations[4];
		int distances[4];
		rotations[0] = bitMatrix;
		distances[0] = hammDistMarker(rotations[0]);
		std::pair<int, int> minDist(distances[0], 0);

		for (int i = 1; i < 4; i++)
		{
			//get the hamming distance to the nearest possible word
			rotations[i] = rotate(rotations[i - 1]);
			distances[i] = hammDistMarker(rotations[i]);
			if (distances[i] < minDist.first)
			{
				minDist.first = distances[i];
				minDist.second = i;
			}
		}

		if (minDist.first == 0)
		{
			int nRotations = minDist.second;
			//sort the points so that they are always in the same order
			//no matter the camera orientation
			std::rotate(marker.begin(), marker.begin() + 4 - nRotations, marker.end());
			goodMarkers.push_back(marker);
		}
	}

	//Marker locaiton refinement
	if (goodMarkers.size() > 0)
	{
		std::vector<cv::Point2f> preciseCorners(4 * goodMarkers.size());
		for (size_t i = 0; i < goodMarkers.size(); i++)
		{
			std::vector<cv::Point2f>& marker = goodMarkers[i];
			for (int c = 0; c < 4; c++)
			{
				preciseCorners[i * 4 + c] = marker[c];
			}
		}

		cv::cornerSubPix(gray, preciseCorners, cvSize(5, 5), cvSize(-1, -
			1), cvTermCriteria(CV_TERMCRIT_ITER, 30, 0.1));

		//copy back
		for (size_t i = 0; i < goodMarkers.size(); i++)
		{
			std::vector<cv::Point2f>& marker = goodMarkers[i];
			for (int c = 0; c < 4; c++)
			{
				marker[c] = preciseCorners[i * 4 + c];
			}
		}
	}

	if (show)
	{
		cv::Mat imgShow;
		cv::cvtColor(gray, imgShow, cv::COLOR_GRAY2BGR);
		float thickness = 2;
		cv::Scalar color = cv::Scalar(0, 0, 255);
		for (size_t i = 0; i < goodMarkers.size(); i++) {
			std::vector<cv::Point2f> current_mark = goodMarkers[i];
			std::vector<cv::Point2i> int_current_mark;
			cv::Mat(current_mark).copyTo(int_current_mark);
			cv::line(imgShow, int_current_mark[0], int_current_mark[1], color, thickness, CV_AA);
			cv::line(imgShow, int_current_mark[1], int_current_mark[2], color, thickness, CV_AA);
			cv::line(imgShow, int_current_mark[2], int_current_mark[3], color, thickness, CV_AA);
			cv::line(imgShow, int_current_mark[3], int_current_mark[0], color, thickness, CV_AA);
		}
		imshow("mark", imgShow);
		imgShow = cv::Mat::zeros(cv::Size(640, 480), CV_8UC3);
	}

	return goodMarkers;
}