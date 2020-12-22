using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
//import the network library
using UnityEngine.Networking;


//ASKNAME - ask for nickname
//DC - disconnect
//CNN - changed nickname for the player
//ASKPOSITION - to change position for the player
//position is updated every frame


public class serverScript : MonoBehaviour
{

    public string serverHost = "127.0.0.1";
    public int port = 5701;
    public int hostid;
    //which one is tcp and which one is udp
    public int reliablechannel, unreliablechannel;

    public bool isStarted = false;

    //to modify update rate
    private float lastMovementUpdate;
    private float movementUpdateRate = 0.2f;

    //to show a network error if I cannot connect
    private byte error;

    //this class will denote the clients on the server
    public class ServerClient
    {
        public int connectionid;
        public string nickname;
        public Vector3 playerposition;
    }

    List<ServerClient> connectedClients = new List<ServerClient>();

    // Use this for initialization
    void Start()
    {

        //initialize the network transport
        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();

        reliablechannel = cc.AddChannel(QosType.Reliable);
        unreliablechannel = cc.AddChannel(QosType.Unreliable);

        //I can accept a max of 100 clients
        HostTopology networktopology = new HostTopology(cc, 100);

        hostid = NetworkTransport.AddHost(networktopology, port, null);

        //Debug.Log(hostid);
        //this means the server has started
        isStarted = true;

    }

    // Update is called once per frame
    void Update()
    {
        //the operations that are going to happen while the server is running

        if (!isStarted)
            return;

        int recievedHostId, connectionId, channelId, dataSize;
        int bufferSize = 1024;
        byte error;
        byte[] receiveBuffer = new byte[bufferSize];

        NetworkEventType recData = NetworkTransport.Receive(
            out recievedHostId,
            out connectionId,
            out channelId,
            receiveBuffer,
            bufferSize,
            out dataSize,
            out error
            );

        Debug.Log(((NetworkError)error).ToString());



        //switch statement based on the kind of network event we receive
        switch (recData)
        {
            case NetworkEventType.Nothing:
              
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Player " + connectionId + " has connected to the server now!");
                //method to handle tracking the newly connected player
                OnConnection(connectionId);
                break;
            case NetworkEventType.DataEvent:
                string sentMessage = Encoding.Unicode.GetString(receiveBuffer, 0, dataSize);
                //method to handle data transfer from the client
                //I need to parse the data sent by the client
                //step 1, split the message on the | symbol
                Debug.Log(sentMessage);
                string[] messages = sentMessage.Split('|');
                //this is what
                switch(messages[0])
                {
                    //the case where the nickname is being set
                    case "NAMEIS":
                        OnSetNickname(connectionId, messages[1]);
                        break;
                    case "MYPOSITION":
                        OnUpdatePosition(connectionId, float.Parse(messages[1]), float.Parse(messages[2]));
                        break;
                    default:
                        Debug.Log("message not parsed correctly");
                        break;

                }
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Player " + connectionId + " has disconnected");
                OnDisconnection(connectionId);
                //method to handle removing the connected player
                break;
        }
        //last time i know a movement happened
        if (Time.time - lastMovementUpdate > movementUpdateRate)
        {
            lastMovementUpdate = Time.time;
            string message = "ASKPOSITION|";
            foreach (ServerClient c in connectedClients)
            {
                message += c.connectionid.ToString() +
                    '%' + c.playerposition.x.ToString() +
                    '%' + c.playerposition.y.ToString() +
                    '|';
            }
            message = message.Trim('|');
            Send(message, unreliablechannel, connectedClients);
        }
    }





    void OnConnection(int connectionID)
    {
        //add the client to the client list
        ServerClient newClient = new ServerClient();
        newClient.connectionid = connectionID;
        newClient.nickname = "TEMP";
        connectedClients.Add(newClient);
        //refresh the nickname for all the clients
        string messageToClients = "ASKNAME|" + connectionID + '|';
        foreach (ServerClient client in connectedClients)
        {
            messageToClients += client.nickname + '%' + client.connectionid + '|';
        }
        messageToClients = messageToClients.Trim('|');

        Send(messageToClients, reliablechannel, connectionID);
    }

    void OnDisconnection(int connectionID)
    {
        connectedClients.Remove(connectedClients.Find(x => x.connectionid == connectionID));
        //tell everyone that the player has disconnected
        Send("DC|" + connectionID, reliablechannel, connectedClients);
    }

    void OnSetNickname(int connectionID, string playername)
    {
        connectedClients.Find(x => x.connectionid == connectionID).nickname = playername;
        //tell everyone that the new nickname for the player is the following
        Send("CNN|" + playername + '|' + connectionID, reliablechannel, connectedClients);
    }

    //as soon as the player moves, this is where the movement of the other player happens
    void OnUpdatePosition(int connectionID, float x, float y)
    {
        //fejn trid issir il-lerp kieku
        connectedClients.Find(a => a.connectionid == connectionID).playerposition = new Vector3(x, y, 0);
    }

    //send to one client
    void Send(string message, int channelID, int connectionID)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(connectedClients.Find(x => x.connectionid == connectionID));
        Send(message, channelID, c);
    }

    //send to all clients
    void Send(string message, int channelID, List<ServerClient> allClients)
    {
        byte[] msg = Encoding.Unicode.GetBytes(message);

        foreach (ServerClient sc in allClients)
        {
            NetworkTransport.Send(hostid, sc.connectionid, channelID, msg, message.Length * sizeof(char), out error);
        }
    }





}
