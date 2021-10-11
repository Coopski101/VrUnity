using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideAssignBehavior : MonoBehaviourPunCallbacks
{
    private bool buttonLocked;
    Collider theHand;
    public ExitButton exitButton;
    Player thisPlayer;
    Material defaultMat;
    public RPCManager rpcManager;
    GameObject thisUserCam;

    private void Start()//local icon on hand should be handeled on its own by callback
    {
        int i = 0;
        theHand = GameObject.Find("hands_coll:b_r_index3").GetComponent<Collider>();
        while (!PhotonNetwork.PlayerListOthers[i++].NickName.Equals(gameObject.transform.parent.parent.name)) { }
        thisPlayer = PhotonNetwork.PlayerListOthers[--i];
        defaultMat = gameObject.GetComponent<Renderer>().material;//stock grey

    }

    private void OnTriggerEnter(Collider other)
    {
        if (theHand.Equals(other) && !buttonLocked)
        {
            buttonLocked = true;
            StartCoroutine(ButtonPress());
        }
    }

    IEnumerator ButtonPress()
    {
        if (rpcManager.backUpGuide == thisPlayer)
        {
            rpcManager.setBackUpGuideRPC(thisPlayer, true);//null global
            gameObject.GetComponent<Renderer>().material = defaultMat;//reset to non selected state upon another click- continuous toggle
        }
        else
        {
            if (rpcManager.backUpGuide != null)//someone else currently selected
                GameObject.Find(rpcManager.backUpGuide.NickName).transform.GetChild(0).GetChild(6).GetComponent<Renderer>().material = defaultMat;//reset the button on the submenu for the current backup
            rpcManager.setBackUpGuideRPC(thisPlayer, false);//set global
            Material changeableMat = new Material(gameObject.GetComponent<Renderer>().material.shader);
            changeableMat.color = Color.green;//to let user know the back up is selected
            gameObject.GetComponent<Renderer>().material = changeableMat;
        }

        yield return new WaitForSeconds(1.5f);//cant accidentally double click
        buttonLocked = false;
    }

 
}
