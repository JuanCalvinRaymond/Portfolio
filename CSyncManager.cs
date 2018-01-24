using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Linq;
using System.Reflection;

public class CSyncManager : Photon.MonoBehaviour
{
    //List of Gameobject to keep track of activeself
    public List<GameObject> m_listOfGameObjectToWatch;
    
    /*
    Description : When player is instantiated find all other syncmanager on the scene and add it to the list
    */
    private void Start()
    {

        //Instantiating list
        m_listOfGameObjectToWatch = m_listOfGameObjectToWatch ?? new List<GameObject>();

    }
    

    public void Sync()
    {
        //Get selected player and sync the gameobject
        if (CPartyManagement.PInstance != null)
        {
            List<Transform> syncedPlayers = CPartyManagement.PInstance.GetSelectedPlayersTransform();
            foreach (Transform player in syncedPlayers)
            {
                CSyncManager syncManager = player.gameObject.GetComponent<CSyncManager>();
                if(syncManager != null)
                {
                    //Tell other sync manager to listen this broadcast
                    syncManager.Listen(m_listOfGameObjectToWatch);
                }
            }
        }

        //Sync all syncable component
        photonView.RPC("SyncComponent", PhotonTargets.All);
    }

    /*
    Description : Sync gameobject based on sync manager that call this function
    */
    public void Listen(List<GameObject> aListOfGameObject)
    {
        
        for (int i = 0; i < aListOfGameObject.Count; i++)
        {
            if (aListOfGameObject != null)
            {
                photonView.RPC("SyncGameObject", PhotonTargets.All, i, aListOfGameObject[i].activeSelf);

            }
        }
    }

    [PunRPC]
    private void SyncGameObject(int aIndex, bool aIsEnabled)
    {
        m_listOfGameObjectToWatch[aIndex].SetActive(aIsEnabled);
    }

    [PunRPC]
    private void SyncComponent()
    {
        if (CPartyManagement.PInstance != null)
        {
            //Get all ISync in the children gameobject and call SyncComponent.
            ISync[] listOfSync = GetComponentsInChildren<ISync>();
            foreach (ISync sync in listOfSync)
            {
                sync.SyncComponent(CPartyManagement.PInstance);
            }
        }
        else
        {
            Debug.Log("The party management in the sync manager of " + gameObject.name + " " + photonView?.owner?.NickName + " is null");
        }
    }
}
