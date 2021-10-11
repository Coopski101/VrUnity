using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using UnityEditor;

public class PrintedObjBehavior : MonoBehaviour
{
    Vector3 ogSpot;
    GameObject theScriptObject;
    bool doneAlready;
    List<GeometryScript> allScripts;
    private void Start()//gameobject attached to must match name of the container that has the printer behavior attached
    {
        allScripts = new List<GeometryScript>();
        int i = 0;
        ogSpot = gameObject.transform.position;
        theScriptObject = GameObject.Find("Obi Solver");//all different paths inherit Geometryscript
        foreach (Transform child in theScriptObject.transform)
        {
            allScripts.Add(child.gameObject.GetComponent<GeometryScript>());
            child.GetComponent<GeometryScript>().canPrint = false;//cant switch geometry to ruin state
        }
        doneAlready = false;
    }
    public void makeInteractable()
    {
        gameObject.AddComponent<Throwable>();//should add interactable and rigid
        gameObject.GetComponent<Interactable>().hideHandOnAttach = false;
    }

    private void Update()
    {
        if (!doneAlready && ((gameObject.transform.position.x > ogSpot.x + .5f) || (gameObject.transform.position.x < ogSpot.x - .5f)))//if moved off bed
        {//range needed as rigidbody turn on shifts position
            doneAlready = true;//only called once
            foreach (GeometryScript child in allScripts)//so that switching shape wont ruin state
            {
                child.GetComponent<GeometryScript>().canPrint = true;
                child.GetComponent<GeometryScript>().layersDone = 0;
                child.GetComponent<GeometryScript>().enabled = false;
            }
        }
    }
}