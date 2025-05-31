using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteAlways]
public class WaterReflection : MonoBehaviour
{
    [Header("Setup")]
    //public Camera mainCamera => SceneView.lastActiveSceneView?.camera;
    public Camera mainCamera => Camera.main;
    public GameObject waterPlane;

    [Header("Reflection Texture")]
    public int textureSize = 512;
    private RenderTexture reflectionTexture;

    private Camera reflectionCamera;
    private static readonly string ReflectionTexName = "_ReflectionTex";
    
    public Texture Texture => reflectionTexture;

    void OnEnable()
    {
        CreateReflectionCamera();
        CreateReflectionTexture();
    }

    void OnDisable()
    {
        Cleanup();
    }

    void LateUpdate()
    {
        if (!mainCamera || !waterPlane || !reflectionCamera)
            return;

        UpdateReflectionCamera();
        reflectionCamera.Render();

        Shader.SetGlobalTexture(ReflectionTexName, reflectionTexture);
    }

    void CreateReflectionTexture()
    {
        if (reflectionTexture && (reflectionTexture.width != textureSize || reflectionTexture.height != textureSize))
        {
            reflectionTexture.Release();
            DestroyImmediate(reflectionTexture);
        }

        if (!reflectionTexture)
        {
            reflectionTexture = new RenderTexture(textureSize, textureSize, 32, DefaultFormat.HDR);
            reflectionTexture.name = "WaterReflectionTex";
            reflectionTexture.hideFlags = HideFlags.DontSave;
            reflectionTexture.Create();
        }

        if (reflectionCamera)
            reflectionCamera.targetTexture = reflectionTexture;
    }

    void CreateReflectionCamera()
    {
        if (reflectionCamera == null)
        {
            GameObject camGO = new GameObject("ReflectionCamera", typeof(Camera));
            camGO.transform.SetParent(this.transform);
            camGO.hideFlags = HideFlags.HideAndDontSave;
            reflectionCamera = camGO.GetComponent<Camera>();
            reflectionCamera.enabled = false;
        }
    }

    void UpdateReflectionCamera()
    {
        reflectionCamera.CopyFrom(mainCamera);
        reflectionCamera.targetTexture = reflectionTexture;

        Vector3 planeNormal = waterPlane.transform.up;
        Vector3 planePos = waterPlane.transform.position;

        Vector3 camPos = mainCamera.transform.position;
        float distance = Vector3.Dot(planeNormal, camPos - planePos);
        Vector3 reflectedPos = camPos - 2f * distance * planeNormal;

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camUp = mainCamera.transform.up;

        Vector3 reflectedForward = Vector3.Reflect(camForward, planeNormal);
        Vector3 reflectedUp = Vector3.Reflect(camUp, planeNormal);

        reflectionCamera.transform.position = reflectedPos;
        reflectionCamera.transform.rotation = Quaternion.LookRotation(reflectedForward, reflectedUp);

        // Clip plane to avoid artifacts above water
        Vector4 clipPlane = CameraSpacePlane(reflectionCamera, planePos, planeNormal);
        Matrix4x4 projection = mainCamera.CalculateObliqueMatrix(clipPlane);
        reflectionCamera.projectionMatrix = projection;
    }

    Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal)
    {
        Vector3 offsetPos = pos + normal * -0.05f;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    void Cleanup()
    {
        if (reflectionCamera)
            DestroyImmediate(reflectionCamera.gameObject);
        if (reflectionTexture)
            DestroyImmediate(reflectionTexture);
    }
}

[CustomEditor(typeof(WaterReflection))]
public class WaterReflectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        var target = this.target as WaterReflection;
        var rect = GUILayoutUtility.GetRect(256, 256);
        EditorGUI.DrawPreviewTexture( new Rect(rect.x,rect.y,256,256), target.Texture);
    }
}