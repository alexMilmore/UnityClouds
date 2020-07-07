using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudGraphics : MonoBehaviour
{
    // ALL CONSTANTS
    Renderer m_Renderer;
    public ComputeShader compute;
    RenderTexture result;
    RenderTexture bumpMap;
    RenderTexture smoothMap;

    [Header("Textures")]
    public Texture2D noiseTex;
    public Texture2D shapeTex;

    [Header("Texture settings")]
    public int pixWidth;
    public int pixHeight;
    [Range(0, 200)]
    public int xOffset;
    [Range(0, 200)]
    public int yOffset;
    [Range(0.0f, 1.0f)]

    [Header("Cloud shape")]
    public float NoiseWeight;
    [Range(0.0f, 1.0f)]
    public float shapeWeight;

    [Header("Normals")]
    public bool largerSobel;
    [Range(0.0f, 100.0f)]
    public float sharpness;
    [Range(1, 20)]
    public int sampleDist;
    public bool blur;
    [Range(1, 64)]
    public int blurDist = 1;
    public bool viewNormals;

    [Header("Lighting")]
    public bool lightObject;
    public bool celShading;

    // delegete functon for generating textures
    public delegate Color DelegateDeclaration(int a, int b, int c, int d, int e);

    // kernels from compute shader
    private int kernel;
    private int normalKernel;
    private int normalKernel5;
    private int blurKernel;

    // float for animation
    private float timeShift;


    // ALL FUNCTIONS
    // Start is called before the first frame update
    void Start()
    {
        // get kernel id
        kernel = compute.FindKernel("CreateSprite");
        normalKernel = compute.FindKernel("CalcNormals");
        normalKernel5 = compute.FindKernel("CalcNormals5");
        blurKernel = compute.FindKernel("GaussianBlur");

        // create textures
        result = new RenderTexture(pixHeight, pixWidth, 24);
        result.enableRandomWrite = true;
        result.Create();
        bumpMap = new RenderTexture(pixHeight, pixWidth, 24);
        bumpMap.enableRandomWrite = true;
        bumpMap.Create();
        smoothMap = new RenderTexture(pixHeight, pixWidth, 24);
        smoothMap.enableRandomWrite = true;
        smoothMap.Create();

        // create delegates (this is for passing functions through the gentex Function)
        DelegateDeclaration noiseCol = PerlinNoiseColour;
        DelegateDeclaration shapeCol = ShapeColour;

        // THIS SHIT CHOKES THE COMPUTER THE FUCK OUT, NEED TO FIX
        // generate noise texture
        //noiseTex = new Texture2D(pixWidth * 2, pixHeight * 2);
        //noiseTex = GenTex(noiseTex, noiseCol);

        // generate basic shape
        //shapeTex = new Texture2D(pixWidth, pixHeight);
        //shapeTex = GenTex(shapeTex, shapeCol);

        // set textures in compute shader
        compute.SetTexture(kernel, "Result", result);
        compute.SetTexture(kernel, "Noise", noiseTex);
        compute.SetTexture(kernel, "Shape", shapeTex);

        // run compute shader
        compute.Dispatch(kernel, pixWidth / 8, pixHeight / 8, 1);

        // get renderer
        m_Renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
      if (CheckVisible()) {
        // set offset values in noise texture
        float[] offset = { xOffset, yOffset };
        compute.SetFloats(Shader.PropertyToID("offset"), offset);
        compute.SetFloat(Shader.PropertyToID("noiseWeight"), NoiseWeight);
        compute.SetFloats(Shader.PropertyToID("shapeWeight"), shapeWeight);
        timeShift = Mathf.Sin(Time.time);
        compute.SetFloats(Shader.PropertyToID("timeShift"), timeShift);
        compute.SetFloat(Shader.PropertyToID("sharpness"), sharpness);
        compute.SetInt(Shader.PropertyToID("dist"), sampleDist);

        // run compute shader
        compute.Dispatch(kernel, pixWidth / 8, pixHeight / 8, 1);

        ComputeNormals();

        // Mean blur to smooth out normals
        if (blur == true)
        {
            SmoothNormals(blurDist);
        }

        // Send to renderer
        if (viewNormals == false)
        {
            m_Renderer.material.SetTexture("_MainTex", result);
            m_Renderer.material.SetTexture("_BumpMap", bumpMap);
        }
        else
        {
            m_Renderer.material.SetTexture("_MainTex", bumpMap);
            Texture2D blank = new Texture2D(bumpMap.width, bumpMap.height);
            m_Renderer.material.SetTexture("_BumpMap", blank);
        }
      }
    }

    bool CheckVisible()
    {
        GameObject player = GameObject.Find("Player");
        Vector3 distance = transform.position - player.transform.position;
        if ((Mathf.Abs(distance.y) < 7 + (transform.localScale.y / 2)) && (Mathf.Abs(distance.x) < 12 + (transform.localScale.x / 2)))
        {
            return true;
        }
        return false;
    }

    void ComputeNormals()
    {
        if (largerSobel == true)
        {
            // send computed image to shader to calculate normals
            compute.SetTexture(normalKernel5, "Result", result);
            compute.SetTexture(normalKernel5, "BumpMap", bumpMap);

            // run compute shader
            compute.Dispatch(normalKernel5, pixWidth / 8, pixHeight / 8, 1);
        }
        else
        {
            // send computed image to shader to calculate normals
            compute.SetTexture(normalKernel, "Result", result);
            compute.SetTexture(normalKernel, "BumpMap", bumpMap);

            // run compute shader
            compute.Dispatch(normalKernel, pixWidth / 8, pixHeight / 8, 1);
        }
    }

    void SmoothNormals(int distance)
    {
        compute.SetTexture(blurKernel, "BumpMap", bumpMap);
        compute.SetTexture(blurKernel, "SmoothMap", smoothMap);
        compute.SetInt(Shader.PropertyToID("blurSize"), distance);

        // run compute shader
        compute.Dispatch(blurKernel, pixWidth / 8, pixHeight / 8, 1);
    }

    int FindIndex(int x, int y, int width)
    {
        return y * width + x;
    }

    Texture2D GenTex(Texture2D tex, DelegateDeclaration handler)
    {
        Color[] colourMem = new Color[tex.height * tex.width];
        int noiseScale = 10;
        int y = 0;
        while (y < tex.height)
        {
            int x = 0;
            while (x < tex.width)
            {
                colourMem[FindIndex(x, y, tex.width)] = handler(x, y, tex.width, tex.height, noiseScale);
                x++;
            }
            y++;
        }

        // Copy the pixel data to the texture and load it into the GPU.
        tex.SetPixels(colourMem);
        tex.Apply();
        return tex;
    }

    Color PerlinNoiseColour(int x, int y, int noiseWidth, int noiseHeight, int noiseScale)
    {
        float xCoord = (float)x / noiseWidth * noiseScale;
        float yCoord = (float)y / noiseHeight * noiseScale;
        float sample = Mathf.PerlinNoise(xCoord, yCoord);
        return new Color(sample, sample, sample);
    }

    Color ShapeColour(int x, int y, int width, int height, int const1)
    {
        float angle = Mathf.Sin((float)x / width * Mathf.PI) * Mathf.Sin((float)y / height * Mathf.PI);
        return new Color(angle, angle, angle);
    }
}
