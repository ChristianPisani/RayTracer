using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Source article: 
// http://three-eyed-games.com/2018/05/03/gpu-ray-tracing-in-unity-part-1/

public class RayTracingMaster : MonoBehaviour {
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public Light DirectionalLight;

    public GameObject Sphere;
    public List<GameObject> Spheres;

    [Range(1, 10)]
    public int SamplesPerPixel = 1;

    [Range(0, 10)]
    public int Bounces = 5;

    [Range(0, 2)]
    public float AlbedoStrength = 0.5f;
    [Range(0, 1)]
    public float SpecularStrength = 0.5f;

    public int SphereSpacing = 2;
    public int SphereCountX = 5;
    public int SphereCountY = 5;
    public int SphereCountZ = 5;

    private RenderTexture _target;
    private Camera _camera;


    private void Awake()
    {
        _camera = GetComponent<Camera>();

        SpawnSpheres();
        SetShaderSpheres();
    }

    struct SphereStruct {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;        
    };


    private void SpawnSpheres()
    {
        Spheres = new List<GameObject>();
        for (int x = 0; x < SphereCountX; x++)
        {
            for (int y = 0; y < SphereCountY; y++)
            {
                for (int z = 0; z < SphereCountZ; z++)
                {
                    var sphere = Instantiate(Sphere);
                    sphere.transform.position = new Vector3(x * SphereSpacing, 1 + y * SphereSpacing, z * SphereSpacing);
                    var renderer = sphere.GetComponent<Renderer>();
                    renderer.material.color = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f));
                    sphere.transform.localScale = new Vector3(Random.Range(0.5f, 2), Random.Range(0.05f, 0.1f), Random.value);
                    sphere.GetComponent<Oscillator>().CycleTime = Random.Range(0.5f, 5f);
                    Spheres.Add(sphere);
                }
            }
        }        
    }

    private void SetShaderSpheres()
    {
        var computeBuffer = new ComputeBuffer(Spheres.Count, sizeof(float) * 10);
        computeBuffer.SetData(Spheres.Select(x => {
            var color = x.GetComponent<Renderer>().material.color;
            var metal = x.transform.localScale.z <= 0.5f;

            return new SphereStruct()
            {
                position = new Vector3(
                    x.transform.position.x,
                    x.transform.position.y,
                    x.transform.position.z),
                radius = x.transform.localScale.x,
                albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b),
                specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * x.transform.localScale.y
            };
        })
        .ToArray());

        RayTracingShader.SetBuffer(0, "spheres", computeBuffer);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        InitRenderTexture();
        RayTracingShader.SetTexture(0, "Result", _target);

        /*
          The default thread group size as defined in the Unity compute shader template is [numthreads(8,8,1)], 
          so we’ll stick to that and spawn one thread group per 8×8 pixels. 
        */
        int threadGroupsX = Mathf.CeilToInt(SkyboxTexture.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(SkyboxTexture.height / 8f);

        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(_target, destination);
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetInt("bounces", Bounces);
        RayTracingShader.SetInt("samplesPerPixel", SamplesPerPixel);

        Vector3 l = DirectionalLight.transform.forward;
        var lc = DirectionalLight.color;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        RayTracingShader.SetVector("_DirectionalLightColor", new Vector3(lc.r / 255f, lc.g / 255f, lc.b / 255f));

        SetShaderSpheres();
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            if (_target != null) _target.Release();

            _target = new RenderTexture((int)(Screen.width), (int)(Screen.height), 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            _target.useDynamicScale = false;
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }
}
