using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class OverlayCameraStackFinder : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var URPData = GetComponent<Camera>().GetUniversalAdditionalCameraData();

        URPData.cameraStack.AddRange(Camera.allCameras.Where(camera => camera.GetUniversalAdditionalCameraData().renderType != CameraRenderType.Base));
    }
}
