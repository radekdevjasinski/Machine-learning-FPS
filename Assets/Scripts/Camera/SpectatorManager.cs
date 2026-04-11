using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorManager : MonoBehaviour
{
    [Header("Components")]
    public Camera mainCamera;
    public FreeCam freeCam;

    public List<Transform> targetsHeads = new List<Transform>();

    [Header("Input Actions")]
    public InputActionReference freeCamToggleAction;
    public InputActionReference nextBotAction;
    public InputActionReference prevBotAction;

    private int currentTargetIndex = -1;
    private bool isFreeCam = true;

    void OnEnable()
    {
        freeCamToggleAction.action.Enable();
        nextBotAction.action.Enable();
        prevBotAction.action.Enable();

        freeCamToggleAction.action.performed += _ => EnableFreeCam();
        nextBotAction.action.performed += _ => NextTarget();
        prevBotAction.action.performed += _ => PrevTarget();
        PerspectiveController.OnCharacterAppear += AddTarget;
        PerspectiveController.OnCharacterDisappear += RemoveTarget;
    }

    private void OnDisable()
    {
        freeCamToggleAction.action.Disable();
        nextBotAction.action.Disable();
        prevBotAction.action.Disable();

        freeCamToggleAction.action.performed -= _ => EnableFreeCam();
        nextBotAction.action.performed -= _ => NextTarget();
        prevBotAction.action.performed -= _ => PrevTarget();
        PerspectiveController.OnCharacterAppear -= AddTarget;
        PerspectiveController.OnCharacterDisappear -= RemoveTarget;
    }

    void AddTarget(Transform head)
    {
        if (!targetsHeads.Contains(head))
        {
            targetsHeads.Add(head);
        }
    }

    void RemoveTarget(Transform head)
    {
        int removedIndex = targetsHeads.IndexOf(head);

        if (removedIndex == -1) return;

        if (!isFreeCam && currentTargetIndex == removedIndex)
        {
            currentTargetIndex = -1;
            EnableFreeCam();
        }

        targetsHeads.RemoveAt(removedIndex);

        if (removedIndex < currentTargetIndex)
        {
            currentTargetIndex--;
        }

        if (currentTargetIndex >= targetsHeads.Count)
        {
            currentTargetIndex = targetsHeads.Count - 1;
        }
    }

    void Start()
    {
        EnableFreeCam();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PerspectiveController[] existingTargets = FindObjectsByType<PerspectiveController>();
        foreach (var target in existingTargets)
        {
            AddTarget(target?.GetHead());
        }
    }

    private void EnableFreeCam()
    {
        isFreeCam = true;
        mainCamera.transform.SetParent(null);
        freeCam.enabled = true;
        freeCam.SetPosition(new Vector3(0, 10, 0), Quaternion.Euler(75, 0, 0));

        if (currentTargetIndex >= 0 && currentTargetIndex < targetsHeads.Count)
        {
            DeactivateTarget(targetsHeads[currentTargetIndex]);
        }
    }

    private void NextTarget()
    {
        if (targetsHeads.Count == 0) return;

        if (!isFreeCam) DeactivateTarget(targetsHeads[currentTargetIndex]);

        isFreeCam = false;

        currentTargetIndex = (currentTargetIndex + 1) % targetsHeads.Count;

        ActivateTarget(targetsHeads[currentTargetIndex]);
    }

    private void PrevTarget()
    {
        if (targetsHeads.Count == 0) return;

        if (!isFreeCam) DeactivateTarget(targetsHeads[currentTargetIndex]);

        isFreeCam = false;
        currentTargetIndex = (currentTargetIndex - 1 + targetsHeads.Count) % targetsHeads.Count;

        ActivateTarget(targetsHeads[currentTargetIndex]);
    }

    private void ActivateTarget(Transform head)
    {
        freeCam.enabled = false;

        mainCamera.transform.SetParent(head);
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;

        PerspectiveController perspective = head.GetComponentInParent<PerspectiveController>();
        if (perspective != null) perspective.SetHideView();

        PlayerController player = head.GetComponentInParent<PlayerController>();
        if (player != null) player.enabled = true;
    }

    private void DeactivateTarget(Transform head)
    {
        if (head == null) return;

        PerspectiveController perspective = head.GetComponentInParent<PerspectiveController>();
        if (perspective != null) perspective.SetDefaultView();

        PlayerController player = head.GetComponentInParent<PlayerController>();
        if (player != null) player.enabled = false;
    }
}