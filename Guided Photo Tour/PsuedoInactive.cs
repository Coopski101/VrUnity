using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PsuedoInactive : MonoBehaviour //since parent deactive deactivates all children and puts them out of bounds, this is my workaround to keep neat parent child organization (and not redo too much). 
{//they are always active to communicate with their scripts but are not always able to be touched and seen. recursivly searches children for their specific targeted components and activates/ deactivates them


    public bool active = false;//default is "inactive" - in editor can decide if it should start "active" or not
    GameObject thisUserCam;

    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(.5f);
        thisUserCam = GameObject.Find("OVRPlayerController");
        PsuedoToggler(active);
    }

    public void PsuedoToggler(bool changeTo)//if contained enable what makes an object "active"
    {
        active = changeTo;
        thisUserCam = GameObject.Find("OVRPlayerController");//needs to be refound on player amount change -not sure why but it fixes that it randomly nulls itself. easy to find so nbd
        List<GameObject> allDescendants = new List<GameObject>();
        CreateDescendantList(gameObject.transform, allDescendants);
        allDescendants.Add(gameObject);//must add self too for the activeness functionality
        foreach (GameObject child in allDescendants)//control visibility and interactibility
        {
         
            if (child.TryGetComponent(out Collider toEnableC))
                toEnableC.enabled = active;
            if (child.TryGetComponent(out Renderer toEnableR))
                toEnableR.enabled = active;
            if (child.TryGetComponent(out Image toEnableI))
                toEnableI.enabled = active;
            if (child.TryGetComponent(out TextMeshPro toEnableT))
                toEnableT.enabled = active;
        }

    }

    private void CreateDescendantList(Transform parent, List<GameObject> descendants)//cheaper to call every time than EVERYTHING store a list
    {//recursivly adds ALLLLLL children
        foreach (Transform child in parent)
        {
            if (child.childCount > 0)//recursive call
                CreateDescendantList(child, descendants);

            //quick exit if guide, evaluate LtoR
            if(thisUserCam.CompareTag("TourGuide") || ( !child.name.Equals("Kick") && !child.name.Equals("Ban") && !child.name.Equals("kickPic") && !child.name.Equals("GuideAssign") && !child.name.Equals("GuidePic")) )//if tour guide, ok to add ban button, kick - other wise stays "inactive" forever
                descendants.Add(child.gameObject);


        }
    }
   
}
