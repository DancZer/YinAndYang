using UnityEngine;
using System.Collections;
using FishNet.Connection;
using FishNet.Object;
 
public class PlayerMovement : NetworkBehaviour {
 
    public float CamMinHeight = 2f;

    public float ZoomSpeed = 50.0f; //zoom speed
    public float MainSpeed = 100.0f; //regular speed
    public float ShiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
    public float MaxShift = 1000.0f; //Maximum speed when holdin gshift
    public float CamSens = 0.25f; //How sensitive it with mouse

    private Vector3 _lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
    private float _totalRun= 1.0f;

    private Camera playerCamera;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if(base.IsOwner)
        {
            playerCamera = Camera.main;
            playerCamera.transform.parent = transform;
            playerCamera.transform.localPosition = new Vector3(0, 0, 0);
        }
        else
        {
            enabled = false;
        }
    }
     
    void Update () {
        
        if (Input.GetKey(KeyCode.Mouse2) && MiscHelper.IsOnTheScreen(Input.mousePosition)) { 
            _lastMouse = Input.mousePosition - _lastMouse ;
            _lastMouse = new Vector3(-_lastMouse.y * CamSens, _lastMouse.x * CamSens, 0 );
            _lastMouse = new Vector3(transform.eulerAngles.x + _lastMouse.x , transform.eulerAngles.y + _lastMouse.y, 0);
            transform.eulerAngles = _lastMouse;
        }
        _lastMouse = Input.mousePosition;
        //Mouse  camera angle done.  

        //Keyboard commands
        Vector3 p = GetBaseInput();
        if (p.sqrMagnitude > 0){ // only move while a direction key is pressed
            if (Input.GetKey (KeyCode.LeftShift)){
                _totalRun += Time.deltaTime;
                p  = p * _totalRun * ShiftAdd;
                p.x = Mathf.Clamp(p.x, -MaxShift, MaxShift);
                p.y = Mathf.Clamp(p.y, -MaxShift, MaxShift);
                p.z = Mathf.Clamp(p.z, -MaxShift, MaxShift);
            } else {
                _totalRun = Mathf.Clamp(_totalRun * 0.5f, 1f, 1000f);
                p = p * MainSpeed;
            }
         
            p = p * Time.deltaTime;
            Vector3 newPosition = transform.position;

            transform.Translate(p);
            newPosition.x = transform.position.x;
            newPosition.z = transform.position.z;
            transform.position = newPosition;
        }

        if(Input.GetAxis("Mouse ScrollWheel") != 0) {
            transform.localPosition += (transform.rotation * Vector3.forward) * Input.GetAxis("Mouse ScrollWheel") * ZoomSpeed;
        }

        if(transform.position.y < CamMinHeight){
            var pos = transform.position;
            pos.y = CamMinHeight;
            transform.position = pos;
        }
    }
     
    private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey (KeyCode.W)){
            p_Velocity += new Vector3(0, 0 , 1);
        }
        if (Input.GetKey (KeyCode.S)){
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey (KeyCode.A)){
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey (KeyCode.D)){
            p_Velocity += new Vector3(1, 0, 0);
        }
        return p_Velocity;
    }
}