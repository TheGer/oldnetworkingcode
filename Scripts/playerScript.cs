using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerScript : MonoBehaviour
{


    CharacterController controlSystem;

    float verticalspeed;


    // Use this for initialization
    void Start()
    {
        //the control system for the moving box 
        controlSystem = GetComponent<CharacterController>();

    }

    // Update is called once per frame
    void Update()
    {
        //zero out any keypresses so that my character controller is not oversensitive
        Vector3 inputs = Vector3.zero;

        //move left and right
        inputs.x = Input.GetAxis("Horizontal") * 15f;

        //JUMPING
        if (controlSystem.isGrounded)
        {
            verticalspeed = -1f;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalspeed = 10f;
            }
        }
        else
        {
            verticalspeed -= 14f * Time.deltaTime;
        }

        inputs.y = verticalspeed;

        controlSystem.Move(inputs * Time.deltaTime);

    }
}
