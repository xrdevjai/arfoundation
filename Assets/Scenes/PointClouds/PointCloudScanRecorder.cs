using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPointCloudManager))]
public class PointCloudScanRecorder : MonoBehaviour
{
    [Header("Capture Settings")]
    public float captureInterval = 1f;

    private ARPointCloudManager pointCloudManager;

    private bool isScanning = false;
    private int imageIndex = 0;

    private string scanFolder;

    private List<FramePose> framePoses =
        new List<FramePose>();

    private List<SerializableVector3> allPoints =
        new List<SerializableVector3>();

    private void Awake()
    {
        pointCloudManager =
            GetComponent<ARPointCloudManager>();
    }

    public void StartScan()
    {
        if (isScanning)
            return;

        imageIndex = 0;

        framePoses.Clear();
        allPoints.Clear();

        scanFolder =
            Path.Combine(
                Application.persistentDataPath,
                "Scan_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        Directory.CreateDirectory(scanFolder);

        isScanning = true;

        StartCoroutine(CaptureRoutine());

        Debug.Log("SCAN STARTED");
    }

    public void StopScan()
    {
        if (!isScanning)
            return;

        isScanning = false;

        SavePoses();
        SavePointCloud();

        PlayerPrefs.SetString(
        "LastScanFolder",
        scanFolder);

        PlayerPrefs.Save();

        Debug.Log(
            "Last Scan Folder Saved: " +
            scanFolder);

        Debug.Log("SCAN FINISHED");
    }

    private IEnumerator CaptureRoutine()
    {
        while (isScanning)
        {
            yield return new WaitForEndOfFrame();

            CaptureImage();

            CollectPoints();

            yield return new WaitForSeconds(
                captureInterval);
        }
    }

    private void CaptureImage()
    {
        try
        {
            Texture2D tex =
                ScreenCapture.CaptureScreenshotAsTexture();

            if (tex == null)
                return;

            string imageName =
                $"image_{imageIndex:D4}.jpg";

            string imagePath =
                Path.Combine(
                    scanFolder,
                    imageName);

            File.WriteAllBytes(
                imagePath,
                tex.EncodeToJPG(90));

            SaveCurrentPose(imageName);

            Destroy(tex);

            imageIndex++;

            Debug.Log(
                $"Saved {imageName}");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private void SaveCurrentPose(
        string imageName)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        FramePose pose =
            new FramePose();

        pose.imageFile =
            imageName;

        pose.position =
            SerializableVector3.FromVector3(
                cam.transform.position);

        pose.rotation =
            SerializableVector3.FromVector3(
                cam.transform.eulerAngles);

        pose.fieldOfView =
            cam.fieldOfView;

        pose.imageWidth =
            Screen.width;

        pose.imageHeight =
            Screen.height;

        framePoses.Add(pose);
    }

    private void CollectPoints()
    {
        foreach (var pointCloud in pointCloudManager.trackables)
        {
            var visualizer =
                pointCloud.GetComponent
                <ARAllPointCloudPointsParticleVisualizer>();

            if (visualizer == null)
                continue;

            foreach (var point in visualizer.AllPoints)
            {
                Vector3 worldPoint =
                    pointCloud.transform.TransformPoint(
                        point.Value);

                if (float.IsNaN(worldPoint.x) ||
                    float.IsNaN(worldPoint.y) ||
                    float.IsNaN(worldPoint.z))
                    continue;

                allPoints.Add(
                    SerializableVector3
                    .FromVector3(worldPoint));
            }
        }
    }

    private void SavePoses()
    {
        PoseCollection poses =
            new PoseCollection();

        poses.frames =
            framePoses;

        string json =
            JsonUtility.ToJson(
                poses,
                true);

        File.WriteAllText(
            Path.Combine(
                scanFolder,
                "poses.json"),
            json);

        Debug.Log(
            "Poses Saved");
    }

    private void SavePointCloud()
    {
        PointCloudData data =
            new PointCloudData();

        data.scanDate =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss");

        data.points =
            allPoints;

        data.totalPoints =
            allPoints.Count;

        string json =
            JsonUtility.ToJson(
                data,
                true);

        File.WriteAllText(
            Path.Combine(
                scanFolder,
                "pointcloud.json"),
            json);

        Debug.Log(
            $"Point Cloud Saved: {allPoints.Count}");
    }
}

[Serializable]
public class PointCloudData
{
    public string scanDate;

    public int totalPoints;

    public List<SerializableVector3> points =
        new List<SerializableVector3>();
}

[Serializable]
public class FramePose
{
    public string imageFile;

    public SerializableVector3 position;

    public SerializableVector3 rotation;

    public float fieldOfView;

    public int imageWidth;

    public int imageHeight;
}

[Serializable]
public class PoseCollection
{
    public List<FramePose> frames =
        new List<FramePose>();
}

[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public static SerializableVector3 FromVector3(
        Vector3 v)
    {
        return new SerializableVector3
        {
            x = v.x,
            y = v.y,
            z = v.z
        };
    }
}