using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmartSightBase;
using OpenCvSharp;
using System;

public class MonitorScript : MonoBehaviour
{

    GameObject mRoomLight;

    private Monitor mMonitor;
    private bool delay;

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

        mMonitor.StartCameraMonitoring();
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
            Cv2.ImShow("TestGesture", mMonitor.GestureThresholdImg);
        }

        if (mMonitor?.GestureImg != null)
        {
            Cv2.ImShow("TestGesture2", mMonitor.GestureImg);
        }
    }

    private void mMonitor_OneFingerDetected(object sender, EventArgs e)
    {
        var light = mRoomLight.GetComponent<Light>();
        mRoomLight.GetComponent<Light>().color = Color.blue;
    }

    private void mMonitor_TwoFingersDetected(object sender, EventArgs e)
    {
        var light = mRoomLight.GetComponent<Light>();
        mRoomLight.GetComponent<Light>().color = Color.green;
    }

    private void mMonitor_ThreeFingersDetected(object sender, EventArgs e)
    {
        var light = mRoomLight.GetComponent<Light>();
        mRoomLight.GetComponent<Light>().color = Color.yellow;
    }

    private void mMonitor_FourFingersDetected(object sender, EventArgs e)
    {
        var light = mRoomLight.GetComponent<Light>();
        mRoomLight.GetComponent<Light>().color = Color.red;
    }

    private void mMonitor_FiveFingersDetected(object sender, EventArgs e)
    {
        var light = mRoomLight.GetComponent<Light>();
        mRoomLight.GetComponent<Light>().enabled = !mRoomLight.GetComponent<Light>().enabled;
    }
}
