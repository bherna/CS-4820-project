using System.IO;
using UnityEngine;
using System.Collections;
using System;
using OpenCvSharp.Demo;
using UnityEngine.UI;


public class CameraCapture : MonoBehaviour
 {

    /*
    Things to make this script work:
        - need to attach this to a camera object
        - on the camera object, it needs to have a target texture attached to it
        - set the public variables    
    
    */
     public RenderTexture overviewTexture;
     GameObject OVcamera_left;
     GameObject OVcamera_right;
     private int compressSize = 1;

    //switch variable, to print either hsv or sobel
     private bool printhsv = false;
     Texture2D imageOverview;

    //ui canvas gameobject that displays the frames on scrren
    GameObject canvasObject;

     void Start()
     {
        //object for taking pictures
        OVcamera_left = GameObject.FindGameObjectWithTag("left");
        OVcamera_right = GameObject.FindGameObjectWithTag("right");

        //get the canvas object
        canvasObject = GameObject.FindGameObjectWithTag("UI");
     }

    


    void LateUpdate()
     {           
         if (Input.GetKeyUp("9"))
         {
            //capture image
            printhsv = true;
            StartCoroutine(TakeScreenShot("left"));
            StartCoroutine(TakeScreenShot("right"));

        }    
        else if (Input.GetKeyUp("8"))
         {
            //capture image
            printhsv = false;
            StartCoroutine(TakeScreenShot("left"));
            StartCoroutine(TakeScreenShot("right"));

        }
     }
 
 
     public IEnumerator TakeScreenShot(string cameraAngle)
     {
        yield return new WaitForEndOfFrame();


        Camera camOV;
        if (cameraAngle == "left")
        {
            camOV = OVcamera_left.GetComponent<Camera>();
        }
        else
        {
            camOV = OVcamera_right.GetComponent<Camera>();
        }
        RenderTexture currentRT = RenderTexture.active;    
        RenderTexture.active = camOV.targetTexture;
        camOV.Render();
        imageOverview = new Texture2D(camOV.targetTexture.width, camOV.targetTexture.height, TextureFormat.RGB24, false);
        imageOverview.ReadPixels(new Rect(0, 0, camOV.targetTexture.width, camOV.targetTexture.height), 0, 0);
        imageOverview.Apply();
        RenderTexture.active = currentRT;    
        

        //compress texture
        TextureScale.Bilinear(imageOverview, imageOverview.width/compressSize,  imageOverview.height/compressSize);

        
        //use 0
        //update the output frame on screen
        if(printhsv){

            if(cameraAngle == "left")
            {
                canvasObject.transform.Find("Left_Frame_Image").GetComponent<computeDepth>().UpdateFrame(imageOverview);
            }
            else if(cameraAngle == "right")
            {
                canvasObject.transform.Find("Right_Frame_Image").GetComponent<computeDepth>().UpdateFrame(imageOverview);
            }
        }
        else{
            
            //for each camera
            if(cameraAngle == "left")
            {
                //sobel operation
                imageOverview = sobelOperation(imageOverview);
                saveImage_original(imageOverview, "left");
            }
            else if(cameraAngle == "right")
            {
                //sobel operation
                imageOverview = sobelOperation(imageOverview);
                saveImage_original(imageOverview, "right");
            }
            

        }
        

    }



    //save image to computer// original copy, post compression
    private void saveImage_original(Texture2D imageOverview, string cameraAngle){

        byte[] bytes = imageOverview.EncodeToJPG();
        
        String filename = cameraAngle + "0" + ".jpg";
        File.WriteAllBytes(Application.dataPath + "/Backgrounds/" + filename, bytes);
    }




    //given a texture, convet texture to greyscale
    private Texture2D convertToGrey(Texture2D graph){
        
        Texture2D grayImg;

        //convert texture
        grayImg = new Texture2D(graph.width, graph.height, graph.format, false);
        Graphics.CopyTexture(graph, grayImg);
        Color32[] pixels = grayImg.GetPixels32();
        Color32[] changedPixels = new Color32[grayImg.width*grayImg.height];
    
        for (int x = 0; x < grayImg.width; x++)
        {
            for (int y = 0; y < grayImg.height; y++)
            {
                Color32 pixel = pixels[x + y * grayImg.width];
                int p = ((256 * 256 + pixel.r) * 256 + pixel.b) * 256 + pixel.g;
                int b = p % 256;
                p = Mathf.FloorToInt(p / 256);
                int g = p % 256;
                p = Mathf.FloorToInt(p / 256);
                int r = p % 256;
                float l = (0.2126f * r / 255f) + 0.7152f * (g / 255f) + 0.0722f * (b / 255f);
                Color c = new Color(l, l, l, 1);
                changedPixels[x + y * grayImg.width] = c;
            }
        }
        grayImg.SetPixels32(changedPixels);
        grayImg.Apply(false);
        return grayImg;
    }



    //just return a black filled texture, given a  texture for size reference
    private Texture2D convertToBlack(Texture2D originalTexture){

        Texture2D targetTexture = new Texture2D(originalTexture.width, originalTexture.height);

        for (int y = 0; y < originalTexture.height; y++) {
            for (int x = 0; x < originalTexture.width; x++) {
 
                targetTexture.SetPixel(x, y, Color.black);
 
            }
        }
        
        return targetTexture;
    }


    //do sobel operation, used for depth detection
    //credit:   https://epochabuse.com/csharp-sobel/
    //          https://www.youtube.com/watch?v=uihBwtPIBxM 
    private Texture2D sobelOperation(Texture2D originalTexture){

        //variables
        //grid of 9 pixels
        float _00 = 0.0f;
        float _01 = 0.0f;
        float _02 = 0.0f;
        float _10 = 0.0f;
        //float _11 = 0.0;
        float _12 = 0.0f;
        float _20 = 0.0f;
        float _21 = 0.0f;
        float _22 = 0.0f;

        //sobel x and y and total
        float totalX = 0f;
        float totalY = 0f;
        float total = 0f;

        //color temp
        Color tempColor = new Color(0,0,0);

        //for loop logic
        int filterOffset = 1;
        int height = originalTexture.height;
        int width = originalTexture.width;

        //return  texutre:
        Texture2D retTexture = new Texture2D(originalTexture.width, originalTexture.height);
        
        //return to black 
        retTexture = convertToBlack(retTexture);


        //sobel op
        //first calculate the lighting disparity between a set of nine pixels
        /*      X                       Y
            -1, 0, 1 // _00, _01, _02  // 1, 2, 1
            -2, 0, 2 // _10, _11, _12  // 0, 0, 0
            -1, 0, 1 // _20, _21, _22  //-1,-2,-1
        */
        //assuming  we start at _11
        for (int filterY = filterOffset; filterY <= height - filterOffset; filterY++)
        {
            for (int filterX = filterOffset; filterX <= width - filterOffset; filterX++)
            {   
                //X
                //left side
                _00 = -1 * originalTexture.GetPixel(filterX-1, filterY-1).grayscale;
                _10 = -2 * originalTexture.GetPixel(filterX-1, filterY).grayscale;
                _20 = -1 * originalTexture.GetPixel(filterX-1, filterY+1).grayscale;

                //right side
                _02 = 1 * originalTexture.GetPixel(filterX+1, filterY-1).grayscale;
                _12 = 2 * originalTexture.GetPixel(filterX+1, filterY).grayscale;
                _22 = 1 * originalTexture.GetPixel(filterX+1, filterY+1).grayscale;

                //calculate X
                totalX = _00 + _10 + _20 + _02 + _12 + _22;

                //Y
                //topside
                _00 = 1 * originalTexture.GetPixel(filterX-1, filterY-1).grayscale;
                _01 = 2 * originalTexture.GetPixel(filterX, filterY-1).grayscale;
                _02 = 1 * originalTexture.GetPixel(filterX+1, filterY-1).grayscale;

                //downside
                _20 = -1 * originalTexture.GetPixel(filterX-1, filterY+1).grayscale;
                _21 = -2 * originalTexture.GetPixel(filterX, filterY+1).grayscale;
                _22 = -1 * originalTexture.GetPixel(filterX+1, filterY+1).grayscale;

                //calculate Y
                totalY = _00 + _01 + _02 + _20 + _21 + _22;

                //now get hypo
                total = (float)Math.Sqrt(Math.Pow(totalX,2) + Math.Pow(totalY,2));
                Debug.Log(total.ToString("0.00"));

                //set pixel in the return texture
                tempColor = new Color(total, total, total);
                retTexture.SetPixel(filterX, filterY, tempColor);
            }
        }

        return retTexture;
    }

    

 }