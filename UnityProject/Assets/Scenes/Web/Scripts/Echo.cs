using System;
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

    private byte[] readBuff = new byte[1024];
    private string recvStr = "";
    void Start()
    {
        connectBtn.onClick.AddListener(Connection);
        sendBtn.onClick.AddListener(Send);
    }

    void Update()
    {
        text.text = recvStr;
 
    }

    public void Connection()
    {
        //Socket
        _socket = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
        //Connect
        _socket.BeginConnect("127.0.0.1", 8888, ConnectCallback, _socket);
    }

    //Connect回调
    public void ConnectCallback(IAsyncResult ar){
        try{
            Socket socket = (Socket) ar.AsyncState;
            socket.EndConnect(ar);
            Debug.Log("Socket Connect Succ");
            socket.BeginReceive( readBuff, 0, 1024, 0,
                ReceiveCallback, socket);
        }
        catch (SocketException ex){
            Debug.Log("Socket Connect fail" + ex.ToString());
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try {
            Socket socket = (Socket) ar.AsyncState;
            int count = socket.EndReceive(ar);
            string s = System.Text.Encoding.Default.GetString(readBuff, 0, count);
            recvStr = s + "\n" + recvStr;

            socket.BeginReceive( readBuff, 0, 1024, 0,
                ReceiveCallback, socket);
            Debug.Log("Socket Receive succ" );

        }
        catch (SocketException ex){
            Debug.Log("Socket Receive fail" + ex.ToString());
        }
    }

    public void Send()
    {
        string sendStr = inputField.text;
        byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
        // _socket.Send(sendBytes);

        _socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, _socket);
    }

    public void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            int count = socket.EndSend(ar);
            Debug.Log("Send succ "+ count);
            
        }
        catch (Exception e)
        {
            Debug.LogError("Send fail " + e);
        }
    }
}
