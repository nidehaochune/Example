using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using TMPro;
using UnityEngine.UI;

public class Echo : MonoBehaviour
{
    private Socket _socket;
    public TMP_InputField inputField;
    public Button sendBtn;
    public Button connectBtn;
    public TMP_Text text;
    void Start()
    {
        connectBtn.onClick.AddListener(Connect);
        sendBtn.onClick.AddListener(Send);
    }

    void Update()
    {
        
    }

    public void Connect()
    {
        //定义socket
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //connect
        _socket.Connect("127.0.0.1",8888);
    }

    public void Send()
    {
        string sendStr = inputField.text;
        byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
        _socket.Send(sendBytes);

        byte[] readBuff = new byte[1024];
        int count = _socket.Receive(readBuff);
        string recvStr = System.Text.Encoding.Default.GetString(readBuff, 0, count);
        text.text = recvStr;
        _socket.Close();
    }
}
