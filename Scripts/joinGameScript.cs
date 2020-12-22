using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class joinGameScript : MonoBehaviour {

    Button joinGameButton;

    clientScript clientConnectionScript;

    string nickname;
    //Use this for initialization
	void Start () {
        clientConnectionScript = GameObject.Find("ClientController").GetComponent<clientScript>();
        joinGameButton = GetComponent<Button>();
        joinGameButton.onClick.AddListener(() => joinGameButtonPressed());
	}


    void joinGameButtonPressed()
    {
        nickname = GameObject.Find("nicknameField").GetComponent<InputField>().text;
        //set the nickname to the value of the textbox
        clientConnectionScript.playerName = nickname;
        //call the connect method in the clientconnectionscript
        clientConnectionScript.Connect();

        GameObject.Find("Plane").GetComponent<MeshRenderer>().enabled = true;
        //GameObject.Find("Player").GetComponent<MeshRenderer>().enabled = true;
        //GameObject.Find("NameTag").GetComponent<MeshRenderer>().enabled = true;
        //GameObject.Find("NameTag").GetComponent<TextMesh>().text = nickname;


        //make the buttons disappear
        GameObject.Find("Canvas").SetActive(false);



    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
