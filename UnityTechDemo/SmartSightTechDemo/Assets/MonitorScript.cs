using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmartSightBase;
using OpenCvSharp;
using System;
using UnityEditor;
using UnityEngine.UI;

public class MonitorScript : MonoBehaviour
{

    GameObject mRoomLight;
    GameObject mBedLight;
    GameObject mKitchenLight;

    public bool mGestureRecognitionSetUp;

    private Monitor mMonitor;
    private bool delay;
    private bool monitoringStarted;

    // Start is called before the first frame update
    void Start()
    {
        mMonitor = new Monitor();

        mMonitor.OneFingerDetected += mMonitor_OneFingerDetected;
        mMonitor.TwoFingersDetected += mMonitor_TwoFingersDetected;
        mMonitor.ThreeFingersDetected += mMonitor_ThreeFingersDetected;
        mMonitor.FourFingersDetected += mMonitor_FourFingersDetected;
        mMonitor.FiveFingersDetected += mMonitor_FiveFingersDetected;

        mRoomLight = GameObject.Find("RoomLight");
        mBedLight = GameObject.Find("BedLight");
        mKitchenLight = GameObject.Find("KitchenLight");

        GameObject.Find("UI").transform.GetChild(1).GetComponent<Button>().enabled = false;

        mMonitor.StartImageCapture();
    }

    public void SetupGestureRecognition()
    {
        if (monitoringStarted)
        {
            mMonitor.StopCameraMonitoring();
            monitoringStarted = false;
        }

        var setupSuccessful = mMonitor.GestureDetector.SetUpGestureRecognition();

        if (!setupSuccessful)
        {
            this.SetupGestureRecognition();
        }
        else
        {
            GameObject.Find("UI").transform.GetChild(0).GetComponent<Button>().enabled = false;
            GameObject.Find("UI").transform.GetChild(1).GetComponent<Button>().enabled = true;

            monitoringStarted = true;
            mMonitor.StartCameraMonitoring();
        }
    }

    void Delay()
    {
        delay = true;

        System.Threading.Tasks.Task.Run(() =>
        {
            System.Threading.Thread.Sleep(50);
            delay = false;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (delay)
        {
            return;
        }

        Delay();

        if (mMonitor?.GestureThresholdImg != null)
        {
            Cv2.ImShow("Gesture Threshold Img", mMonitor.GestureThresholdImg);
        }

        if (mMonitor?.GestureImg != null)
        {
            Cv2.ImShow("Gesture Recognition Img", mMonitor.GestureImg);
        }
    }

    private void mMonitor_OneFingerDetected(object sender, EventArgs e)
    {
        var light = mBedLight.GetComponent<Light>();
        mBedLight.GetComponent<Light>().enabled = !mBedLight.GetComponent<Light>().enabled;
    }

    private void mMonitor_TwoFingersDetected(object sender, EventArgs e)
    {
        var light = mKitchenLight.GetComponent<Light>();
        mKitchenLight.GetComponent<Light>().enabled = !mKitchenLight.GetComponent<Light>().enabled;
    }

    private void mMonitor_ThreeFingersDetected(object sender, EventArgs e)
    {
        var light = mRoomLight.GetComponent<Light>();
        mRoomLight.GetComponent<Light>().enabled = !mRoomLight.GetComponent<Light>().enabled;
    }

    private void mMonitor_FourFingersDetected(object sender, EventArgs e)
    {
        //var light = mRoomLight.GetComponent<Light>();
        //mRoomLight.GetComponent<Light>().color = Color.red;
    }

    private void mMonitor_FiveFingersDetected(object sender, EventArgs e)
    {
        //var light = mRoomLight.GetComponent<Light>();
        //mRoomLight.GetComponent<Light>().enabled = !mRoomLight.GetComponent<Light>().enabled;
    }
}
