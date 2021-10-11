using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class UserListBehavior : MonoBehaviourPunCallbacks
{
    public GameObject defaultNameObject;//will be instatiated multiple times then made active
    List<GameObject> nameList = new List<GameObject>();//all names separate of their page
    public List<GameObject[]> pageList = new List<GameObject[]>();//names assigned to pages
    int curPageNum, perPage;
    Vector3 defaultNamePos, defaultPagePos;//save defaults as posistion will change as new people join
    GameObject thisUserCam;//local instance


    void Start()
    {
        perPage = 6;
        curPageNum = 0;
        defaultNamePos = defaultNameObject.GetComponent<RectTransform>().anchoredPosition3D;
        defaultPagePos = defaultNameObject.transform.GetChild(0).localPosition;
        thisUserCam = GameObject.Find("OVRPlayerController");
        StartCoroutine(WaitAndGetTag());
    }
    public IEnumerator WaitAndGetTag()//contains all delayed loading items
    {
        yield return new WaitForSeconds(.6f);
        if (thisUserCam.CompareTag("TourGuide"))//enable tour guide specifics
        {//enable renderers so only guide can see (and also use) but all have RPC client access
            defaultNameObject.transform.GetChild(0).GetChild(4).GetChild(0).gameObject.SetActive(true);//pic - these need to be set active
            defaultNameObject.transform.GetChild(0).GetChild(5).GetChild(0).gameObject.SetActive(true);//pic 
            defaultNameObject.transform.GetChild(0).GetChild(6).GetChild(0).gameObject.SetActive(true);//pic
            for(int i = 4; i <= defaultNameObject.transform.parent.childCount -1; i++)//all exsisting users on guide transfer
            {
                defaultNameObject.transform.parent.GetChild(i).GetChild(0).GetChild(4).GetChild(0).gameObject.SetActive(true);//pic 
                defaultNameObject.transform.parent.GetChild(i).GetChild(0).GetChild(5).GetChild(0).gameObject.SetActive(true);//pic 
                defaultNameObject.transform.parent.GetChild(i).GetChild(0).GetChild(6).GetChild(0).gameObject.SetActive(true);//pic 
            }
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        foreach (Player playerExsisting in PhotonNetwork.PlayerListOthers)//add others to your local list that were there previously
        {
            GameObject newGuy;
            nameList.Add(newGuy = GameObject.Instantiate(defaultNameObject, defaultNameObject.transform.position, defaultNameObject.transform.rotation, defaultNameObject.transform.parent));//new item to the list
            newGuy.SetActive(true);//as defult it inactive
            newGuy.transform.parent = defaultNameObject.transform.parent.transform;//child to correct item
            newGuy.name = playerExsisting.NickName;//change text that user sees to match nickname
            newGuy.GetComponent<TextMeshPro>().text = playerExsisting.NickName;
            newGuy.transform.GetChild(0).GetComponent<TextMeshPro>().text = playerExsisting.NickName;//cant be get in children bc there's more than one now
            newGuy.GetComponent<PsuedoInactive>().PsuedoToggler(false);//shut off initially - must only be loaded by a click
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        GameObject newGuy;
        nameList.Add( newGuy = GameObject.Instantiate(defaultNameObject, defaultNameObject.transform.position, defaultNameObject.transform.rotation, defaultNameObject.transform.parent));//new item to the list
        newGuy.SetActive(true);//as default is inactive
        newGuy.transform.parent = defaultNameObject.transform.parent.transform;//sibling
        newGuy.name = newPlayer.NickName;//set visible text
        newGuy.GetComponent<TextMeshPro>().text = newPlayer.NickName;
        newGuy.transform.GetChild(0).GetComponent<TextMeshPro>().text = newPlayer.NickName;

        if (newGuy.transform.parent.gameObject.GetComponent<PsuedoInactive>().active)//must be looking at user section for page update to occur
            PageUpdate();
        else
            newGuy.GetComponent<PsuedoInactive>().PsuedoToggler(false);//shut off if not looking at user screen
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        GameObject toRemove = nameList.Find(x => x.name == otherPlayer.NickName);
        nameList.Remove(toRemove);
        Destroy(toRemove);
        if (defaultNameObject.transform.parent.gameObject.GetComponent<PsuedoInactive>().active)//must be looking at user section for page update to occur
            PageUpdate();
    }

    public void PageUpdate()//called on exit or enter to alter the number of pages ect -should always take user back to first page of list  even if in a submenu
    {
        foreach (var page in pageList)//reset list of organized pages as names may be diffrent than before
        {
            for(int i = 0; i < page.Length; i++)
            {
                page[i] = null;
            }
        }

        int newPageNum = ((PhotonNetwork.CurrentRoom.PlayerCount - 2 )/ perPage) + 1;//how many pages should there be?
        if (newPageNum > curPageNum)//add a new page if needed
        {
            pageList.Add(new GameObject[perPage]);
            curPageNum++;
        }
        else if (newPageNum < curPageNum)//remove a page it needed
        {
            curPageNum--;
            GameObject[] toRemove = pageList[pageList.Count - 1];//last page
            pageList.Remove(toRemove);
        }

        int currentPageSpot = 0;//b/c index starts at zero, first time thru will add first (0th ) page
        foreach (var item in nameList)//reorganize name objects
        {
            int i;//for scope
            RectTransform theTransfrom = item.GetComponent<RectTransform>();
            theTransfrom.anchoredPosition3D = defaultNamePos;//reset before reshift
            item.transform.GetChild(0).localPosition = defaultPagePos;//reset 

            for ( i = 0  ; i < (nameList.IndexOf(item)  % perPage); i++)//set position of name on ipad screen
            {
                theTransfrom.anchoredPosition3D = new Vector3(theTransfrom.anchoredPosition3D.x, theTransfrom.anchoredPosition3D.y, theTransfrom.anchoredPosition3D.z - .03f);
                item.transform.GetChild(0).localPosition = new Vector3(0f, item.transform.GetChild(0).localPosition.y + (.03f * 20f), 0f);//correct page to not follow parent name
            }

            if (nameList.IndexOf(item) % perPage == 0 && nameList.IndexOf(item) != 0)//increment the current page if amount is exceeding capacity
            {
                currentPageSpot++;
            }
            pageList[currentPageSpot][i] = item;//parent (in spirit) to correct page so that pages can be deactivated via scroll functionality
            item.GetComponent<NameClickManager>().parentPage = currentPageSpot;
        }

        //also needed so that first time, two pages are displayed seperately after assignment
        if (nameList.Count != 0 )//if user is looking at user screen (or not) go back to first page on updated member list if not not only person
        {
            foreach (var page in pageList)//all inactive
            {
                if (page != null)//with loading order and edge cases, best to constantly check for null
                {
                    for (int i = 0; i < page.Length; i++)
                    {
                        if (page[i] != null)
                        {
                            if (page[i].transform.GetChild(0).gameObject.GetComponent<PsuedoInactive>().active)//if activly looking at a users menu screen
                            {
                                page[i].transform.GetChild(0).gameObject.GetComponent<PsuedoInactive>().PsuedoToggler(false);//incase volume menu is up while reloading
                                page[i].transform.parent.parent.GetChild(1).gameObject.GetComponent<PsuedoInactive>().PsuedoToggler(true);//turn list scroll buttons on
                                page[i].transform.parent.parent.GetChild(2).gameObject.GetComponent<PsuedoInactive>().PsuedoToggler(true);//turn list scroll buttons on
                                page[i].transform.parent.parent.GetChild(3).gameObject.GetComponent<PsuedoInactive>().PsuedoToggler(true);//turn list scroll buttons on
                            }
                            page[i].GetComponent<PsuedoInactive>().PsuedoToggler(false);//shut it off since a update will take the user back to looking at somewthing else
                        }
                    }
                }
            }
            for (int i = 0; i < pageList[0].Length; i++)//make first page active so user sees this on a reload
            {
                if (pageList[0][i] != null && pageList[0][i].transform.parent.gameObject.GetComponent<PsuedoInactive>().active)//user section must be active in order to reactivate anything
                {
                    pageList[0][i].GetComponent<PsuedoInactive>().PsuedoToggler(true);//name on 
                    pageList[0][i].transform.GetChild(0).GetComponent<PsuedoInactive>().PsuedoToggler(false);//name  menu off
                   
                }
            }
        }

    }

}
