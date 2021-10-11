using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;
using Obi;
using UnityEditor;

public class Circle : GeometryScript //inherits much functionaly from GeometryScript
{

    public override void Start()
    {
        base.Start();//inherited behavior - universal

        layerStartPos = new Vector3(1.296f, childExtruder.position.y, -.178f);//start layer position (hard coded for each)
        StartCoroutine(MoveExtuder(layerStartPos, .85f));//move in to start pos
        startHasRan = true;
        speed = .02f;
    }

    private new void OnEnable()//copy paste only bc need to change get components's argument
    {
        if (!canPrint && startHasRan)
        {
            Debug.Log("Cant print, obj on bed");
            gameObject.transform.GetComponent<Circle>().enabled = false;//disable self until object  off printer bed
            return;
        }
        else if (startHasRan && canPrint)//after printed once already
        {
            this.Start();
        }
        currentPrintObj = Instantiate(printedObject, printedObject.transform.position, printedObject.transform.rotation);//spawn new object to print
        currentPrintObj.SetActive(true);
        layer = currentPrintObj.transform.GetChild(0).gameObject;
    }


    new void Update()// wont be the same depending on geometry
    {
        if (layersDone < 20)//go
        {
            FrameOfPrinting();
        }
        else if (activatePrinter)//stop - active printer needed so call not made infinitely
        {
            activatePrinter = false;
            audioMotorSource.Pause();
            StartCoroutine(LowerBed());
            StartCoroutine(MoveExtuder(nozzleStartPos, .5f));
        }

    }

    public new void FrameOfPrinting()// wont be same depending on geometry or even needed neccesarily 
    {
        if (raiseBedPlate_y)//animate bed raise (unieversal despite shape)
        {
            bedNew_y = BedPlate.position.y + bedSpeed_y;
        }
        float upperPlatePrevX = childUpperPlate.position.x;
        float upperPlatePrevZ = childUpperPlate.position.z;

        float x, z;
        if (activatePrinter && !moveLocked)
        {
            x = radius * Mathf.Cos(counter);
            z = radius * Mathf.Sin(counter);
            z -= 2f;//adjust 

            counter += speed;
            int new_period = (int)(counter / (2 * System.Math.PI));
            if (new_period > period)
            {
                numPasses++;
                layersDone++;
                if (numPasses == 2)//requires multiple passes to drop height
                {
                    bedMove = true;
                    bedNew_y = BedPlate.position.y - 0.075f;
                    numPasses = 0;
                }
                period = new_period;
            }
        }
        else
        {
            x = upperPlatePrevX;
            z = upperPlatePrevZ;
        }

        if (bedNew_y >= bedFinal_y)
        {
            fluid.enabled = true;//enable once raised
            bedNew_y = bedFinal_y;
            raiseBedPlate_y = false;
            activatePrinter = true;
        }
        if (bedNew_y < 0f)//edge case - old
        {
            bedNew_y = 0f;
        }


        ///////////////////////////////////////////////////////////////// Update to new positions - should be universal despite what object is printing (as long as layer is same scale high
        BedPlate.position = new Vector3(0, bedNew_y, 0);
        if (activatePrinter)
            childExtruder.position = new Vector3(x, childExtruder.position.y, z);
        AnimatePrinter();

        //remove particles and create layer
        if (bedMove)
        {
            bedMove = false;
            foreach (Transform child in layer.transform.parent)//lower each solid layer
            {
                if (!child.name.Equals(layer.name))//not original copy
                    child.transform.position = new Vector3(child.transform.position.x, child.transform.position.y - .075f, child.transform.position.z);
            }
            GameObject newLayer = GameObject.Instantiate(layer, new Vector3(layer.transform.position.x, layer.transform.position.y - .075f, layer.transform.position.z), layer.transform.rotation);//spawn new
            newLayer.SetActive(true);//bc original is inactive
            newLayer.transform.parent = layer.transform.parent;//set parent to other parent
            newLayer.GetComponent<Renderer>().material.color = particles.particleColor;
            fluid.KillAll();
        }
    }


    //moveextruder, animateprinter, and lower bed are inhearited

}
