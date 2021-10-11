using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RPCManager : MonoBehaviourPunCallbacks //funnels RPC functionality intended for children through this so there is no photonview id issues 
{//children call RPC methods, which call the real PunRPC methods over the network
    GameObject thisUserCam;
    public UserIPadManager userIPadManager;//assigned ineditor
    public UserListBehavior userListBehavior;//assigned ineditor
    public Player backUpGuide;
    public Player tourGuide;
    bool assignedGuideAlready;
    public List<Material> materials = new List<Material>();//set by photosscreenmanager script
    TourGuideManager initialAssignmentScript;
    public List<Player> otherPlayers;//needed to have an dynamic index to use for positions of miniavatars as PlayerListOthers is an array - store here in case good to use for something else later
    GameObject guideLaser;

    private void Start()
    {
        otherPlayers = new List<Player>();
        thisUserCam = GameObject.Find("OVRPlayerController");
        assignedGuideAlready = false;
        initialAssignmentScript = thisUserCam.GetComponent<TourGuideManager>();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)//keep miniavatar list up to date
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if(newPlayer != PhotonNetwork.LocalPlayer)
            otherPlayers.Add(newPlayer);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        foreach (Player player in PhotonNetwork.PlayerListOthers)//add pre exsisting player
        {
            otherPlayers.Add(player);
        }
    }


    #region SETGUIDE/BACKUP
    public void setGuideRPC(Player player)
    {
        photonView.RPC("setGuide", RpcTarget.AllBuffered, player);
    }

    [PunRPC]
    void setGuide(Player player)
    {
        tourGuide = player;
        initialAssignmentScript.tourGuideAssigned = true;//set all to true - condition will only be true for very first person joined.
                                                         //Since all further reassigning is taken care of here, buffer a call to disable that behavior. (when all exit it kept reassigning the guide)
        StartLaserRPC();//must happen for every guide
    }

    public void setBackUpGuideRPC(Player player, bool nullify)
    {
        photonView.RPC("setBackUpGuide", RpcTarget.AllBuffered, player, nullify);
    }

    [PunRPC]
    void setBackUpGuide(Player player, bool nullify)
    {
        if (nullify)
            backUpGuide = null;//since you cant pass null as the argument for player  due to RPC, this is how i indicate/detect the case where no backup is yet selected upon switch
        else
            backUpGuide = player;
    }
    #endregion

    public override void OnPlayerLeftRoom(Player otherPlayer)//decide who backup guide will be
    {
        Player toRemove = otherPlayers.Find(x => x.NickName == otherPlayer.NickName);
        otherPlayers.Remove(toRemove);

        print("left" + otherPlayer.NickName);
        if(tourGuide == otherPlayer && backUpGuide == null && !thisUserCam.CompareTag("TourGuide"))//no back up and  guide left - make sure not double calling hence last condition
        {
            print("none" + otherPlayer.NickName);
            
            PhotonNetwork.LeaveRoom(false); // load lobby scene, returns to master server
            SceneManager.LoadSceneAsync(0);//back to main menu
        }
        else if(tourGuide == otherPlayer && backUpGuide != null)
        {
            print("reassign" + otherPlayer.NickName + backUpGuide.NickName);
            photonView.RPC("ReassignGuide", backUpGuide);//many will call only one will execute in method
        }
        else if (thisUserCam.CompareTag("TourGuide") && otherPlayer == backUpGuide)//if backup guide leaves - only call once from tourguide instance
        {
            print("backup left" + otherPlayer.NickName);
            setBackUpGuideRPC(PhotonNetwork.LocalPlayer, true);//resets globally to null
        }

        base.OnPlayerLeftRoom(otherPlayer);
    }


    #region BAN/KICKPLAYER
     public void AddToGlobalBanListRPC(string user)//cant pass in rpc as whole list so just keep everyones local copies up to date
    {
        photonView.RPC("AddToGlobalBanList", RpcTarget.AllBuffered, user);
    }

    [PunRPC]
    void AddToGlobalBanList(string name)
    {
        userIPadManager.banList.Add(name);
    }    
    
    public void BanKickPlayerRPC(Player thePlayer)
    {
        photonView.RPC("BanKickPlayer", thePlayer);//only called on the person to be kicked
    }

    [PunRPC]
     void BanKickPlayer()
    {
        PhotonNetwork.LeaveRoom(false); // load lobby scene, returns to master server
        SceneManager.LoadSceneAsync(0);//back to main menu
    }

    public void KickPlayerRPC(Player thePlayer)
    {
        photonView.RPC("KickPlayer", thePlayer);//only called on the person to be kicked
    }

    [PunRPC]
     void KickPlayer()
    {
        PhotonNetwork.LeaveRoom(false); // load lobby scene, returns to master server
        SceneManager.LoadSceneAsync(0);//back to main menu
    }
    #endregion 

    #region GLOBALMUTE
    public void GlobalMuteRPC(string nameToMute, bool guidesMuteSetting)
    {
        photonView.RPC("GlobalMute", RpcTarget.AllBuffered, nameToMute, guidesMuteSetting);
    }

    [PunRPC]
     void GlobalMute(string nameToMute, bool guidesMuteSetting)
    {
        StartCoroutine(DelayedButtonPress(nameToMute, guidesMuteSetting));
    }

    IEnumerator DelayedButtonPress(string nameToMute, bool guidesMuteSetting)//since buffered, need to provide a second for "waitforplayerloaded" to execute before this is called
    {
        yield return new WaitForSeconds(1.1f);
        MuteButtonOthers localCopy = transform.GetChild(0).GetChild(1).Find(nameToMute).GetChild(0).GetChild(3).gameObject.GetComponent<MuteButtonOthers>();
        localCopy.isMuted = guidesMuteSetting;// set to tourguide's setting so that can be toggled to true/false for everyone at once
        StartCoroutine(localCopy.ButtonPress());//use build functionality to keep local state consistent
    }
    #endregion

    #region REASSIGNGUIDE
    public void ReassignGuideRPC(Player thePlayer)
    {
        photonView.RPC("ReassignGuide", thePlayer);
    }

    [PunRPC]
    void ReassignGuide()
    {
        if (!assignedGuideAlready)//so despite many calls, only one execution on recieving end
        {
            assignedGuideAlready = true;
            thisUserCam.tag = "TourGuide";
            photonView.RPC("setGuide", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer);//reset global guide as self
            photonView.RPC("setBackUpGuide", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer, true);//reset backup guide
                                                                          //update so that states pertinent to tourguide are activated
            gameObject.transform.GetChild(0).GetChild(0).GetChild(4).gameObject.SetActive(true);//enable picture select for tourguide
            StartCoroutine(gameObject.transform.GetChild(0).GetChild(3).gameObject.GetComponent<PhotosScreenManager>().WaitForLoad());//initialize picture list for new guide

            GameObject defaultNameObject = transform.GetChild(0).GetChild(1).GetChild(0).gameObject;
            defaultNameObject.transform.GetChild(0).GetChild(4).GetChild(0).gameObject.SetActive(true);//pic - these need to be set active
            defaultNameObject.transform.GetChild(0).GetChild(5).GetChild(0).gameObject.SetActive(true);//pic 
            defaultNameObject.transform.GetChild(0).GetChild(6).GetChild(0).gameObject.SetActive(true);//pic
            for (int i = 4; i <= defaultNameObject.transform.parent.childCount - 1; i++)//all exsisting users on guide transfer
            {
                defaultNameObject.transform.parent.GetChild(i).GetChild(0).GetChild(4).GetChild(0).gameObject.SetActive(true);//pic 
                defaultNameObject.transform.parent.GetChild(i).GetChild(0).GetChild(5).GetChild(0).gameObject.SetActive(true);//pic 
                defaultNameObject.transform.parent.GetChild(i).GetChild(0).GetChild(6).GetChild(0).gameObject.SetActive(true);//pic 
            }

            userIPadManager.MainTrigger();//for state correction
        }
    }
    #endregion

    #region LASER
    public void StartLaserRPC()
    {
        photonView.RPC("StartLaser", RpcTarget.OthersBuffered);
        GameObject[] possibleObjs = GameObject.FindGameObjectsWithTag("OtherPlayerParent");
        foreach (GameObject possibility in possibleObjs)//find guides OWN avatar and enable its laser to see/control
        {
            if (possibility.TryGetComponent<PhotonView>(out PhotonView theView))//not all tagged objects have one
            {
                print(possibility + "    "+ theView + "     "+ theView.IsMine);

                if (theView.IsMine)
                    possibility.GetComponent<PlayerAvatarObjectManager>().laser.SetActive(true);
            }
        }
    }

    [PunRPC]
    void StartLaser()
    {
        GameObject[] possibleObjs = GameObject.FindGameObjectsWithTag("OtherPlayerParent");
        foreach (GameObject possibility in possibleObjs)//find guides avatar and enable its laser to see
        {
           if(possibility.TryGetComponent<PhotonView>(out PhotonView theView))//not all tagged objects have one
           {
                if (theView.Owner.Equals(tourGuide))
                {
                    guideLaser = possibility.GetComponent<PlayerAvatarObjectManager>().laser;
                    guideLaser.SetActive(true);//only one pointer per game instance, that of the guide avatar
                }
           }  
        }
    }

    //public void UpdateLaserRPC(Vector3 endPos, bool inputB)//back up for rpc already inside physicspointer
    //{
    //    photonView.RPC("UpdateLaser", RpcTarget.AllBuffered, endPos, inputB);
    //}

    //[PunRPC]
    //void UpdateLaser(Vector3 endPos, bool inputB)
    //{
    //    LineRenderer theRenderer = guideLaser.GetComponent<LineRenderer>();
    //    theRenderer.SetPosition(0, guideLaser.transform.position);//local posistion of guide avatar set
    //    theRenderer.SetPosition(1, endPos);//guides instance end position
    //    theRenderer.enabled = inputB;
    //}
    #endregion

    #region SKYBOX
    public void ChangeSkyboxRPC(int nextMatIndex) //not matching state  for users that enter afterwards - fix
    {
        photonView.RPC("ChangeSkybox", RpcTarget.AllBuffered, nextMatIndex);
    }

    [PunRPC]
    void ChangeSkybox(int nextMatIndex)//dont want to send whole material over network
    {
        RenderSettings.skybox = materials[nextMatIndex];
    }
    #endregion

    #region SENDID
    public void SendIdNetwork()//send unique android id to be used in banning
    {
        photonView.RPC("RecieveIdNetwork", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer, SystemInfo.deviceUniqueIdentifier);
    }

    [PunRPC]
    void RecieveIdNetwork(Player otherNetworkedPlayer, string uniqueId)//apply android id to local copy of player
    {
        ((GameObject)otherNetworkedPlayer.TagObject).GetComponent<PlayerAvatarObjectManager>().uniqueId = uniqueId;
    }
    #endregion



}
