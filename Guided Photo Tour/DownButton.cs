using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class DownButton : MonoBehaviourPunCallbacks //opposite of upbutton
{
    List<GameObject[]> organizedList;
    Collider theHand;
    private bool buttonLocked;
    int activePage;

    private void Start()
    {
        theHand = GameObject.Find("hands_coll:b_r_index3").GetComponent<Collider>();
        organizedList = gameObject.transform.parent.parent.parent.GetComponent<UserListBehavior>().pageList;
        buttonLocked = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!buttonLocked && other.Equals(theHand))//prevent double click
        {
            buttonLocked = true;
            StartCoroutine(ButtonPress());
        }

    }
    IEnumerator ButtonPress()
    {        
        if (organizedList.Count > 1)
        {
            //find which page is active, deactivate, go to next, activate
            foreach (var page in organizedList)//can always change
            {
                if (page[0] != null)
                {
                    if (page[0].GetComponent<PsuedoInactive>().active)//current page?
                        activePage = organizedList.IndexOf(page);
                }
            }
            foreach (var item in organizedList[activePage])
            {//deactivate current
                if (item != null)
                {
                    item.GetComponent<PsuedoInactive>().PsuedoToggler(false);
                    item.transform.GetChild(0).GetComponent<PsuedoInactive>().PsuedoToggler(false);//to keep state correct for line 48
                }
            }
            --activePage;
            if (activePage < 0)
                activePage = organizedList.Count -1;//end of list
            else
                activePage %= organizedList.Count;//will loop back around
            foreach (var item in organizedList[activePage])
            {//nextpage
                if (item != null)
                {
                    item.GetComponent<PsuedoInactive>().PsuedoToggler(true);
                    item.transform.GetChild(0).GetComponent<PsuedoInactive>().PsuedoToggler(false);
                }
            }
        }

        yield return new WaitForSeconds(1.5f);//cant keep pressing
        buttonLocked = false;//reset
    }
}
