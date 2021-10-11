using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Voice;
using Photon.Voice.PUN;
using OVR;
using Photon.Realtime;

public class MuteButtonOthers : MonoBehaviour //similar to mute button. later implementation will have functionality of muting a specific player
{
    public GameObject mutedPic, unmutedPic;
    public bool isMuted;//needs access in rpc manager
    private bool buttonLocked;
    Collider theHand;
    private AudioSource audioSource;
    private PhotonVoiceView voiceView;
    private Player thisPlayer;
    private GameObject thisUserCam;
    public RPCManager rpcManager;

    private void Start()
    {
        theHand = GameObject.Find("hands_coll:b_r_index3").GetComponent<Collider>();
        mutedPic.SetActive(isMuted = false);//start unmuted
        unmutedPic.SetActive(true);
        buttonLocked = false;
        int i = 0;
        thisUserCam = GameObject.Find("OVRPlayerController");
        while (!PhotonNetwork.PlayerListOthers[i++].NickName.Equals(gameObject.transform.parent.parent.name)) { }
        thisPlayer = PhotonNetwork.PlayerListOthers[--i];

        StartCoroutine(WaitForPlayerLoaded());
    }

    IEnumerator WaitForPlayerLoaded()
    {//no good way to wait for linking of the manager script and the head so just wait a sec
        while ((GameObject)thisPlayer.TagObject == null)
            yield return null;
        while (((GameObject)thisPlayer.TagObject).GetComponent<PlayerAvatarObjectManager>().uniqueId == null)//wait for rpc to set the id
            yield return null;

        if (((GameObject)thisPlayer.TagObject).TryGetComponent<PlayerAvatarObjectManager>(out PlayerAvatarObjectManager playerAvatarObjectManager))//null check
        {
            audioSource = playerAvatarObjectManager.playerHead.GetComponent<AudioSource>();
            voiceView = playerAvatarObjectManager.playerHead.GetComponent<PhotonVoiceView>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!buttonLocked && other.Equals(theHand))//prevent double click
        {
            buttonLocked = true;

            if (thisUserCam.CompareTag("TourGuide"))//tourguide muting a player will mute for everyone
                rpcManager.GlobalMuteRPC(transform.parent.parent.name, isMuted);
            else
                StartCoroutine(ButtonPress());//mute in local instance only
        }
    }

    public IEnumerator ButtonPress()
    {
        isMuted = !isMuted;//toggle state and images
        mutedPic.SetActive(isMuted);
        unmutedPic.SetActive(!isMuted);
        voiceView.RecorderInUse.TransmitEnabled = !(isMuted);//get muted son - this is still needed to keep states correct
        audioSource.enabled = !isMuted;//actually fixes
        Debug.Log("muting    " + thisPlayer.NickName + "   "+ voiceView.RecorderInUse.TransmitEnabled);
        yield return new WaitForSeconds(1f);//cant keep pressing
        buttonLocked = false;//reset
    }

   
}
