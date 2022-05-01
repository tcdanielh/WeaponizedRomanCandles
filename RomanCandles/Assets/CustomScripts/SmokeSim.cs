using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSim : MonoBehaviour
{
    public RenderTexture[] smokeDensity;
    public ComputeShader impulseShader;
    public static int READ = 0;
    public static int WRITE = 1;
    public Vector4 textureSize;
    public int size;
    

    //other buffer variables go here
    // Start is called before the first frame update

    void Start()
    {
        size = 256;
        textureSize = new Vector4(size, size, size, 0.0f);

        smokeDensity = new RenderTexture[2];

        for (int i = 0; i < smokeDensity.Length; i++){
            smokeDensity[i] = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            smokeDensity[i].dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            smokeDensity[i].volumeDepth = size;
            smokeDensity[i].enableRandomWrite = true;
            smokeDensity[i].Create();
        }
        //time = 0.0f;
    }

    void FixedUpdate()
    {     
        //Un/comment these to show smoke           
        float impulseRadius = 90.0f; //debug vals and call
        Vector4 impulsePosition = new Vector4(9.0f, 9.0f, 13.0f, 0.0f);
        AddDensity(impulsePosition, textureSize, impulseRadius);
    

    }

    public void AddDensity(Vector4 position, Vector4 textureSize, float radius) {
        //apply impulse
        int kernelHandle = impulseShader.FindKernel("AddDensity");
        impulseShader.SetTexture(kernelHandle, "Prev", smokeDensity[READ]);
        impulseShader.SetTexture(kernelHandle, "Result", smokeDensity[WRITE]);
        impulseShader.SetFloat("radius", radius);
        impulseShader.SetVector("position", position);
        impulseShader.SetVector("size", textureSize);
        impulseShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);
        Switch(smokeDensity);
    }

    void Switch(RenderTexture[] textures) {
        RenderTexture temp = textures[READ];
        textures[READ] = textures[WRITE];
        textures[WRITE] = temp; 
    }
}