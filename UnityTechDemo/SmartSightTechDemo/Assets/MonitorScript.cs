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

    private LastUsedLight mLastUsedLight;
    private DetectionType mDetectionType;

    // Start is called before the first frame update
    void Start()
    {
        mMonitor = new Monitor();

        mMonitor.MarkerAngle += mMonitor_MarkerAngle;
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

    public void SetupGestureRecognition(bool automatic)
    {
        if (monitoringStarted)
        {
            mMonitor.StopCameraMonitoring();
            monitoringStarted = false;
        }

        var setupSuccessful = mMonitor.GestureDetector.SetUpGestureRecognition(automatic);

        if (!setupSuccessful)
        {
            this.SetupGestureRecognition(automatic);
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

        GameObject.Find("DetectionType").GetComponent<Text>().text = mDetectionType.ToString();
        GameObject.Find("LastUsedLight").GetComponent<Text>().text = mLastUsedLight.ToString();

        var lightIntensityText = GameObject.Find("LightIntensity");

        switch(mLastUsedLight)
        {
            case LastUsedLight.BedLight:
                lightIntensityText.GetComponent<Text>().text = mBedLight.GetComponent<Light>().intensity.ToString();
                break;
            case LastUsedLight.KitchenLight:
                lightIntensityText.GetComponent<Text>().text = mKitchenLight.GetComponent<Light>().intensity.ToString();
                break;
            case LastUsedLight.RoomLight:
                lightIntensityText.GetComponent<Text>().text = mRoomLight.GetComponent<Light>().intensity.ToString();
                break;
        }
    }

    private float NormalizeValue(float val, float max, float min)
    {
        return (val - min) / (max - min);
    }

    private void mMonitor_MarkerAngle(object sender, float e)
    {
        switch(mLastUsedLight)
        {
            case LastUsedLight.BedLight:
                mBedLight.GetComponent<Light>().intensity = this.NormalizeValue(e, 180, -180);
                break;
            case LastUsedLight.KitchenLight:
                mKitchenLight.GetComponent<Light>().intensity = this.NormalizeValue(e, 180, -180);
                break;
            case LastUsedLight.RoomLight:
                mRoomLight.GetComponent<Light>().intensity = this.NormalizeValue(e, 180, -180);
                break;
        }
    }

    private void mMonitor_OneFingerDetected(object sender, EventArgs e)
    {
        if (mDetectionType == DetectionType.Marker)
        {
            return;
        }

        var light = mBedLight.GetComponent<Light>();
        mBedLight.GetComponent<Light>().enabled = !mBedLight.GetComponent<Light>().enabled;

        mLastUsedLight = LastUsedLight.BedLight;
    }

    private void mMonitor_TwoFingersDetected(object sender, EventArgs e)
    {
        if (mDetectionType == DetectionType.Marker)
        {
            return;
        }

        var light = mKitchenLight.GetComponent<Light>();
        mKitchenLight.GetComponent<Light>().enabled = !mKitchenLight.GetComponent<Light>().enabled;

        mLastUsedLight = LastUsedLight.KitchenLight;
    }

    private void mMonitor_ThreeFingersDetected(object sender, EventArgs e)
    {
        if (mDetectionType == DetectionType.Marker)
        {
            return;
        }

        var light = mRoomLight.GetComponent<Light>();
        mRoomLight.GetComponent<Light>().enabled = !mRoomLight.GetComponent<Light>().enabled;

        mLastUsedLight = LastUsedLight.RoomLight;
    }

    private void mMonitor_FourFingersDetected(object sender, EventArgs e)
    {
        //var light = mRoomLight.GetComponent<Light>();
        //mRoomLight.GetComponent<Light>().color = Color.red;
    }

    private void mMonitor_FiveFingersDetected(object sender, EventArgs e)
    {
        if (mDetectionType == DetectionType.Gesture)
        {
            mDetectionType = DetectionType.Marker;
        }
        else
        {
            mDetectionType = DetectionType.Gesture;
        }

        //var light = mRoomLight.GetComponent<Light>();
        //mRoomLight.GetComponent<Light>().enabled = !mRoomLight.GetComponent<Light>().enabled;
    }
}

public enum LastUsedLight
{
    BedLight,
    RoomLight,
    KitchenLight
}

public enum DetectionType
{
    Gesture,
    Marker
}
