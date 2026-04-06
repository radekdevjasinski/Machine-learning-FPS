using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorManager : MonoBehaviour
{
    [Header("Components")]
    public Camera mainCamera;
    public FreeCam freeCam;
    public Transform[] botHeads;

    [Header("Input Actions")]
    public InputActionReference freeCamToggleAction;
    public InputActionReference nextBotAction;
    public InputActionReference prevBotAction;

    private int currentBotIndex = -1;
    private bool isFreeCam = true;

    void OnEnable()
    {
        freeCamToggleAction.action.Enable();
        nextBotAction.action.Enable();
        prevBotAction.action.Enable();

        freeCamToggleAction.action.performed += _ => EnableFreeCam();
        nextBotAction.action.performed += _ => NextBot();
        prevBotAction.action.performed += _ => PrevBot();
    }

    void Start()
    {
        //EnableFreeCam();
        NextBot();
    }

    private void EnableFreeCam()
    {
        isFreeCam = true;
        mainCamera.transform.SetParent(null);
        freeCam.enabled = true;
        freeCam.SetPosition(new Vector3(0, 10, 0), Quaternion.identity);


        PlayerController playerController = botHeads[currentBotIndex].GetComponentInParent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

    }

    private void NextBot()
    {
        if (botHeads.Length == 0) return;

        if (isFreeCam)
        {
            currentBotIndex = 0;
            isFreeCam = false;
        }
        else
        {
            currentBotIndex = (currentBotIndex + 1) % botHeads.Length;
        }

        AttachCameraToBot(currentBotIndex);
    }

    private void PrevBot()
    {
        if (botHeads.Length == 0) return;

        if (isFreeCam)
        {
            currentBotIndex = botHeads.Length - 1;
            isFreeCam = false;
        }
        else
        {
            currentBotIndex = (currentBotIndex - 1 + botHeads.Length) % botHeads.Length;
        }

        AttachCameraToBot(currentBotIndex);
    }

    private void AttachCameraToBot(int index)
    {
        freeCam.enabled = false;

        Transform targetHead = botHeads[index];
        mainCamera.transform.SetParent(targetHead);
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;

        PlayerController playerController = targetHead.GetComponentInParent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Debug.Log($"Obserwowanie bota: {targetHead.parent.name}");
    }
}