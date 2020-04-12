using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmartSightBase;
using OpenCvSharp;
using System;
using UnityEditor;
using UnityEngine.UI;
using SmartSightBase.Enumeration;

public class MonitorScript : MonoBehaviour
{

    GameObject mRoomLight;
    GameObject mBedLight;
    GameObject mKitchenLight;
    GameObject mRadio;

    private EMarker mLastMarker = EMarker.None;

    public bool mGestureRecognitionSetUp;

    private Monitor mMonitor;
    private bool delay;
    private bool monitoringStarted;
    private float angle;

    private LastUsedObject mLastUsedObject;

    // Start is called before the first frame update
    void Start()
    {
        mMonitor = new Monitor();

        mMonitor.MarkerDetected += mMonitor_MarkerDetected;
        mMonitor.MarkerAngle += mMonitor_MarkerAngle;
        mMonitor.OneFingerDetected += mMonitor_OneFingerDetected;
        mMonitor.TwoFingersDetected += mMonitor_TwoFingersDetected;
        mMonitor.ThreeFingersDetected += mMonitor_ThreeFingersDetected;
        mMonitor.FourFingersDetected += mMonitor_FourFingersDetected;
        mMonitor.FiveFingersDetected += mMonitor_FiveFingersDetected;

        mRoomLight = GameObject.Find("RoomLight");
        mBedLight = GameObject.Find("BedLight");
        mKitchenLight = GameObject.Find("KitchenLight");
        mRadio = GameObject.Find("Radio");

        GameObject.Find("SetupGRBtn").transform.GetChild(1).GetComponent<Button>().enabled = true;
        GameObject.Find("SetupGRBtn1").transform.GetChild(1).GetComponent<Button>().enabled = true;

        mMonitor.StartImageCapture();
        mMonitor.StartCameraMonitoring();
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

        GameObject.Find("LastUsedLight").GetComponent<Text>().text = mLastUsedObject.ToString();

        var lightIntensityText = GameObject.Find("LightIntensity");

        switch(mLastMarker)
        {
            case EMarker.MarkerOne:
                lightIntensityText.GetComponent<Text>().text = mBedLight.GetComponent<Light>().intensity.ToString();
                break;
            case EMarker.MarkerTwo:
                lightIntensityText.GetComponent<Text>().text = mRoomLight.GetComponent<Light>().intensity.ToString();
                break;
            case EMarker.MarkerThree:
                lightIntensityText.GetComponent<Text>().text = mRadio.GetComponent<AudioSource>().volume.ToString();
                break;
        }
    }

    IEnumerator LinerInterp(float v_start, float v_end, float duration)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            angle = Mathf.Lerp(v_start, v_end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        angle = v_end;

        switch (mLastMarker)
        {
            case EMarker.MarkerOne:
                mBedLight.GetComponent<Light>().intensity = this.NormalizeValue(angle, 180, -180);
                break;
            case EMarker.MarkerTwo:
                mRoomLight.GetComponent<Light>().intensity = this.NormalizeValue(angle, 180, -180);
                break;
            case EMarker.MarkerThree:
                mRadio.GetComponent<AudioSource>().volume = this.NormalizeValue(angle, 180, -180);
                break;
        }
    }

    private float NormalizeValue(float val, float max, float min)
    {
        return (val - min) / (max - min);
    }

    private void mMonitor_MarkerDetected(object sender, EMarker e)
    {
        mLastMarker = e;
    }

    private void mMonitor_MarkerAngle(object sender, float e)
    {
        StartCoroutine(LinerInterp(angle, e, 0.1f));

        //switch(mLastMarker)
        //{
        //    case EMarker.MarkerOne:
        //        mBedLight.GetComponent<Light>().intensity = this.NormalizeValue(angle, 180, -180);
        //        break;
        //    case EMarker.MarkerTwo:
        //        mRoomLight.GetComponent<Light>().intensity = this.NormalizeValue(angle, 180, -180);
        //        break;
        //    case EMarker.MarkerThree:
        //        mRadio.GetComponent<AudioSource>().volume = this.NormalizeValue(angle, 180, -180);
        //        break;
        //}
    }

    private void mMonitor_OneFingerDetected(object sender, EventArgs e)
    {
        var light = mBedLight.GetComponent<Light>();
        mBedLight.GetComponent<Light>().enabled = !mBedLight.GetComponent<Light>().enabled;

        mLastUsedObject = LastUsedObject.BedLight;
    }

    private void mMonitor_TwoFingersDetected(object sender, EventArgs e)
    {
        var light = mKitchenLight.GetComponent<Light>();
        mKitchenLight.GetComponent<Light>().enabled = !mKitchenLight.GetComponent<Light>().enabled;

        mLastUsedObject = LastUsedObject.KitchenLight;
    }

    private void mMonitor_ThreeFingersDetected(object sender, EventArgs e)
    {
        var light = mRoomLight.GetComponent<Light>();
        mRoomLight.GetComponent<Light>().enabled = !mRoomLight.GetComponent<Light>().enabled;

        mLastUsedObject = LastUsedObject.RoomLight;
    }

    private void mMonitor_FourFingersDetected(object sender, EventArgs e)
    {
        mLastUsedObject = LastUsedObject.Radio;
    }

    private void mMonitor_FiveFingersDetected(object sender, EventArgs e)
    {

        //var light = mRoomLight.GetComponent<Light>();
        //mRoomLight.GetComponent<Light>().enabled = !mRoomLight.GetComponent<Light>().enabled;
    }
}

public enum LastUsedObject
{
    BedLight,
    RoomLight,
    KitchenLight,
    Radio
}
