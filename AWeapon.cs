using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Description: An abstract weapon class that will have diff properties and know how to shoot and reload
Parameters(Optional):
Creator: Juan Calvin Raymond
Creation Date: 10-09-2016
Extra Notes:
*/
public abstract class AWeapon : MonoBehaviour
{
    //Physics variables
    private CWeaponPhysics m_weaponPhysics;
    private CWeaponDataTracker m_weaponDataTracker;

    //Scoring system variables
    private SWeaponData m_weaponData;

    //Layer mask of what layer the weapon won't collide
    protected int m_layerMask;

    //timer counter for firerate calculation
    protected int m_currentAmmo = 12;
    protected float m_fireRateTimer;
    protected float m_reloadTimer;
    protected bool m_isReloading;//Since the ammo is set to 0 when the reload start this variable could proably be deleted
                                 // but is being kept in order to have a clear easy to read sign that the player is reloading

    //Time when player press fire input
    protected float m_timeSinceLastShot;

    //Bool for checking if weapon is viable to count as performing trick
    protected bool m_isActive;

    protected Transform m_head;

    public EWeaponTypes m_weaponType = EWeaponTypes.OasisSeagull;

    //Common Variable for weapon
    [Header("Weapon properties")]
    public int m_maxAmmo = 12;
    public int m_damage = 100;
    public float m_fireRate = 0.1f;
    public float m_reloadTime = 2.5f;
    public float m_maxShootDistance = 100.0f;

    public bool m_automaticWeapon = false;

    //Position of empty gameobject for raycast start point and particle startpoint
    [Header("Firing Points")]
    public Transform m_raycastPoint;
    public Transform m_particlePoint;

    [Header("Trick Detections")]
    public float m_activeDistance = 4.0f;
    [Tooltip("This is the local height in relation to the platform position")]
    public float m_activeHeight = 0.2f;

    public delegate void delegFire(int aCurrentAmmo, EWeaponHand aWeaponHand);
    public event delegFire OnFire;
    public event delegFire OnFireOutOfAmmo;

    public delegate void delegReload(int aCurrentAmmo);
    public event delegReload OnStartReload;
    public event delegReload OnEndReload;

    public delegate void delegOnTargetHit(List<GameObject> aListOfTargetThatHit, EWeaponHand aWeaponHand, float aTimeWhenShot = 0.0f);
    public event delegOnTargetHit OnTargetHit;

    //Delegate and event that takes into account only the closest object hit by a shot
    public delegate void delegClosestObjectShot(GameObject aObjectHit, Vector3 aHitPosition, Vector3 aHitNormal, int aWeaponDamage);
    public event delegClosestObjectShot OnObjectShotClosest;

    //Note: This event gets called if no target is hit by that shot
    public delegate void delegateNonTargetShot(GameObject aObjectsShot, Vector3 aHitPositions, Vector3 aHitNormal, 
        
        int aWeaponDamage, Vector2 aHitUV, Collider aCollider);
    public event delegateNonTargetShot OnNonTargetObjectsShot;

    public int PLayerMask
    {
        get
        {
            return m_layerMask;
        }
    }

    public Vector3 PWeaponVelocity
    {
        get
        {
            //If there are weapon physics
            if (m_weaponPhysics != null)
            {
                //Get the velocity
                return m_weaponPhysics.PVelocity;
            }
            else//If there are no weapons physics 
            {
                //Return velocity of 0
                return Vector3.zero;
            }
        }
    }

    public Vector3 PWeaponAngularVelocity
    {
        get
        {
            //If there are weapon physics
            if (m_weaponPhysics != null)
            {
                //Get the angular velocity
                return m_weaponPhysics.PAngularVelocity;
            }
            else//If there are no weapons physics 
            {
                //Return velocity of 0
                return Vector3.zero;
            }
        }
    }

    public CWeaponPhysics PWeaponPhysics
    {
        get
        {
            return m_weaponPhysics;
        }
    }
    
    public CWeaponDataTracker PWeaponDataTracker
    {
        get
        {
            return m_weaponDataTracker;
        }
    }

    public GameObject PHoldingHandGameObject
    {
        get
        {
            //If  there are weapon physics
            if (m_weaponPhysics != null)
            {
                //Get its holding hand
                return m_weaponPhysics.PHoldingHandGameObject;
            }
            else//If there are no weapon physics
            {
                //Return there is no weapon hand
                return null;
            }
        }
    }

    public EWeaponHand PHoldingHand
    {
        get
        {
            //If  there are weapon physics
            if (m_weaponPhysics != null)
            {
                //Get its holding hand
                return m_weaponPhysics.PHoldingHand;
            }
            else//If there are no weapon physics
            {
                //Return there is right handed weapon
                return EWeaponHand.RightHand;
            }
        }
    }

    public EWeaponPhysicsState PWeaponPhysiscsState
    {
        get
        {
            //If there are weapon physics
            if (m_weaponPhysics != null)
            {
                //Get their state
                return m_weaponPhysics.PWeaponPhysiscsState;
            }
            else//If there are no weapons physics 
            {
                //Return the weapon is grabbed
                return EWeaponPhysicsState.Grabbed;
            }
        }
    }

    public EWeaponTypes PWeaponType
    {
        get
        {
            return m_weaponType;
        }

        set
        {
            m_weaponType = value;
        }
    }

    public SWeaponData PWeaponData
    {
        get
        {
            return m_weaponData;
        }
    }

    public int PMaxAmmo
    {
        get
        {
            return m_maxAmmo;
        }
    }

    public int PCurrentAmmo
    {
        get
        {
            return m_currentAmmo;
        }
    }

    public Transform PRaycastPoint
    {
        set
        {
            m_raycastPoint = value;

            CSelectingMenu menuSelecter = GetComponent<CSelectingMenu>();
            if (menuSelecter != null)
            {
                menuSelecter.PRaycastPoint = m_raycastPoint;
            }
        }
    }

    public Transform PParticlePoint
    {
        get
        {
            //If there is a particle point
            if (m_particlePoint != null)
            {
                //Return the particle point
                return m_particlePoint;
            }
            else//If there is no particle point
            {
                //Return the raycast point
                return m_raycastPoint;
            }
        }
    }

    public Transform PHead
    {
        set
        {
            m_head = value;

            CSelectingMenu menuSelecter = GetComponent<CSelectingMenu>();
            if (menuSelecter != null)
            {
                menuSelecter.PHead = m_head;
            }
        }
    }

    /*
    Description: Initialize all the Variable
    Creator: Juan Calvin Raymond
    Creation Date: 10-09-2016
    */
    protected virtual void Awake()
    {
        //initialize timer, i set it to 0 so player can shoot it immediately after they went into the gaem
        m_fireRateTimer = 0;
        m_reloadTimer = 0;
        m_isReloading = false;

        m_currentAmmo = m_maxAmmo;

        m_weaponPhysics = GetComponent<CWeaponPhysics>();
        m_weaponDataTracker = GetComponent<CWeaponDataTracker>();

        //Set the collision mask
        m_layerMask = ~LayerMask.GetMask("Projectile", "Ignore Raycast");
    }

    /*
    Description: Set all Variable
    Creator: Juan Calvin Raymond
    Creation Date: 10-09-2016
    */
    protected virtual void Start()
    {
        if (CSettingsStorer.PInstanceSettingsStorer.PInputMethod == EControllerTypes.ViveController)
        {
            PHead = CGameManager.PInstanceGameManager.PPlayerScript.GetComponentInChildren<SteamVR_Camera>().gameObject.transform;
        }
        else
        {
            //If there is a player script
            if (CGameManager.PInstanceGameManager.PPlayerScript != null)
            {
                PHead = CGameManager.PInstanceGameManager.PPlayerScript.m_playerHeadNonVR.transform;
            }
        }

        //If there is a moving platform
        if (CGameManager.PInstanceGameManager.PMovingPlatform != null)
        {
            //Set the platform as the parent of the weapon
            transform.parent = CGameManager.PInstanceGameManager.PMovingPlatform.gameObject.transform;
        }


        //Set the head raycast point
        PRaycastPoint = m_raycastPoint;
    }

    /*
    Description: Update Function
    Creator: Juan Calvin Raymond
    Creation Date: 10-09-2016
    */
    protected virtual void Update()
    {
        //Update the firerate timer
        m_fireRateTimer -= Time.deltaTime;

        //Reloading
        if (m_isReloading == true)
        {
            Reloading();
        }

        //Check if weapon is active or not
        CheckActive();

        //Update weapon data
        UpdateWeaponData();
    }

    /*
    Description: Checking if weapon is in a certain distance from player and above platform's ground
    Creator: Juan Calvin Raymond
    Creation Date: 16 Jan 2017
    */
    private void CheckActive()
    {
        //If the game manager is valid
        if (CGameManager.PInstanceGameManager != null)
        {
            //If the player script and the player object (the root object of the player, the platform) are valid
            if (CGameManager.PInstanceGameManager.PPlayerScript != null && CGameManager.PInstanceGameManager.PPlayerObject != null)
            {
                //Calculate weapon distance from player
                float distanceFromPlayer = Vector3.Distance(CGameManager.PInstanceGameManager.PPlayerScript.gameObject.transform.position, transform.position);

                //If distance is in active distance and higher then the platform ground then set active to true
                m_isActive = distanceFromPlayer < m_activeDistance && transform.localPosition.y > m_activeHeight ? true : false;
            }
        }
    }

    /*
    Description: Function to update the weapon data struct so that it can be used in the scoring system
    Creator: Alvaro Chavez Mixco
    Creation Date:  Wednesday, December 1, 2016
    Extra Notes: 
    */
    protected void UpdateWeaponData()
    {
        //Update the content of the weapon data struct
        m_weaponData.m_angularVelocity = PWeaponAngularVelocity;
        m_weaponData.m_linearVelocity = PWeaponVelocity;
        m_weaponData.m_physicState = PWeaponPhysiscsState;
        m_weaponData.m_weaponForwardDirection = m_raycastPoint.transform.forward;
        m_weaponData.m_playerForwardDirection = m_head.forward;
        m_weaponData.m_playerQuaternion = m_head.rotation;
        m_weaponData.m_playerPosition = m_head.position;
        m_weaponData.m_playerRotation = m_head.localRotation.eulerAngles;
        m_weaponData.m_holdingHand = PHoldingHand;
        m_weaponData.m_weaponPosition = transform.position;
        m_weaponData.m_weaponRotation = transform.localRotation.eulerAngles;
        m_weaponData.m_active = m_isActive;
    }

    /*
    Description: Updating reload timer when timer finish call OnReload event
    Creator: Juan Calvin Raymond
    Creation Date: 20 Dec 2016
    */
    protected void Reloading()
    {
        //Increment timer
        m_reloadTimer += CGameManager.PInstanceGameManager.GetScaledDeltaTime();

        //If timer is finish
        if (m_reloadTimer > m_reloadTime)
        {
            //Call Reload Mechanic function
            ReloadMechanics();

            //Call OnReload event
            if (OnEndReload != null)
            {
                OnEndReload(m_currentAmmo);
            }

            //Set bool to false
            m_isReloading = false;
        }
    }

    /*
    Description: Setting ammo back to max ammo amount
    Creator: Juan Calvin Raymond
    Creation Date: 20 Dec 2016
    Extra: Can be override if some weapon have a unique reload mechanic
    */
    protected virtual void ReloadMechanics()
    {
        m_currentAmmo = m_maxAmmo;
    }

    /*
    Description: Handling firing when weapon is fired
    Parameter : aCurrentAmmo : weapon's current ammo
    Creator: Juan Calvin Raymond
    Creation Date: 10-09-2016
    */
    protected virtual void HandleFiring(int aCurrentAmmo)
    {
        //reset the timer
        m_fireRateTimer = m_fireRate;

        //Get time since last shot
        m_timeSinceLastShot = Time.time;

        //Abstract function that actually handles "shooting" the weapon
        FireMechanics();

    }

    /*
    Description: Function to call event on the closest object that was hit.
    Parameters: GameObject aObjectHit - The object that was hit
                Vector3 aHitPosition - The position where the shot hit
                Vector3 aHitNormal - The normal of the hit
                int aWeaponDamage - The damage made by the weapon
    Creator: Alvaro Chavez Mixco
    Creation Date: Thursday, March 16th, 2017
    Extra Notes: Made into a function so that child classes can call event
    */
    public void DetectedObjectShotClosest(GameObject aObjectHit, Vector3 aHitPosition, Vector3 aHitNormal, int aWeaponDamage)
    {
        if (OnObjectShotClosest != null)
        {
            OnObjectShotClosest(aObjectHit, aHitPosition, aHitNormal, aWeaponDamage);
        }
    }

    /*
    Description: Function to call object that was shot, that were not a target. This is presuming
                 that the weapon fire can penetrate every object it hits.
    Parameters: GameObject aObjectHit - The objects that was hit
                Vector3 aHitPosition - The positions where the object was hit
                int aWeaponDamage - The damage made by the weapon
                Vector2 aHitUV - The UV coordinates from where the object was hit
    Creator: Alvaro Chavez Mixco
    Creation Date: Thursday, March 16th, 2017
    Extra Notes: Made into a function so that child classes can call event
    */
    public void DetectedNonTargetObjectShot(GameObject aObjectsHit, Vector3 aHitPositions, Vector3 aHitNormal, int aWeaponDamage, Vector2 aHitUV, Collider aCollider)
    {
        if (OnNonTargetObjectsShot != null)
        {
            OnNonTargetObjectsShot(aObjectsHit, aHitPositions, aHitNormal, aWeaponDamage, aHitUV,aCollider);
        }
    }

    /*
    Description: Function that check if weapon is able to fire and call OnFire function          
    Creator: Juan Calvin Raymond
    Creation Date: 20 Dec 2016
    */
    public void Fire()
    {
        //if firerate timer is finished
        if (m_fireRateTimer < 0)
        {
            //if the weapon still have ammo and not reloading
            if (m_currentAmmo > 0 && m_isReloading == false)
            {
                if (CGameManager.PInstanceGameManager.PGameState == EGameStates.Play)
                {
                    //Decrease current ammo
                    m_currentAmmo--;
                }

                //Fire the weapon
                HandleFiring(m_currentAmmo);

                if (OnFire != null)
                {
                    OnFire(m_currentAmmo, PHoldingHand);
                }
            }
            else//If the weapon doesn't have ammo or is reloading
            {
                //If the player tried to shoot when out of ammo
                //If event is for it has suscribes
                if (OnFireOutOfAmmo != null)
                {
                    //Call event
                    OnFireOutOfAmmo(m_currentAmmo, PHoldingHand);
                }
            }
        }
    }

    /*
    Description: Make current ammo to max ammo (basic reload function)
    Creator: Juan Calvin Raymond
    Creation Date: 10-09-2016
    Extra Notes: haven't implement reload time, will implement it in the future
    */
    public virtual void Reload()
    {
        //If user is on play allow reloads
        if (CGameManager.PInstanceGameManager.PGameState == EGameStates.Play)
        {
            //If the user is not reloading
            if (m_isReloading == false && m_currentAmmo < m_maxAmmo)
            {
                //Reset timer
                m_reloadTimer = 0.0f;

                //Set the user as reloading
                m_isReloading = true;

                //Set ammo to 0, to signal user is currently reloading
                m_currentAmmo = 0;

                //If there are suscribers to the event
                if (OnStartReload != null)
                {
                    //Call event
                    OnStartReload(m_currentAmmo);
                }
            }
        }
    }


    /*
    Description: Call OnTargetHit event
    Creator: Juan Calvin Raymond
    Creation Date: 20 Mar 2017
    Extra Notes: Made this function so child script can call the event
    */
    public void TargetHit(List<GameObject> aListOfTargetHit)
    {
        if (OnTargetHit != null)
        {
            OnTargetHit(aListOfTargetHit, PHoldingHand, m_timeSinceLastShot);
        }
    }

    /*
    Description: Set the raycast, shooting, positon to match a specific position
    Creator: Alvaro Chavez Mixco
    Creation Date: Friday, October 1, 2016
    */
    public void SetRaycastStartPosition(Vector3 aPosition)
    {
        if (m_raycastPoint != null)
        {
            m_raycastPoint.transform.position = aPosition;
        }
    }

    /*
    Description: Fucntion to actually execute the shooting mechanics of the weapon                     
    Creator: Juan Calvin Raymond
    Creation Date: 10-09-2016
    Extra Notes: Have to be inherit, different weapon can override it for different kind of calculation.
    */
    protected abstract void FireMechanics();
}