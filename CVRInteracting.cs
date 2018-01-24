using UnityEngine;
using System.Collections;
using System;

public class CVRInteracting : AInteracting
{
    private Collider[] m_hit;

    [Tooltip("How big you want your interacting radius")]
    public float m_sphereCastRadius = 1.0f;

    private void Awake()
    {
        m_controller = GetComponentInParent<CViveController>();
    }

    private void OnEnable()
    {
        m_controller.OnInteractButtonDown += Click;
        m_controller.OnInteractButtonUp += UnClick;
    }

    protected override void OnDisable()
    {
        m_controller.OnInteractButtonDown -= Click;
        m_controller.OnInteractButtonUp -= UnClick;

        base.OnDisable();
    }

    private void Update()
    {
        //Ray ray = new Ray(transform.position, transform.forward);
        m_hit = Physics.OverlapSphere(transform.position, m_sphereCastRadius, (int)m_layerMask);

        //If something was hit
        if (m_hit.Length > 0) //if (Physics.Raycast(ray, out m_hit))
        {
            //First index is the closest object.
            IInteractable tempInteractable = m_hit[0].GetComponent<IInteractable>();

            if (tempInteractable != null)
            {
                //If the collider that got hit different from current hovered object
                if (tempInteractable != m_hoveringObject)
                {
                    //Call OnHover on the collider
                    tempInteractable.OnHover(this, gameObject);

                    //Set current hovered object to new object
                    m_hoveringObject = tempInteractable;

                    Hover();
                }

            }
        }
        else
        {
            //If Trigger is not pressed and there is hovered object
            if (!m_controller.PIsInteractButtonPress && m_hoveringObject != null)
            {
                //Call OnUnHover function and set hovering object to null
                m_hoveringObject.OnUnhover();
                m_hoveringObject = null;

                UnHover();

            }
        }

        //If controller trigger is pressed and there is hovered object
        if (m_controller.PIsInteractButtonPress && m_hoveringObject != null)
        {
            //Call OnClickHold function
            m_hoveringObject.OnClickHold(this, gameObject);
        }
    }
}
