using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class clientScript : MonoBehaviour {

    //a public slot for the player I am going to create
    public GameObject playerPrefab;

    private int port = 5701;
    private int hostid;

    //which one is tcp and which one is udp
    public int reliablechannel, unreliablechannel,connectionid;

    public bool isStarted,isConnected = false;

    public float connectionTime;

    private int myclientid;

    private byte error;
    public string playerName;
    //the list of players to show
    public Dictionary<int, Player> players = new Dictionary<int, Player>();

    //inner class to store all players (clients)
    public class Player
    {
        public string playerName;
        public GameObject avatar;
        public int connectionId;

    }

    public void Connect()
    {
        //if the playername is blank, don't do anything (you could show an error)
        if (playerName == "")
            return;

        //otherwise, connect to the server
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliablechannel = cc.AddChannel(QosType.Reliable);
        unreliablechannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, 100);

        hostid = NetworkTransport.AddHost(topo, 0);
        connectionid = NetworkTransport.Connect(hostid, "127.0.0.1", port, 0, out error);

        

        connectionTime = Time.time;

        isConnected = true;
        //at this point, the client is connected to the server, and the connectionevent is sent.
        //  Debug.Log(connectionid);
        Debug.Log(((NetworkError)error).ToString());

    }

    //the client will send only to one person, which is the server
    private void Send(string message, int channelId)
    {
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostid, connectionid, channelId, msg, message.Length * sizeof(char), out error);
    }

    private void PlayerDisconnect(int connectionid)
    {
        //remove the player from the dictionary so he is no longer displayed
        Destroy(players[connectionid].avatar);
        players.Remove(connectionid);
    }


    //send the nickname to the server
    private void SendNickname(string[] serverdata)
    {
        myclientid = int.Parse(serverdata[1]);
        Send("NAMEIS|" + playerName, reliablechannel);
        //eventually we will create the other players on the server
        //create all others (lobby) - get a full list of names in the communication
        for (int i = 2; i < serverdata.Length - 1; i++)
        {
            string[] eachplayer = serverdata[i].Split('%');
            SpawnPlayer(eachplayer[0], int.Parse(eachplayer[1]));
        }


    }

    //send position updates to the server
    //serverdata is the array that i have split on the | character
    //inside serverdata there are several arrays containing connectionid%xposition%yposition which denote
    //the positions of each of the players.  Here we are updating all the player positions
    //from the server except my own, which I am going to send to the server
    private void SendPosition(string[] serverdata)
    {

        
        //if the client is not connected, do nothing
        if (!isStarted)
            return;

        
        
        //now we loop through the data that I get, skipping the first element which is the description
        for (int i =1;i<=serverdata.Length-1;i++)
        {
            string[] positiondata = serverdata[i].Split('%');

            int clientid = int.Parse(positiondata[0]);

            Debug.Log("ClientId:"+myclientid);
            //here I am checking if the positions sent by the server are NOT me
            if (myclientid != clientid)
            {
                //if not me, update position
                Vector3 otherplayerposition = Vector3.zero;
                otherplayerposition.x = float.Parse(positiondata[1]);
                otherplayerposition.y = float.Parse(positiondata[2]);
                //update the avatar
                try
                {
                    players[clientid].avatar.transform.position = otherplayerposition;
                }catch (KeyNotFoundException e)
                {
                    Debug.Log(clientid + e.Message);
                }
            }

            //if it is my position, I am going to send my position to the server
            //current player's position

            Vector3 myPosition=new Vector3(0f,0f);
            try { 
             myPosition = players[myclientid].avatar.transform.position;
            } catch (KeyNotFoundException e)
            {
                Debug.Log(clientid + e.Message);
            }
            string message = "MYPOSITION|" + myPosition.x.ToString() + '|' + myPosition.y.ToString();
            //send the current player's position on the UDP channel
            //Debug.Log(message);
            Send(message, unreliablechannel);
        }
    }

    
    private void SpawnPlayer(string playername, int connectionid)
    {
        GameObject p = Instantiate(playerPrefab) as GameObject;

        Player info = new Player();
        info.connectionId = connectionid;
        info.avatar = p;
        //set the nickname
        info.avatar.GetComponentInChildren<TextMesh>().text = playername;
        players.Add(connectionid, info);

        if (connectionid == myclientid)
        {
            //if he is my player, he can move using my keyboard
            p.AddComponent<playerScript>();
            isStarted = true;
        }

       
    }



    // Update is called once per frame
    void Update()
    {
        //the operations that are going to happen while the client is running
        if (!isConnected)
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

        

        switch (recData)
        {
            case NetworkEventType.DataEvent:
                //put any message from the server into the receivebuffer, starting from data offset 0.
                string messagefromserver = Encoding.Unicode.GetString(receiveBuffer, 0, dataSize);
                //check what messages were received from the server and write them to the console
                Debug.Log(messagefromserver);
                string[] splitdata = messagefromserver.Split('|');
                //based on the kind of message, do different things
                switch(splitdata[0])
                {
                    //ASKNAME - ask for nickname
                    //DC - disconnect
                    //CNN - changed nickname for the player
                    //ASKPOSITION - to change position for the player
                    //position is updated every frame

                    //in case the server is asking for a nickname
                    case "ASKNAME":
                        SendNickname(splitdata);
                        break;
                    case "CNN":
                        //change nickname, parameter 1: new nickname, parameter 2: connection id bound to 
                        //the new nickname
                        SpawnPlayer(splitdata[1], int.Parse(splitdata[2]));
                        break;
                    case "ASKPOSITION":
                        
                        SendPosition(splitdata);
                        break;
                    case "DC":
                        int clientid = int.Parse(splitdata[1]);
                        PlayerDisconnect(clientid);
                        break;
                    default:

                        break;
                    
                }
                break;
        }





    }
}
