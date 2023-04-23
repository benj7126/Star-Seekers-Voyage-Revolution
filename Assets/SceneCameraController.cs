using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SceneCameraController : MonoBehaviour
{
#if UNITY_EDITOR
    public SceneView SceneView;
    public Camera Camera;
     
    private Camera ReferenceCamera;
    public bool UseReferenceCamera;
     
    public void OnEnable()
    {
        Camera = GetCamera();
        ReferenceCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        ReferenceCamera.transform.SetPositionAndRotation(Camera.transform.position, Camera.transform.rotation);
    }

    private Camera GetCamera()
    {
        SceneView = EditorWindow.GetWindow<SceneView>();
        return SceneView.camera;
    }
#endif
}