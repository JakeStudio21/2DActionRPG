using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaEntrance : MonoBehaviour
{
    [SerializeField] private string transitionName;

    private void Start() {
        var playerController = FindObjectOfType<PlayerController>();
        if (transitionName == SceneManagement.Instance.SceneTransitionName) {
            playerController.transform.position = this.transform.position;
            CameraController.Instance.SetPlayerCameraFollow();
            // UIFade.Instance.FadeToClear();
        }
    }
}
