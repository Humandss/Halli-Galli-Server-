using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUIManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField loginInput;
    [SerializeField]
    private string playerID;

    public LoginManager loginManager;

    public void Start()
    {
        playerID = null;
    }

    public void onLoginButtonClicked()
    {
        if (loginInput == null || loginManager == null)
        {
            Debug.LogWarning("Login UI references are missing.");
            return;
        }

        playerID = loginInput.text?.Trim();
        if (string.IsNullOrEmpty(playerID))
        {
            Debug.LogWarning("Please enter a player ID.");
            return;
        }

        loginManager.Login(playerID);
    }
}
