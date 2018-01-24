using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class CWritingAndErasing : Photon.PunBehaviour, ISync
{
    [Tooltip("Raycast distance so it's not infinity")]
    private const float M_RAYCAST_DISTANCE = 10.0f;
    private const float M_RAYCAST_OFFSET_DISTANCE = 1.0f;
    private bool m_isColliding;

    private Vector3 m_hitPoint;
    private Vector3 m_previousPosition;
    private Vector3 m_positionOffset;
    private Vector3 m_rotationOffset;

    private Vector3 m_wallNormal;

    private CViveController m_controller;
    private GameObject m_rightController;

    [Tooltip("Distance between marker and the wall to snap marker back to controller")]
    public float m_distanceToRelease;

    [Tooltip("Gameobject to activate when writing is enabled. Ex : Marker Model")]
    public GameObject[] m_gameObjectToShow;
    [Tooltip("Gameobject to deactivate when writing is enabled. Ex : Controller model")]
    public GameObject[] m_gameObjectToHide;

    [Tooltip("Layer mask for detecting writable surface")]
    public LayerMask m_layerMask;

    [Space]
    [Header("Brush")]
    private PaintReceiver m_paintReceiver;
    private Stamp stamp;
    private Vector2? lastDrawPosition = null;
    private Vector2 m_texCoord;
    private float currentAngle = 0f;
    private float lastAngle = 0f;

    [SerializeField]
    [Tooltip("Stamp for marker")]
    public Texture2D m_brushAlpha;

    [Tooltip("Enum between paint or erase")]
    public PaintMode m_paintMode;

    [SerializeField]
    [Tooltip("Color for marker to paint over the surface")]
    public Color m_color;

    [SerializeField]
    private float spacing = 1f;

    [Tooltip("Script to change marker's color")]
    public CColorPicker m_colorPicker;

    [Tooltip("Mesh that will change color as the marker's color change. Ex : Marker's tip mesh")]
    public MeshRenderer m_markerColoredRenderer;

    private void Awake()
    {
        stamp = new Stamp(m_brushAlpha);
        stamp.mode = m_paintMode;
        m_controller = GetComponentInParent<CViveController>();
        m_rightController = GetComponentInParent<SteamVR_ControllerManager>().right;

        m_positionOffset = transform.localPosition;
        m_rotationOffset = transform.localRotation.eulerAngles;
    }

    private void Start()
    {
        //Get player's color and set it to be the marker's starting marker
        if (photonView != null)
        {
            if (photonView.owner != null)
            {
                Color colorRead = new Color();

                if (CPhotonPlayerCustomProperties.GetPlayerColor(photonView.owner.CustomProperties, ref colorRead))
                {
                    m_color = colorRead;
                }
            }
            else
            {
                m_color = CLocalPlayerData.PInstance.PPlayerColor;
            }
        }

        if (m_markerColoredRenderer != null)
        {
            m_markerColoredRenderer.material.color = m_color;

        }

        if (m_controller != null)
        {
            m_controller.OnInteractButtonUp += BroadcastEndLine;
        }
        if (m_colorPicker != null)
        {
            m_colorPicker.PCurrentColor = m_color;

            m_colorPicker.OnColorModified += BroadcastChangeColor;
        }
    }

    /*
    Description : Whenever the script is enabled change the model.
    */
    private void OnEnable()
    {
        if (PhotonNetwork.room != null)
        {
            photonView.RPC("ChangeModel", PhotonTargets.All, true);
        }
        else
        {
            ChangeModel(true);
        }
    }

    /*
    Description : Whenever the script is Disabled change the model.
    */
    private void OnDisable()
    {
        if (PhotonNetwork.room != null)
        {
            photonView.RPC("ChangeModel", PhotonTargets.All, false);
        }
        else
        {
            ChangeModel(false);
        }
    }

    private void OnDestroy()
    {
        if (m_controller != null)
        {
            m_controller.OnInteractButtonUp -= BroadcastEndLine;
        }

        if (m_colorPicker != null)
        {
            m_colorPicker.OnColorModified -= BroadcastChangeColor;
        }
    }
    
    /*
    Description : Raycast forward when not colliding to check if the marker hit any writable surface. If it's colliding
    check for writing input to calculate changes in pixel color to start writing/erasing and check the distance from
    the surface to snap back to controller.
    */
    private void Update()
    {
        if (!photonView.isMine)
        {
            return;
        }


        if (!m_isColliding)
        {
            //Raycast every frame to detect any writable surface
            CalculateCollision();
        }
        else
        {
            //Move and rotate marker and RPC it through all the scene 
            CalculatePencilPositionAndRotation();

            //Check if marker far enough from the wall to snap back to controller 
            CalculateDistance();

            currentAngle = -transform.rotation.eulerAngles.z;

            //If user press interact and marker is still on the surface, start doing writing calculation.
            if (m_controller.PIsInteractButtonPress && m_isColliding)
            {
                if (lastDrawPosition.HasValue && lastDrawPosition.Value != m_texCoord)
                {
                    if (PhotonNetwork.room != null)
                    {
                        photonView.RPC("DrawLine", PhotonTargets.All, lastDrawPosition.Value, m_texCoord, lastAngle, currentAngle, m_color.r, m_color.g, m_color.b, m_color.a, spacing);
                    }
                    else
                    {
                        DrawLine(lastDrawPosition.Value, m_texCoord, lastAngle, currentAngle, m_color.r, m_color.g, m_color.b, m_color.a, spacing);
                    }
                }
                lastDrawPosition = m_texCoord;
                lastAngle = currentAngle;
            }
        }

        m_previousPosition = transform.position;
    }


    /*
    Description : Raycast forward to detect any paint receiver, when hit store the surface's normal and texcoord.
    */
    private void CalculateCollision()
    {
        Vector3 direction = transform.position - m_previousPosition;
        float distance = direction.magnitude;

        if (distance == 0.0f)
        {
            return;
        }

        direction /= distance;

        Ray ray = new Ray(m_previousPosition, direction);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distance, m_layerMask, QueryTriggerInteraction.Ignore))
        {
            m_paintReceiver = hit.collider.gameObject.GetComponent<PaintReceiver>();
            if (m_paintReceiver != null)
            {
                m_texCoord = hit.textureCoord;
                m_wallNormal = hit.normal;

                if (PhotonNetwork.room != null)
                {
                    photonView.RPC("BroadcastIsColliding", PhotonTargets.All, hit.point);
                }
                else
                {
                    BroadcastIsColliding(hit.point);
                }
            }
        }
    }

    /*
    Description : Calculate the distance from the surface if the distance is higher than the threshold snap it back
    to the controller.
    */
    private void CalculateDistance()
    {
        Vector3 offset = m_rightController.transform.position - m_hitPoint;

        float dotProduct = Vector3.Dot(offset, m_wallNormal);

        if (dotProduct > 0)
        {
            float handDistanceFromWall = Vector3.Project(m_hitPoint - m_rightController.transform.position, m_wallNormal).magnitude;

            if (handDistanceFromWall > m_distanceToRelease * m_distanceToRelease)
            {
                if (PhotonNetwork.room != null)
                {
                    photonView.RPC("HoldToHand", PhotonTargets.All);
                }
                else
                {
                    HoldToHand();
                }

                if (PhotonNetwork.room != null)
                {
                    photonView.RPC("EndLine", PhotonTargets.All);
                }
                else
                {
                    EndLine();
                }
            }
        }
    }


    /*
    Description : Calculating where the marker should move and rotate, after that raycast to get a new texcoord or if
    marker is hitting new writable surface.
    Note : Change this function to get different movement behaviour.
    */
    private void CalculatePencilPositionAndRotation()
    {
        //Marker will snap to the surface and move around, and always stay straight up from the surface.
        //Change this block of code for different movement behaviour.
        Vector3 offset = m_rightController.transform.position - m_hitPoint;
        Vector3 vectorOnSurface = Vector3.ProjectOnPlane(offset, m_wallNormal);
        Vector3 targetPosition = m_hitPoint + vectorOnSurface;
        Quaternion targetRotation = Quaternion.LookRotation(-m_wallNormal);


        if (PhotonNetwork.room != null)
        {
            photonView.RPC("BroadcastPencilPositionAndRotation", PhotonTargets.All, targetPosition, targetRotation);
        }
        else
        {
            BroadcastPencilPositionAndRotation(targetPosition, targetRotation);
        }
        
        Ray ray = new Ray(transform.position - (transform.forward * M_RAYCAST_OFFSET_DISTANCE), transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, M_RAYCAST_DISTANCE, m_layerMask, QueryTriggerInteraction.Ignore))
        {
            PaintReceiver paintReceiver = hit.collider.gameObject.GetComponent<PaintReceiver>();
            if (m_paintReceiver != null)
            {
                if (m_paintReceiver != paintReceiver)
                {
                    if (PhotonNetwork.room != null)
                    {
                        photonView.RPC("ChangePaintReceiver", PhotonTargets.All);
                    }
                    else
                    {
                        ChangePaintReceiver();
                    }
                    lastDrawPosition = null;
                }
                m_texCoord = hit.textureCoord;
            }

            return;
        }

        if (PhotonNetwork.room != null)
        {
            photonView.RPC("HoldToHand", PhotonTargets.All);
        }
        else
        {
            HoldToHand();
        }

        BroadcastEndLine();
    }


    #region Broadcasting
    private void BroadcastEndLine()
    {
        if (PhotonNetwork.room != null)
        {
            photonView.RPC("EndLine", PhotonTargets.All);
        }
        else
        {
            EndLine();
        }
    }

    private void BroadcastChangeColor(Color aNewColor)
    {
        if (PhotonNetwork.room != null)
        {
            photonView.RPC("ChangeBrushColor", PhotonTargets.All, aNewColor.r, aNewColor.g, aNewColor.b, aNewColor.a);
        }
        else
        {
            ChangeBrushColor(aNewColor.r, aNewColor.g, aNewColor.b, aNewColor.a);
        }
    }

    #endregion

    #region RPCs
    [PunRPC]
    private void DrawLine()
    {
        m_paintReceiver.DrawLine(stamp, lastDrawPosition.Value, m_texCoord, lastAngle, currentAngle, m_color, spacing);
    }

    [PunRPC]
    private void EndLine()
    {
        lastDrawPosition = null;
    }

    [PunRPC]
    /*
    Description : Handle all variable that need to be sync when marker is colliding with the surface.
    Argument : aHitPoint : Hit point where the raycast hit to store through all the scene
    Note : this function will raycast once through all the scene to get the paint receiver reference.
    */
    private void BroadcastIsColliding(Vector3 aHitPoint)
    {
        m_hitPoint = aHitPoint;
        transform.parent = null;
        m_isColliding = true;
        Ray ray = new Ray(transform.position - (transform.forward * M_RAYCAST_OFFSET_DISTANCE), transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, M_RAYCAST_DISTANCE, m_layerMask, QueryTriggerInteraction.Ignore))
        {
            m_paintReceiver = hit.collider.gameObject.GetComponent<PaintReceiver>();
        }
    }

    [PunRPC]
    /*
    Description : Handle if there's a change in PaintReceiver reference.
    */
    private void ChangePaintReceiver()
    {
        Ray ray = new Ray(transform.position - (transform.forward * M_RAYCAST_OFFSET_DISTANCE), transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, M_RAYCAST_DISTANCE, m_layerMask, QueryTriggerInteraction.Ignore))
        {
            m_paintReceiver = hit.collider.gameObject.GetComponent<PaintReceiver>();
        }
    }

    [PunRPC]
    /*
    Description : Handle snapping back to controller
    */
    private void HoldToHand()
    {
        transform.parent = m_rightController.transform;
        m_isColliding = false;

        transform.localPosition = m_positionOffset;
        transform.localRotation = Quaternion.Euler(m_rotationOffset);
    }

    [PunRPC]
    private void BroadcastPencilPositionAndRotation(Vector3 aPosition, Quaternion aRotation)
    {
        transform.position = aPosition;
        transform.rotation = aRotation;
    }

    [PunRPC]
    private void ChangeBrushColor(float aRedValue, float aGreenValue, float aBlueValue, float aAlphaValue)
    {
        m_color = new Color(aRedValue, aGreenValue, aBlueValue, aAlphaValue);
        if (m_markerColoredRenderer != null)
        {
            m_markerColoredRenderer.material.color = m_color;
        }
    }

    [PunRPC]
    /*
    Description : Handle changing controller model to marker model vice versa.
    Argument : aIsActive : whether script is activated or deactivated
    */
    private void ChangeModel(bool aIsActive)
    {
        foreach (GameObject gameObject in m_gameObjectToShow)
        {
            gameObject.SetActive(aIsActive);
        }
        foreach (GameObject gameObject in m_gameObjectToHide)
        {
            gameObject.SetActive(!aIsActive);
        }

        if (!aIsActive)
        {
            HoldToHand();
        }
    }

    [PunRPC]
    public void DrawLine(Vector2? aLastDrawPosition, Vector2 aTexCoord, float aLastAngle, float aCurrentAngle, float aRed, float aGreen, float aBlue, float aAlpha, float aSpacing)
    {
        m_paintReceiver.DrawLine(stamp, aLastDrawPosition.Value, aTexCoord, aLastAngle, aCurrentAngle, new Color(aRed, aGreen, aBlue, aAlpha), aSpacing);
    }
    #endregion

    /*
    Description : Syncing the component across different scene
    Argument : aPartyManagement : a reference to party management in party leader scene
    Note : Implement ISync method
    */
    public void SyncComponent(CPartyManagement aPartyManagement)
    {
        //Find other player which selected
        List<Transform> syncedPlayers = aPartyManagement.GetSelectedPlayersTransform();
        foreach (Transform player in syncedPlayers)
        {
            //Get the same component from those player
            CWritingAndErasing[] listOfOtherComponent = player.gameObject.GetComponentsInChildren<CWritingAndErasing>(true);

            foreach (CWritingAndErasing script in listOfOtherComponent)
            {
                //Check if the component is the same game object
                if (script.gameObject.name == gameObject.name)
                {
                    //sync script enable and color
                    script.enabled = enabled;

                    script.m_color = m_color;

                    if (script.m_markerColoredRenderer != null)
                    {
                        script.m_markerColoredRenderer.material.color = m_color;
                    }
                }
            }
        }
    }
}
