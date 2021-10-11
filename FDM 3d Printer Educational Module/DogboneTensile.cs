using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DogboneTensile : GeometryScript
{
    private List<Vector3> points;
    private int moveIndex;
    public override void Start()
    {
        base.Start();//inherited behavior - universal
        points = new List<Vector3>();

        layerStartPos = new Vector3(.28f, childExtruder.position.y, 2.219f);//start layer position (hard coded for each)
        StartCoroutine(MoveExtuder(layerStartPos, .85f));//move in to start pos
        points.Add(layerStartPos);
        points.Add(new Vector3(-.329f, childExtruder.position.y, 2.219f));//front left
        points.Add(new Vector3(-.329f, childExtruder.position.y, 1.472f));//front left inner
        points.Add(new Vector3(-.284f, childExtruder.position.y, 1.167f));//front left half slope
        points.Add(new Vector3(-.15f, childExtruder.position.y, .896f));//front left half slope mid
        points.Add(new Vector3(-.103f, childExtruder.position.y, .66f));//front left mid

        points.Add(new Vector3(-.103f, childExtruder.position.y, -.66f));//front right mid
        points.Add(new Vector3(-.15f, childExtruder.position.y, -.896f));//front right half slope mid
        points.Add(new Vector3(-.284f, childExtruder.position.y, -1.167f));//front right half slope
        points.Add(new Vector3(-.329f, childExtruder.position.y,- 1.472f));//front right inner
        points.Add(new Vector3(-.329f, childExtruder.position.y, -2.219f));//front right

        points.Add(new Vector3(.329f, childExtruder.position.y, -2.219f));//back right
        points.Add(new Vector3(.329f, childExtruder.position.y, -1.472f));//back right inner
        points.Add(new Vector3(.284f, childExtruder.position.y, -1.167f));//back right half slope
        points.Add(new Vector3(.15f, childExtruder.position.y, -.896f));//back right half slope mid
        points.Add(new Vector3(.103f, childExtruder.position.y, -.66f));//back right mid

        points.Add(new Vector3(.103f, childExtruder.position.y, .66f));//back left mid
        points.Add(new Vector3(.15f, childExtruder.position.y, .896f));//back left half slope mid
        points.Add(new Vector3(.284f, childExtruder.position.y, 1.167f));//back left half slope
        points.Add(new Vector3(.329f, childExtruder.position.y, 1.472f));//back left inner
        points.Add(new Vector3(.329f, childExtruder.position.y, 2.219f));//back left


        moveIndex = 0;
        startHasRan = true;
    }

    private new void OnEnable()//copy paste only bc need to change get components's argument
    {
        if (!canPrint && startHasRan)
        {
            Debug.Log("Cant print, obj on bed");
            gameObject.transform.GetComponent<DogboneTensile>().enabled = false;//disable self until object  off printer bed
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
        if (layersDone < 3)//go
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

        if (activatePrinter && !moveLocked)
        {//fill in position here
            StartCoroutine(MoveExtuder(points[(++moveIndex) % points.Count], speed*50f));//step through list of all coordinates for a layer
            if (moveIndex % points.Count == 0)
            {//completed a layer
                bedMove = true;
                bedNew_y = BedPlate.position.y - 0.075f;
                layersDone++;
            }
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
