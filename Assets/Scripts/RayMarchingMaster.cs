using UnityEngine;

// Source article: 
// http://jamie-wong.com/2016/07/15/ray-marching-signed-distance-functions/#the-raymarching-algorithm

public class RayMarchingMaster : MonoBehaviour {
    public ComputeShader RayMarchingShader;    
    public Texture SkyboxTexture;
    public Light DirectionalLight;
    [Range(1, 16)]
    public float SoftShadowStrength = 2;

    [Range(0, 1)]
    public float Specular = 0.1f;

    [Range(0, 2f)]
    public float GlowStrength = 1f;
    [Range(0, 200)]
    public int GlowCutoff = 0;
    public Color GlowColor = Color.magenta;

    [Range(0.0001f, 1f)]
    public float SurfaceDistance = 0.001f;

    public Color Albedo = Color.gray;

    private RenderTexture _target;
    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        InitRenderTexture();
        RayMarchingShader.SetTexture(0, "Result", _target);

        /*
          The default thread group size as defined in the Unity compute shader template is [numthreads(8,8,1)], 
          so we’ll stick to that and spawn one thread group per 8×8 pixels. 
        */
        int threadGroupsX = Mathf.CeilToInt(SkyboxTexture.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(SkyboxTexture.height / 8f);

        RayMarchingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(_target, destination);
    }

    private void SetShaderParameters()
    {
        RayMarchingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayMarchingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayMarchingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);

        Vector3 l = DirectionalLight.transform.forward;
        var lc = DirectionalLight.color;
        RayMarchingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        RayMarchingShader.SetVector("_DirectionalLightColor", new Vector3(lc.r / 255f, lc.g / 255f, lc.b / 255f));
        RayMarchingShader.SetFloat("_SoftShadowStrength", SoftShadowStrength);

        RayMarchingShader.SetFloat("_Specular", Specular);
        RayMarchingShader.SetVector("_Albedo", new Vector3(Albedo.r, Albedo.g, Albedo.b));
        RayMarchingShader.SetFloat("_Time", Time.time);

        RayMarchingShader.SetVector("_GlowColor", new Vector3(GlowColor.r, GlowColor.g, GlowColor.b));
        RayMarchingShader.SetFloat("_GlowStrength", GlowStrength);
        RayMarchingShader.SetInt("_GlowCutoff", GlowCutoff);
        RayMarchingShader.SetFloat("_SurfaceDistance", SurfaceDistance);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            if (_target != null) _target.Release();

            _target = new RenderTexture((int)(SkyboxTexture.width), (int)(SkyboxTexture.height), 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            _target.useDynamicScale = false;
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }
}
