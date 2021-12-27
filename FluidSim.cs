using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class FluidSim : MonoBehaviour
{
    public int width = 256;
    public int height = 256;
    public int numSolvingSteps = 5;
    public float k = 0.1f;
    public float fadeTime;

    private MeshRenderer meshRenderer;
    private RenderTexture renderTexture;
    public ComputeShader computeShader;
    private ComputeBuffer computeBuffer;

    struct FluidBox
    {
        public float density;
        public float velocityX;
        public float velocityY;
        
        public static FluidBox operator +(FluidBox a, FluidBox b)
        {
            FluidBox result = new FluidBox();
            result.density = a.density + b.density;
            result.velocityX = a.velocityX + b.velocityX;
            result.velocityY = a.velocityY + b.velocityY;

            return result;
        }

        public static FluidBox operator *(FluidBox a, float b)
        {
            FluidBox result = new FluidBox();
            result.density = a.density * b;
            result.velocityX = a.velocityX * b;
            result.velocityY = a.velocityY * b;

            return result;
        }

        public static FluidBox operator /(FluidBox a, float b)
        {
            return a * (1f / b);
        }

        public static FluidBox operator -(FluidBox a, FluidBox b)
        {
            return a + (b * -1f);
        }

        public static FluidBox Lerp(FluidBox a, FluidBox b, float t)
        {
            return a * (1 - t) + b * t;
        }

        public Vector2 VelocityVector()
        {
            return new Vector2(velocityX, velocityY);
        }
    }

    private FluidBox[,] simMatrix;
    
    void Start()
    {
        simMatrix = new FluidBox[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // simMatrix[y, x].density = 1;

                float noise = Mathf.PerlinNoise(((float)x)/width, ((float)y)/height) * Mathf.PI * 2;

                float len = Mathf.PerlinNoise( ((float)y)/height, ((float)x)/width) * 5;

                noise = ((Mathf.Cos(((float)x) / 20) + Mathf.Sin(((float)y) / 20)) + 2) * Mathf.PI;
                // len = 5;
                
                simMatrix[y, x].velocityX = Mathf.Cos(noise) * len;
                simMatrix[y, x].velocityY = Mathf.Sin(noise) * len;
                // Debug.Log(simMatrix[y, x].velocityX + " " + simMatrix[y, x].velocityY);


                simMatrix[y, x].velocityX = 0f;
                simMatrix[y, x].velocityY = 0;

                // simMatrix[y, x].velocityX = 100;
                // simMatrix[y, x].velocityY = 100;
            }
        }

        



        meshRenderer = GetComponent<MeshRenderer>();
        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)    {
            enableRandomWrite = true
        };

        unsafe
        {
            computeBuffer = new ComputeBuffer(width * height, 12); 
        }
        
    }

    private void Update()
    {

        ControlSim();
        
        Diffuse();
        Advection();
        ClearDivergence();
        
        UpdateMaterial();
    }

    private Vector2 prevMousePos;

    private void ControlSim()
    {
        // Adding fluid

        Vector2 currMousePos = Vector2.zero;
        currMousePos.x = Mathf.Clamp((Input.mousePosition.x / Screen.width), -0.0F, 1.0F);
        currMousePos.y = Mathf.Clamp((Input.mousePosition.y / Screen.height), -0.0F, 1.0F);

        if (Input.GetMouseButton(0))
        {
            int matX = (int)Mathf.Clamp(currMousePos.x * width, 0, width - 1);
            int matY = (int)Mathf.Clamp(currMousePos.y * height, 0, height - 1);

            Vector2 velo = -(currMousePos - prevMousePos) * 1000000;

            simMatrix[matY, matX].density = 250;
            simMatrix[matY, matX].velocityX = velo.x;
            simMatrix[matY, matX].velocityY = velo.y;
        }

        prevMousePos = currMousePos;

        // External forces and fade
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // simMatrix[y, x].velocityY += 3 * Time.deltaTime;
                // simMatrix[y, x].density = Mathf.Lerp(simMatrix[y, x].density, 0, Time.deltaTime * fadeTime);
                simMatrix[y,x] = FluidBox.Lerp(simMatrix[y,x], new FluidBox(), fadeTime * Time.deltaTime);
                
            }
        }
    }

    private void ControlSim2()
    {
        // simMatrix[10, 64].density = 20;
        // simMatrix[10, 64].velocityX = 0;
        // simMatrix[10, 64].velocityY = 1000;
        
        // Adding fluid

        Vector2 currMousePos = Vector2.zero;
        currMousePos.x = Mathf.Clamp((Input.mousePosition.x / Screen.width), -0.0F, 1.0F);
        currMousePos.y = Mathf.Clamp((Input.mousePosition.y / Screen.height), -0.0F, 1.0F);

        if (Input.GetMouseButton(0))
        {
            int matX = (int)Mathf.Clamp(currMousePos.x * width, 0, width - 1);
            int matY = (int)Mathf.Clamp(currMousePos.y * height, 0, height - 1);

            Vector2 velo = -(currMousePos - prevMousePos) * 1000000;

            simMatrix[matY, matX].density = 100;
            simMatrix[matY, matX].velocityX = velo.x;
            simMatrix[matY, matX].velocityY = velo.y;
        }

        prevMousePos = currMousePos;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // simMatrix[y, x].velocityY += 3 * Time.deltaTime;
                // simMatrix[y, x].density = Mathf.Lerp(simMatrix[y, x].density, 0, Time.deltaTime * fadeTime);
                
                
                simMatrix[y, x] = FluidBox.Lerp(simMatrix[y, x], new FluidBox(), fadeTime * Time.deltaTime);

                float angle = (float)Perlin.perlin(((float)x)/ width, ((float)y)/height, Time.time / 6) * Mathf.PI * 4;
                
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10;
                
                simMatrix[y, x].velocityX += dir.x * Time.deltaTime;
                simMatrix[y, x].velocityY += dir.y * Time.deltaTime;
                
                
                angle = (float)Perlin.perlin(((float)x)/ width * 10, ((float)y)/height * 10, Time.time / 3) * Mathf.PI * 4;
                
                dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 20;
                
                simMatrix[y, x].velocityX += dir.x * Time.deltaTime;
                simMatrix[y, x].velocityY += dir.y * Time.deltaTime;
            }
        }
    }

    private void Diffuse()
    {
        FluidBox[,] cmat = simMatrix;
        
        FluidBox[,] nmat = new FluidBox[height, width];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                nmat[y, x] = cmat[y, x];
            }
        } 
        
        FluidBox[,] nMatNext = new FluidBox[height, width];
        
        
        for (int step = 0; step < numSolvingSteps; step++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Perform density simulation for current box    

                    FluidBox right = GetBox(x + 1, y, nmat);
                    FluidBox left = GetBox(x - 1, y, nmat);
                    FluidBox up = GetBox(x, y - 1, nmat);
                    FluidBox down = GetBox(x, y + 1, nmat);

                    FluidBox innerSum = (right + left + up + down) / 4f;
                    innerSum = innerSum * k;

                    FluidBox n_xy = (cmat[y, x] + innerSum) / (1f + k);

                    nMatNext[y, x] = n_xy;
                }
            }

            nmat = nMatNext;
            nMatNext = new FluidBox[height, width];
        }

        simMatrix = nmat;
    }

    private void Advection()
    {
        FluidBox[,] newMat = new FluidBox[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                FluidBox? currentBox = GetBox(x, y);

                float xPos = x;
                float yPos = y;

                float xVelo = currentBox.Value.velocityX * Time.deltaTime;
                float yVelo = currentBox.Value.velocityY * Time.deltaTime;
                float density = currentBox.Value.density;

                xPos = xPos - xVelo;
                yPos = yPos - yVelo;

                int xFloor = (int)Math.Floor(xPos);
                int yFloor = (int)Math.Floor(yPos);

                float xFrac = xPos - xFloor;
                float yFrac = yPos - yFloor;

                int xCeil = xFloor + 1;
                int yCeil = yFloor + 1;


                xFloor = (int)Mathf.Clamp(xFloor, 0, width - 1);
                xCeil = (int)Mathf.Clamp(xCeil, 0, width - 1); 
                yFloor = (int)Mathf.Clamp(yFloor, 0, height - 1);
                yCeil = (int)Mathf.Clamp(yCeil, 0, height - 1);


                Vector2 vLerp1 = Vector2.Lerp(GetVelocity(xFloor, yFloor), GetVelocity(xCeil, yFloor), xFrac);
                Vector2 vLerp2 = Vector2.Lerp(GetVelocity(xFloor, yCeil), GetVelocity(xCeil, yCeil), xFrac);
                Vector2 vFinal = Vector2.Lerp(vLerp1, vLerp2, yFrac);

                FluidBox xLerp1 = FluidBox.Lerp(simMatrix[yFloor, xFloor], simMatrix[yFloor, xCeil], xFrac);
                FluidBox xLerp2 = FluidBox.Lerp(simMatrix[yCeil, xFloor], simMatrix[yCeil, xCeil], xFrac);
                FluidBox result = FluidBox.Lerp(xLerp1, xLerp2, yFrac);

                newMat[y, x] = result;
            }
        }

        simMatrix = newMat;
    }

    private void ClearDivergence()
    {
        // Compute delta velocity
        float[,] deltaVelo = new float[height, width];
        float[,] pCurr = new float[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 right = GetBox(x + 1, y, simMatrix).VelocityVector();
                Vector2 left = GetBox(x - 1, y, simMatrix).VelocityVector();
                Vector2 up = GetBox(x, y - 1, simMatrix).VelocityVector();
                Vector2 down = GetBox(x, y + 1, simMatrix).VelocityVector();

                pCurr[y, x] = 0;
                deltaVelo[y, x] = ((right.x - left.x) + (down.y - up.y)) / 2;
            }
        }

        // Compute p(x,y)


        float[,] pNext = new float[height, width];
        for (int step = 0; step < numSolvingSteps; step++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float right = GetValue(x + 1, y, pCurr);
                    float left = GetValue(x - 1, y, pCurr);
                    float up = GetValue(x, y - 1, pCurr);
                    float down = GetValue(x, y + 1, pCurr);

                    float val = ((right + left + up + down) - deltaVelo[y, x]) / 4f;
                    pNext[y, x] = val;
                }
            }

            pCurr = pNext;
            pNext = new float[height, width];
        }
        
        // Compute divergence and subtract
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float right = GetValue(x + 1, y, pCurr);
                float left = GetValue(x - 1, y, pCurr);
                float up = GetValue(x, y - 1, pCurr);
                float down = GetValue(x, y + 1, pCurr);

                float veloX = (right - left) / 2;
                float veloY = (down - up) / 2;

                Vector2 divergence = new Vector2(veloX, veloY);
                simMatrix[y, x].velocityX = simMatrix[y, x].velocityX - divergence.x;
                simMatrix[y, x].velocityY = simMatrix[y, x].velocityY - divergence.y;
            }
        }

    }

    private float GetValue(int x, int y, float[,] mat)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return 0;
        }

        return mat[y, x];
    }
    
    

    private FluidBox GetBox(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return new FluidBox();
        }

        return simMatrix[y, x];
    }
    
    private FluidBox GetBox(int x, int y, FluidBox[,] mat)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return new FluidBox();
        }

        return mat[y, x];
    }

    private float GetDensity(int x, int y)
    {
        FluidBox? box = GetBox(x, y);

        if (box.HasValue)
        {
            return box.Value.density;
        }

        return 0;
    }

    private Vector2 GetVelocity(int x, int y)
    {
        FluidBox? box = GetBox(x, y);

        if (box.HasValue)
        {
            return new Vector2(box.Value.velocityX, box.Value.velocityY);
        }

        return Vector2.zero; 
    }

    private void UpdateMaterial()
    {
        computeShader.SetTexture(0, "Result", renderTexture);
        
        computeBuffer.SetData(simMatrix);
        computeShader.SetBuffer(computeShader.FindKernel("CSMain"), "fluidData", computeBuffer);
        
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
        
        computeShader.Dispatch(0, width / 8, height / 8, 1);

        meshRenderer.material.SetTexture("_Texture", renderTexture);
    }
}