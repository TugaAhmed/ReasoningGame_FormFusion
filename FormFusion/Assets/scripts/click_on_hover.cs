using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using Tobii.XR;
public class click_on_hover : MonoBehaviour
{
    private XRBaseInteractor interactor; // The interactor (e.g., XRRayInteractor)
    private XRBaseInteractable currentHoveredInteractable; // Current hovered interactable
    private const ControllerButton TriggerButton = ControllerButton.Trigger;

    void Start()
    {
        interactor = GetComponent<XRBaseInteractor>();

        if (interactor != null)
        {
            interactor.hoverEntered.AddListener(OnHoverEnter);
            interactor.hoverExited.AddListener(OnHoverExit);
        }
    }

    void Update()
    {
        // Only trigger if hovering over a valid interactable
        if (currentHoveredInteractable != null && ControllerManager.Instance.GetButtonPressDown(TriggerButton))
        {
            if (LevelManager.Instance != null)
            {
                GameObject selectedObj = currentHoveredInteractable.gameObject;
                string objectName = selectedObj.name;

                bool isCorrect = selectedObj.CompareTag("correct"); // ✅ check the tag

                LevelManager.Instance.RecordSelection(objectName, isCorrect);
                LevelManager.Instance.NextLevel();
            }
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        currentHoveredInteractable = args.interactable;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (args.interactable == currentHoveredInteractable)
        {
            currentHoveredInteractable = null;
        }
    }

    private void OnDestroy()
    {
        if (interactor != null)
        {
            interactor.hoverEntered.RemoveListener(OnHoverEnter);
            interactor.hoverExited.RemoveListener(OnHoverExit);
        }
    }
}

