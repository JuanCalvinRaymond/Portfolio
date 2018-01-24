//#define DEBUGGING_ELEVATOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;

public class CElevatorSystem : MonoBehaviour
{
    private bool m_isDirectionIsUp;
    private bool m_canMove;

    //List of which floor the elevator need to stop.
    private List<SFloorPointData> m_upFloorPointQueue;
    private List<SFloorPointData> m_downFloorPointQueue;

    //Current point to go to.
    private SFloorPointData m_targetPoint;

    private int m_currentFloor;
    
    //Variable to tweak
    public float m_movementSpeed;
    public float m_arrivalThreshold = 0.01f;

    public CFloorPoint m_startingFloor;

    public CElevatorDoor m_elevatorDoor;

    //Various event to communicate to another scripts.
    public UnityEvent m_onArrived;
    public UnityEvent m_onFloorChanged;
    public UnityEvent m_onStartMoving;
    public UnityEvent m_onSameFloorCalled;

    /// <summary>
    /// Properties to set when elevator will be able to move
    /// </summary>
    public bool PCanMove
    {
        set
        {
            m_canMove = value;
        }
    }

    /// <summary>
    /// Properties to change current floor variable and call OnFloorChanged event after setting it.
    /// </summary>
    public int PCurrentFloor
    {
        set
        {
            m_currentFloor = value;
            m_onFloorChanged.Invoke();
        }

        get
        {
            return m_currentFloor;
        }
    }

    /// <summary>
    /// Properties to change the outer elevator door when elevator arrive at a different floor.
    /// </summary>
    public CElevatorDoor PElevatorDoor
    {
        get
        {
            return m_elevatorDoor;
        }
    }

    #region SETUP
    private void Awake()
    {
        m_upFloorPointQueue = new List<SFloorPointData>();
        m_downFloorPointQueue = new List<SFloorPointData>();

        m_canMove = true;
    }

    private void Start()
    {
        if (m_startingFloor != null)
        {
            transform.position = m_startingFloor.PFloorPointData.m_floorTransform.position;

            //Set the current floor to invoke the event
            PCurrentFloor = m_startingFloor.PFloorPointData.m_floorIndex;
        }
    }
    #endregion

    /// <summary>
    /// Lerp elevator if it can move and there is a target to move. Call Arrive when elevator finish lerping
    /// </summary>
    private void Update()
    {
        if (m_canMove && m_targetPoint.m_floorTransform != null)
        {
            Vector3 newPosition = transform.position;

            newPosition.y = Mathf.Lerp(newPosition.y, m_targetPoint.m_floorTransform.position.y, m_movementSpeed * Time.deltaTime);

            transform.position = newPosition;

            if (CUtilityMath.AlmostEquals(transform.position.y, m_targetPoint.m_floorTransform.position.y, m_arrivalThreshold))
            {
                Arrive();
            }
        }
    }

    #region EVENTS

    /// <summary>
    /// Manage everything when elevator arrive on the point
    /// </summary>
    private void Arrive()
    {
        #if DEBUGGING_ELEVATOR
        print("Arrived : " + m_targetPoint.m_floorIndex);
        #endif

        //Removing point based on if the direction is up or down
        if (m_isDirectionIsUp)
        {
            if (m_upFloorPointQueue.Count >= 1)
            {
                m_upFloorPointQueue.RemoveAt(0);
            }
        }
        else
        {
            if (m_downFloorPointQueue.Count >= 1)
            {
                m_downFloorPointQueue.RemoveAt(0);
            }
        }

        m_targetPoint.SetTogglesWhenElevatorOnFloor(m_isDirectionIsUp);

        #if DEBUGGING_ELEVATOR
        print("Invoked on arrived : " + m_targetPoint.m_floorIndex);
        #endif

        m_onArrived.Invoke();
        SetTargetFloor();
    }

    private void CallStartedMove()
    {
        if (m_targetPoint.m_floorTransform == null)
        {
            m_onStartMoving.Invoke();
        }
    }
    #endregion

    #region SETTING_TARGET_FLOOR

    /// <summary>
    /// Use to determine which point is the next point
    /// </summary>
    private void SetTargetFloor()
    {
        #if DEBUGGING_ELEVATOR
        print("Set Target Floor : " + m_upFloorPointQueue.Count + " " + m_downFloorPointQueue.Count + " " + m_isDirectionIsUp);
        #endif

        if (m_upFloorPointQueue.Count == 0 && m_downFloorPointQueue.Count == 0)
        {
            #if DEBUGGING_ELEVATOR
            print("Both list are empty, switch to idle state");
            #endif

            m_targetPoint.m_floorTransform = null;
            return;
        }

        if (m_upFloorPointQueue.Count == 0)
        {
            if (m_isDirectionIsUp)
            {
                #if DEBUGGING_ELEVATOR
                print("Up list is empty switch direction to down");
                #endif

                m_isDirectionIsUp = false;
            }
        }

        if (m_downFloorPointQueue.Count == 0)
        {
            if (!m_isDirectionIsUp)
            {
                #if DEBUGGING_ELEVATOR
                print("Down list is empty switch direction to up");
                #endif

                m_isDirectionIsUp = true;
            }
        }

        SFloorPointData tempFloorPoint;
        if (m_isDirectionIsUp == true)
        {
            tempFloorPoint = m_upFloorPointQueue[0];
        }
        else
        {
            tempFloorPoint = m_downFloorPointQueue[0];
        }


        #if DEBUGGING_ELEVATOR
        print("Setting target point to : " + tempFloorPoint.m_floorIndex);
        #endif

        m_targetPoint = tempFloorPoint;
    }
    
    /// <summary>
    /// Use to adding point based on if the point is higher or lower
    /// </summary>
    /// <param name="aFloorData"> Which floor point to add</param>
    public void AddPointToQueue(SFloorPointData aFloorData)
    {
        if (aFloorData.m_floorIndex > m_currentFloor)
        {
            #if DEBUGGING_ELEVATOR
            print("Adding to Up list : " + aFloorData.m_floorIndex + " list has: " + m_upFloorPointQueue.Count);
            #endif

            AddingToUpList(aFloorData);
        }
        else if (aFloorData.m_floorIndex < m_currentFloor)
        {
            AddingToDownList(aFloorData);

            #if DEBUGGING_ELEVATOR
            print("Adding to down list : " + aFloorData.m_floorIndex + " list has: " + m_downFloorPointQueue.Count);
            #endif
        }
        else
        {
            if (m_isDirectionIsUp && !m_upFloorPointQueue.Contains(aFloorData))
            {
                #if DEBUGGING_ELEVATOR
                print("Same Floor " + "Adding to down list : " + aFloorData.m_floorIndex + " list has: " + m_downFloorPointQueue.Count);
                #endif

                AddingToDownList(aFloorData);
            }
            else if (!m_isDirectionIsUp && !m_downFloorPointQueue.Contains(aFloorData))
            {
                #if DEBUGGING_ELEVATOR
                print("Same Floor " + "Adding to up list : " + aFloorData.m_floorIndex + " list has: " + m_downFloorPointQueue.Count);
                #endif

                AddingToUpList(aFloorData);
            }

            //OPEN DOORs
            m_onSameFloorCalled.Invoke();
            if (m_elevatorDoor.PCanBeOpened)
            {
                aFloorData.SetTogglesState(false);
            }
        }
    }
    
    /// <summary>
    /// Use to adding point to go up
    /// </summary>
    /// <param name="aFloorData">Which floor to add</param>
    /// Note : Use by floor button
    public void AddingToUpList(SFloorPointData aFloorData)
    {
        if (m_upFloorPointQueue.Contains(aFloorData))
        {
            return;
        }

        if (m_downFloorPointQueue.Count == 0)
        {
            m_isDirectionIsUp = false;
        }

        m_upFloorPointQueue.Add(aFloorData);
        m_upFloorPointQueue.Sort(delegate (SFloorPointData aFirstArgumentToCompare, SFloorPointData aSecondArgumentToCompare)
        {
            return aFirstArgumentToCompare.m_floorIndex.CompareTo(aSecondArgumentToCompare.m_floorIndex);
        });

        CallStartedMove();

        SetTargetFloor();
    }
    
    /// <summary>
    /// Use to adding point to go down
    /// </summary>
    /// <param name="aFloorData">Which floor to add</param>
    /// Note : Use by floor button
    public void AddingToDownList(SFloorPointData aFloorData)
    {
        if (m_downFloorPointQueue.Contains(aFloorData))
        {
            return;
        }

        if (m_upFloorPointQueue.Count == 0)
        {
            m_isDirectionIsUp = true;
        }

        m_downFloorPointQueue.Add(aFloorData);
        m_downFloorPointQueue.Sort(delegate (SFloorPointData aFirstArgumentToCompare, SFloorPointData aSecondArgumentToCompare)
        {
            return aFirstArgumentToCompare.m_floorIndex.CompareTo(aSecondArgumentToCompare.m_floorIndex);
        });
        m_downFloorPointQueue.Reverse();

        CallStartedMove();

        SetTargetFloor();
    }

    #endregion

    public bool GetHasTargetsLeft()
    {
        return m_targetPoint.m_floorTransform != null;
    }
}