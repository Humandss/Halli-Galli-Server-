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
        playerID = loginInput.text;
        loginManager.Login(playerID);
    }
}
