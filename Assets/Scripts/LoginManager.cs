using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    public TCPClient tcpClient;
    public LoginUIManager loginUIManager;

    public static LoginManager Instance;

    private void Awake()
    {
        //TCP 클라이언 오브젝트 생성
        if (tcpClient == null)
        {
            tcpClient = FindObjectOfType<TCPClient>();
        }
        Instance = this;

    }
    public void Login(string playerID)
    {
        tcpClient.SendLoginReqPacket(playerID);
    }
    public void isLoginSuccess(bool isSuccess)
    {
        if (isSuccess)
        {
            Debug.Log("로그인 성공! 잠시만 기다려주세요");
            SceneManager.LoadScene("InGame");
        }
        else
        {
            Debug.Log("로그인 실패! 다시 시도해주세요");
        }
    }
    

}
