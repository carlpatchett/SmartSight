#include "openGLDraw.hpp"

AR_Draw::AR_Draw() :width(640), height(480)
{
	cameraMatrix = new double[9] { 6.1283817001950126e+02, 0., 3.2646467679700390e+02, 0., 6.1283817001950126e+02, 2.8942041679741317e+02, 0., 0., 1. };
}

int AR_Draw::AR_GL_Draw(const cv::Mat & frame, const Transformation& calibration)
{
	// clear the window
	glClear(GL_COLOR_BUFFER_BIT);

	// show the current camera frame
	//based on the way cv::Mat stores data, you need to flip it before displaying it
	cv::Mat tempimage;
	cv::flip(frame, tempimage, 0);
	glDrawPixels(tempimage.size().width, tempimage.size().height, GL_BGR_EXT, GL_UNSIGNED_BYTE, tempimage.ptr());

	//////////////////////////////////////////////////////////////////////////////////
	// Here, set up new parameters to render a scene viewed from the camera.

	//set viewport
	glViewport(0, 0, tempimage.size().width, tempimage.size().height);

	//set projection matrix using intrinsic camera params
	glMatrixMode(GL_PROJECTION);
	glLoadIdentity();

	//gluPerspective is arbitrarily set, you will have to determine these values based
	//on the intrinsic camera parameters
	gluPerspective(60, tempimage.size().width*1.0 / tempimage.size().height, 1, 20);

	//you will have to set modelview matrix using extrinsic camera params
	glMatrixMode(GL_MODELVIEW);
	glLoadIdentity();
	gluLookAt(0, 0, 5, 0, 0, 0, 0, 1, 0);

	glPushMatrix();

	// Drawing routine
	// move to the position where you want the 3D object to go
	glClear(GL_DEPTH_BUFFER_BIT);
	Matrix44 projectionMatrix;
	buildProjectionMatrix(width, height, projectionMatrix);
	glMatrixMode(GL_PROJECTION);
	glLoadMatrixf(projectionMatrix.data);

	glMatrixMode(GL_MODELVIEW);
	glLoadIdentity();

	glDepthMask(TRUE);
	glEnable(GL_DEPTH_TEST);

	glEnableClientState(GL_VERTEX_ARRAY);
	glEnableClientState(GL_NORMAL_ARRAY);

	glPushMatrix();
	glLineWidth(3.0f);

	Matrix44 glMatrix = calibration.getMat44();

	glLoadMatrixf(reinterpret_cast<const GLfloat*>(&glMatrix.data[0]));


	glTranslated(0.0f, 0.0f, 0.1f);
	glRotated(90.0f, 1.0f, 0.0f, 0.0f);
	glutSolidTeapot(0.5);

	drawAxes(1.0);
	glPopMatrix();

	//lighting
	GLfloat light_ambient[] = { 1.0, 1.0, 1.0, 1.0 };  /* Red diffuse light. */
	GLfloat light_diffuse[] = { 1.0, 0.0, 0.0, 1.0 };  /* Red diffuse light. */
	GLfloat light_position[] = { 1.0, 1.0, 1.0, 0.0 };  /* Infinite light location. */
	glLightfv(GL_LIGHT0, GL_AMBIENT, light_ambient);
	glLightfv(GL_LIGHT0, GL_DIFFUSE, light_diffuse);
	glLightfv(GL_LIGHT0, GL_POSITION, light_position);
	glEnable(GL_LIGHT0);
	glEnable(GL_LIGHTING);


	// show the rendering on the screen
	glutSwapBuffers();

	// post the next redisplay
	glutPostRedisplay();
	return 0;
}

void AR_Draw::buildProjectionMatrix(int width, int height, Matrix44 & projectionMatrix)
{
	float near_scene = 0.01;  // Near clipping distance
	float far_scene = 100;  // Far clipping distance

	// Camera parameters
	float f_x = cameraMatrix[0]; // Focal length in x axis
	float f_y = cameraMatrix[4]; // Focal length in y axis (usually the same?)
	float c_x = cameraMatrix[2]; // Camera primary point x
	float c_y = cameraMatrix[5]; // Camera primary point y

	projectionMatrix.data[0] = -2.0 * f_x / width;
	projectionMatrix.data[1] = 0.0;
	projectionMatrix.data[2] = 0.0;
	projectionMatrix.data[3] = 0.0;

	projectionMatrix.data[4] = 0.0;
	projectionMatrix.data[5] = 2.0 * f_y / height;
	projectionMatrix.data[6] = 0.0;
	projectionMatrix.data[7] = 0.0;

	projectionMatrix.data[8] = 2.0 * c_x / width - 1.0;
	projectionMatrix.data[9] = 2.0 * c_y / height - 1.0;
	projectionMatrix.data[10] = -(far_scene + near_scene) / (far_scene - near_scene);
	projectionMatrix.data[11] = -1.0;

	projectionMatrix.data[12] = 0.0;
	projectionMatrix.data[13] = 0.0;
	projectionMatrix.data[14] = -2.0 * far_scene * near_scene / (far_scene - near_scene);
	projectionMatrix.data[15] = 0.0;
}

void AR_Draw::drawAxes(float length)
{
	glPushAttrib(GL_POLYGON_BIT | GL_ENABLE_BIT | GL_COLOR_BUFFER_BIT);

	glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
	glDisable(GL_LIGHTING);

	glBegin(GL_LINES);
	glColor3f(1, 0, 0);
	glVertex3f(0, 0, 0);
	glVertex3f(length, 0, 0);

	glColor3f(0, 1, 0);
	glVertex3f(0, 0, 0);
	glVertex3f(0, length, 0);

	glColor3f(0, 0, 1);
	glVertex3f(0, 0, 0);
	glVertex3f(0, 0, length);
	glEnd();


	glPopAttrib();
}


