using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;
using Obi;
using UnityEditor;

//Cooper's (my) additions surrounded by // // when needed to distinguish
public class GeometryScript : MonoBehaviour
{
    [Header("The rest are only public for inheritability/ accessibility."), Space(1),Header ("Printed Object, Emitter, and  Audio Motor Source."), Space(1), Header("The only fields that should be changed in the editor are: [Child Upper Plate - rot idler 2]," )]
    public Transform childUpperPlate;
    public Transform childExtruder;
    public Transform BedPlate;
    public Transform belt1MiddleR, belt2MiddleR, belt1MiddleL, belt2MiddleL, beltFrontR, beltFrontL, beltBackR, beltBackL;//for belt animation
    public Transform beltFrontLPoint, beltFrontLPoint2;//left side, used for rightside too
    public Transform beltBackLPoint, beltBackLPoint2;// same idea, where "2" is the point of attahcment to the carridge "upperplate" 
    public Transform eCenterPt, rEdgePt, lEdgePt; //for middle belts
    public Transform gear1, gear2, rotBelt2_Part2, rotBelt1_Part1, rotBelt2_Part3, rotBelt1_Part4, rotIdlerFR1, rotIdlerFR2, rotIdlerFL1, rotIdlerFL2; //idler and gear rotation

    public float counter;
    public float speed;
    public bool activatePrinter;
    public int period;
    public float bedSpeed_y;
    public float bedFinal_y;
    public bool raiseBedPlate_y;
    public float bedNew_y;
    public float radius;

    public float diffBMFtoE, diffBMBtoE;//middle belts to extruder - movement simply an offset of extruders

    public  bool bedMove;
    public GameObject printedObject;//the og, to be duplicated. prefab doesnt work with parenting method
    public GameObject layer;
    public int numPasses;
    public ObiParticleRenderer particles;
    public GameObject emitter;
    public ObiEmitter fluid;
    public int layersDone;
    public bool canPrint;
    public GameObject currentPrintObj;
    public bool startHasRan;
    public AudioSource audioMotorSource;
    public Vector3 childUpperOG, childExtruderOG, wholeOGPos;
    public float bedOG;
    public float rotPos1, rotPos2, rotPos1ML2MR, rotPos1MR2ML;//position in euler angle 
    public Transform nozzle;// its LOCAL POSITION 0,0,0  is the correct spot of the nozzle to easier code paths as global posis relative to center of printbed
    public Vector3 nozzleStartPos, layerStartPos;
    public bool moveLocked = false;

    public virtual void Start()
    {
        canPrint = true;
        bedMove = false;
        raiseBedPlate_y = true;
        bedSpeed_y = 0.01f;
        bedFinal_y = 5.4f;
        period = 0;
        radius = 2f;
        speed = 0.01f;
        activatePrinter = false;
        counter = 0f;
        numPasses = 0;
        emitter = GameObject.Find("Emitter");
        fluid = emitter.GetComponent<ObiEmitter>();
        particles = emitter.GetComponent<ObiParticleRenderer>();
        layersDone = 0;

        if (wholeOGPos != null)//only assigned once per runtime
        {
            wholeOGPos = gameObject.transform.position;
            nozzle = childExtruder.Find("Nozzle");
            childUpperOG = childUpperPlate.transform.position;
            childExtruderOG = childExtruder.transform.position;
            bedOG = BedPlate.position.y;
        }
        else //reset relative positions  - resets positions between local and global 
        {
            gameObject.transform.position = wholeOGPos;
            childUpperPlate.transform.position = childUpperOG;
            childExtruder.transform.position = childExtruderOG;
            BedPlate.position = new Vector3(BedPlate.position.x, bedOG, BedPlate.position.z);
        }
        nozzleStartPos = new Vector3(nozzle.position.x, nozzle.position.y, nozzle.position.z);

        //used to keep middle pieces moving.constant offset
        diffBMFtoE = belt1MiddleR.position.x - childExtruder.position.x;
        diffBMBtoE = belt2MiddleR.position.x - childExtruder.position.x;

        //layerStartPos = new Vector3(1.296f, childExtruder.position.y, -.178f);//start layer position (hard coded for circle)
        //StartCoroutine(MoveExtuder(layerStartPos, .85f));//move in to start pos
        //startHasRan = true;
    }



    public void OnEnable() { }//must be overriden
    //{
    //    if (!canPrint && startHasRan)
    //    {
    //        Debug.Log("Cant print, obj on bed");
    //        gameObject.transform.GetComponent<Circle>().enabled = false;//disable self until object  off printer bed
    //        return;
    //    }
    //    else if (startHasRan && canPrint)//after printed once already
    //    {
    //        this.Start();
    //    }
    //    currentPrintObj = Instantiate(printedObject, printedObject.transform.position, printedObject.transform.rotation);//spawn new object to print
    //    currentPrintObj.SetActive(true);
    //    layer = currentPrintObj.transform.GetChild(0).gameObject;
    //}


    public void Update() { }
    //{
    //    if (layersDone < 20)//go
    //    {
    //        FrameOfPrinting();
    //    }
    //    else if (activatePrinter)//stop - active printer needed so call not made infinitely
    //    {
    //        activatePrinter = false;
    //        fluid.speed = 0f;
    //        audioMotorSource.Pause();
    //        StartCoroutine(LowerBed());
    //        StartCoroutine(MoveExtuder(nozzleStartPos, .5f));
    //    }

    //}

    public void FrameOfPrinting() { }

    public void AnimatePrinter()//Dynamically moves belts and pullys as a function of X and Z plane printhead location.
    {   //Based on origin being printhead in center of print area, all gears at 0 degrees and rotating clockwise (from above)as positive
        //    ^   -x                         //https://corexy.com/theory.html
        //    |
        //    L __ >  z
        //
        //[O] 1         [O] 2         (motors top down illustration)

        childUpperPlate.position = new Vector3(childExtruder.position.x, childUpperPlate.transform.position.y, childUpperPlate.transform.position.z);//upper is fn of extruder pos
        ////middle belt updates x is offset, z is halfway
        belt2MiddleR.position = new Vector3(childExtruder.position.x + diffBMBtoE, belt2MiddleR.position.y, (eCenterPt.position.z + rEdgePt.position.z) / 2f);
        belt1MiddleL.position = new Vector3(childExtruder.position.x + diffBMBtoE, belt1MiddleL.position.y, (eCenterPt.position.z + lEdgePt.position.z) / 2f);
        belt1MiddleR.position = new Vector3(childExtruder.position.x + diffBMFtoE, belt1MiddleR.position.y, (eCenterPt.position.z + rEdgePt.position.z) / 2f);
        belt2MiddleL.position = new Vector3(childExtruder.position.x + diffBMFtoE, belt2MiddleL.position.y, (eCenterPt.position.z + lEdgePt.position.z) / 2f);
        //scale
        belt2MiddleR.localScale = new Vector3((rEdgePt.position.z - eCenterPt.position.z), belt2MiddleR.localScale.y, belt2MiddleR.localScale.z);
        belt1MiddleL.localScale = new Vector3((eCenterPt.position.z - lEdgePt.position.z), belt1MiddleL.localScale.y, belt1MiddleL.localScale.z);
        belt1MiddleR.localScale = new Vector3((rEdgePt.position.z - eCenterPt.position.z), belt1MiddleR.localScale.y, belt1MiddleR.localScale.z);
        belt2MiddleL.localScale = new Vector3((eCenterPt.position.z - lEdgePt.position.z), belt2MiddleL.localScale.y, belt2MiddleL.localScale.z);

        //////////////////idlers and gear - all series 1 ( RHS motor) and all series 2 (LHS motor) from front
        rotPos2 = (nozzle.position.x + nozzle.position.z) / (-.753f / 2f);//diameter gear to radius b/c s = r * theta
        rotPos1 = (nozzle.position.x - nozzle.position.z) / (.753f / 2f);
        rotPos1ML2MR = nozzle.position.z / (.753f / 2f);
        rotPos1MR2ML = -rotPos1ML2MR;
        gear1.eulerAngles = rotIdlerFR1.eulerAngles = rotIdlerFL1.eulerAngles = new Vector3(0, 20f * rotPos1, 0);//needs to be scaled up by 20 like whole world is
        gear2.eulerAngles = rotIdlerFR2.eulerAngles = rotIdlerFL2.eulerAngles = new Vector3(0, 20f * rotPos2, 0);
        rotBelt1_Part1.eulerAngles = rotBelt2_Part2.eulerAngles = new Vector3(0, 20f * rotPos1MR2ML, 0);
        rotBelt1_Part4.eulerAngles = rotBelt2_Part3.eulerAngles = new Vector3(0, 20f * rotPos1ML2MR, 0);

        ////////////////////Diags
        float frontLHypot;//used twice
        float backLHypot;//used twice
        beltFrontL.position = new Vector3((beltFrontLPoint2.position.x + beltFrontLPoint.position.x) / 2f, beltFrontL.position.y, beltFrontL.position.z);//center always between both idlers
        beltFrontL.localScale = new Vector3(frontLHypot = Mathf.Sqrt(Mathf.Pow(beltFrontLPoint2.position.x - beltFrontLPoint.position.x, 2) + Mathf.Pow(.21977f, 2)), beltFrontL.localScale.y, beltFrontL.localScale.z); //pythagorean thrm to scale chg
        beltFrontL.rotation = new Quaternion(beltFrontL.rotation.x, -Mathf.Asin(.1f / frontLHypot), beltFrontL.rotation.z, beltFrontL.rotation.w);
        //R is mirror of L belt
        beltFrontR.position = new Vector3((beltFrontLPoint2.position.x + beltFrontLPoint.position.x) / 2f, beltFrontR.position.y, beltFrontR.position.z);//center always between both idlers
        beltFrontR.localScale = new Vector3(frontLHypot, beltFrontR.localScale.y, beltFrontR.localScale.z); //pythagorean thrm to scale chg
        beltFrontR.rotation = new Quaternion(beltFrontR.rotation.x, Mathf.Asin(.1f / frontLHypot), beltFrontR.rotation.z, beltFrontR.rotation.w);
        //backL
        beltBackL.position = new Vector3((beltBackLPoint2.position.x + beltBackLPoint.position.x) / 2f, beltBackL.position.y, beltBackL.position.z);//center always between both idlers
        beltBackL.localScale = new Vector3(backLHypot = Mathf.Sqrt(Mathf.Pow(beltBackLPoint2.position.x - beltBackLPoint.position.x, 2) + Mathf.Pow(.21977f, 2)), beltBackL.localScale.y, beltBackL.localScale.z); //pythagorean thrm to scale chg
        beltBackL.rotation = new Quaternion(beltBackL.rotation.x, Mathf.Asin(.12f / backLHypot), beltBackL.rotation.z, beltBackL.rotation.w);
        //R mirror or back L
        beltBackR.position = new Vector3((beltBackLPoint2.position.x + beltBackLPoint.position.x) / 2f, beltBackR.position.y, beltBackR.position.z);//center always between both idlers
        beltBackR.localScale = new Vector3(backLHypot, beltBackR.localScale.y, beltBackR.localScale.z); //pythagorean thrm to scale chg
        beltBackR.rotation = new Quaternion(beltBackR.rotation.x, -Mathf.Asin(.12f / backLHypot), beltBackR.rotation.z, beltBackR.rotation.w);
    }

    public IEnumerator LowerBed()//lowe bed after done
    {
        fluid.enabled = false;
        //BedPlate.localPosition = new Vector3(BedPlate.localPosition.x, 7.4f, BedPlate.localPosition.z);//even so equal later evaluates
        while (BedPlate.localPosition.y > 2.02f)
        {
            BedPlate.position = new Vector3(BedPlate.position.x, BedPlate.position.y - .01f, BedPlate.position.z);
            layer.transform.parent.transform.position = new Vector3(layer.transform.parent.transform.position.x, layer.transform.parent.transform.position.y - .01f, layer.transform.parent.transform.position.z);
            yield return new WaitForSeconds(.01f);
        }
        if (BedPlate.localPosition.y < 2.02f)
        {
            canPrint = false;
            currentPrintObj.GetComponent<PrintedObjBehavior>().makeInteractable();//after done printing, on this specific instance
        }
    }

    public IEnumerator MoveExtuder(Vector3 destination, float speed)//destination should be global
    {
        float distCovered, Y, X;
        float fractionOfJourneyDirect = 0f;
        Vector3 startingPos = new Vector3(nozzle.position.x, nozzle.position.y, nozzle.position.z);//in terms of actual nozzle location
        float directDist = Vector2.Distance(new Vector2(nozzle.position.z, nozzle.position.x), new Vector2(destination.z, destination.x));// "x" is z and "y" is -x when looking top down motors nearset you
        float startTime = Time.time;
        moveLocked = true;//lock out other print paths

        while (fractionOfJourneyDirect < 1f)
        {
            distCovered = (Time.time - startTime) * speed;
            fractionOfJourneyDirect = distCovered / Mathf.Abs(directDist);

            Y = Mathf.Lerp(startingPos.x, destination.x, fractionOfJourneyDirect);//both same
            X = Mathf.Lerp(startingPos.z, destination.z, fractionOfJourneyDirect);//only e

            //since Nozzle is essentially a wrapper for the origin of childExturder being odd, an offset is needed. Must still alter childExtruder as other things rely on its movement
            childExtruder.position = new Vector3(Y - nozzleStartPos.x, childExtruder.position.y, X - nozzleStartPos.z);// - y because  "x" is z and "y" is -x when looking top down motors nearset you
            AnimatePrinter();
            yield return null;//so this occurs once per frame
        }
        moveLocked = false;
        counter = 0;//reset counter at end of move
    }
}
