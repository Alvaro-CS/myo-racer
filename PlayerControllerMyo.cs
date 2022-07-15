using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEditor;
using UnityEngine;
using LockingPolicy = Thalmic.Myo.LockingPolicy;
using Pose = Thalmic.Myo.Pose;
using UnlockType = Thalmic.Myo.UnlockType;
using VibrationType = Thalmic.Myo.VibrationType;

public class PlayerControllerMyo : MonoBehaviour
{
    [SerializeField]private float fSpeed = 5f;
    private float turnSpeed = 45f;
    [SerializeField] private float horizontalInput;
    [SerializeField] private float forwardInput = 1;
    private Rigidbody playerRb;
    [SerializeField]float elapsed = 0f;
    public List<WheelCollider> allWheelsColliders;
    public List<Transform> allWheelsTransforms;
    [SerializeField] int wheelsOnGround;
    private GameManager gameManager;
    private float acceleration=1;
    private float deceleration = 1;
    private bool accelerate;
    public GameObject brakeLights;
    // Myo game object to connect with.
    // This object must have a ThalmicMyo script attached.
    public GameObject myo = null;
    
    private void Start()
    {
        myo = GameObject.Find("Myo");
        playerRb = GetComponent<Rigidbody>();
        // playerRb.constraints = RigidbodyConstraints.FreezeRotationX; //car doesn't flip
        gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
        brakeLights.SetActive(false);
        accelerate = true;

    }

  

    // A rotation that compensates for the Myo armband's orientation parallel to the ground, i.e. yaw.
    // Once set, the direction the Myo armband is facing becomes "forward" within the program.
    // Set by making the fingers spread pose or pressing "r".
    private Quaternion _antiYaw = Quaternion.identity;

    // A reference angle representing how the armband is rotated about the wearer's arm, i.e. roll.
    // Set by making the fingers spread pose or pressing "r".
    private float _referenceRoll = 0.0f;

    // The pose from the last update. This is used to determine if the pose has changed
    // so that actions are only performed upon making them rather than every frame during
    // which they are active.
    private Pose _lastPose = Pose.Unknown;

    // Update is called once per frame.
    void Update()
    {
        if (gameManager.getMyoMode())
        {
            // Access the ThalmicMyo component attached to the Myo object.
            ThalmicMyo thalmicMyo = myo.GetComponent<ThalmicMyo>();

            // Update references when the pose becomes fingers spread or the q key is pressed.
            bool updateReference = false;
            if (thalmicMyo.pose != _lastPose)
            {
                _lastPose = thalmicMyo.pose;
                
                    if (thalmicMyo.pose == Pose.Fist && !accelerate)//if fist when decelerating, it increases the brake power
                    {
                        deceleration++;
                        thalmicMyo.Vibrate(VibrationType.Short);
                    }
                
                /*  if (thalmicMyo.pose == Pose.FingersSpread)
                  {
                      updateReference = true;

                      ExtendUnlockAndNotifyUserAction(thalmicMyo);
                  }*/ //THIS IS IF WE WANT TO RESET POSITION BY FINGER SPREADING
            }
            if (Input.GetKeyDown("r"))
            {
                updateReference = true;
            }

            // Update references. This anchors the joint on-screen such that it faces forward away
            // from the viewer when the Myo armband is oriented the way it is when these references are taken.
            if (updateReference)
            {
                // _antiYaw represents a rotation of the Myo armband about the Y axis (up) which aligns the forward
                // vector of the rotation with Z = 1 when the wearer's arm is pointing in the reference direction.
                _antiYaw = Quaternion.FromToRotation(
                    new Vector3(myo.transform.forward.x, 0, myo.transform.forward.z),
                    new Vector3(0, 0, 1)
                );

                // _referenceRoll represents how many degrees the Myo armband is rotated clockwise
                // about its forward axis (when looking down the wearer's arm towards their hand) from the reference zero
                // roll direction. This direction is calculated and explained below. When this reference is
                // taken, the joint will be rotated about its forward axis such that it faces upwards when
                // the roll value matches the reference.
                Vector3 referenceZeroRoll = computeZeroRollVector(myo.transform.forward);
                _referenceRoll = rollFromZero(referenceZeroRoll, myo.transform.forward, myo.transform.up);
            }

            // Current zero roll vector and roll value.
            Vector3 zeroRoll = computeZeroRollVector(myo.transform.forward);
            float roll = rollFromZero(zeroRoll, myo.transform.forward, myo.transform.up);

            // The relative roll is simply how much the current roll has changed relative to the reference roll.
            // adjustAngle simply keeps the resultant value within -180 to 180 degrees.
            float relativeRoll = normalizeAngle(roll - _referenceRoll);

            // antiRoll represents a rotation about the myo Armband's forward axis adjusting for reference roll.
            Quaternion antiRoll = Quaternion.AngleAxis(relativeRoll, myo.transform.forward);

            // Here the anti-roll and yaw rotations are applied to the myo Armband's forward direction to yield
            // the orientation of the joint.
            if (updateReference)
            {
                transform.rotation = _antiYaw * antiRoll * Quaternion.LookRotation(myo.transform.forward);
            }

            // The above calculations were done assuming the Myo armbands's +x direction, in its own coordinate system,
            // was facing toward the wearer's elbow. If the Myo armband is worn with its +x direction facing the other way,
            // the rotation needs to be updated to compensate.
            if (thalmicMyo.xDirection == Thalmic.Myo.XDirection.TowardWrist)
            {
                // Mirror the rotation around the XZ plane in Unity's coordinate system (XY plane in Myo's coordinate
                // system). This makes the rotation reflect the arm's orientation, rather than that of the Myo armband.
                if (updateReference)
                {
                    transform.rotation = new Quaternion(transform.localRotation.x,
                                                    -transform.localRotation.y,
                                                    transform.localRotation.z,
                                                    -transform.localRotation.w);
                }
            }

            if (gameManager.isGameActive)
            {
                gamePlay(thalmicMyo);//doing emg actions
            }
        }
         else  { 
            if (gameManager.isGameActive)
            {
                if (Input.GetKeyDown("space") && !accelerate)//if fist when decelerating, it increases the brake power
                {
                    deceleration++;
                }
                gameplayManual();
            }
        }
    }

    void gamePlay(ThalmicMyo thalmicMyo) {
        
        if (thalmicMyo.pose == Pose.Fist&&accelerate)//if fist when accelerating, it brakes
        {
            accelerate = false;
            brakeLights.SetActive(true);
            thalmicMyo.Vibrate(VibrationType.Short);
        }//increase brake power in update first lines

        if (fSpeed < 0)//brakes may cause negative speed, we fix it
        {
            fSpeed = 0;
        }
        if (thalmicMyo.pose == Pose.DoubleTap && !accelerate)//if double tap when decelerating, return to accelerate mode
        {
            accelerate = true;
            deceleration=1;
            brakeLights.SetActive(false);
            thalmicMyo.Vibrate(VibrationType.Short);
        }
        float speedUI = fSpeed * 1.5f;
        gameManager.showSpeedText(speedUI);
        if (fSpeed > 0) //only move when speed is positive, in order to not reverse
        {
            transform.Translate(Vector3.forward * forwardInput * fSpeed * Time.deltaTime); //translation instead of forces because forces with band results in crazy movements.

        }
        if (IsOnGround() && accelerate)//enable changing direction and speed increase
        {
            horizontalInput = myo.transform.forward.z; //sometimes needs a - before myo.... ¿?
            transform.Rotate(Vector3.up, horizontalInput * turnSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            if (elapsed >= 1f) //each x seconds, call function
            {
                elapsed = elapsed % 1f;
                increaseSpeed();
            }
        }
        else if (IsOnGround() && !accelerate) {//deceleration
            horizontalInput = myo.transform.forward.z; //sometimes needs a - before myo.... ¿?
            transform.Rotate(Vector3.up, horizontalInput * turnSpeed * Time.deltaTime);
            if (fSpeed > 0)//no generate negative speeds
            {
                elapsed += Time.deltaTime;
                if (elapsed >= 1f) //each x seconds, call function
                {
                    elapsed = elapsed % 1f;
                    decreaseSpeed();
                }
            }
        }
        else if(!IsOnGround())
        {//no acceleration in air
            elapsed += Time.deltaTime;
            if (elapsed >= 1f) //each x seconds, call function
            {
                elapsed = elapsed % 1f;
                decreaseSpeed();
            }
        }
        if (transform.position.y < gameManager.getDeathHeight())
        {
            thalmicMyo.Vibrate(VibrationType.Medium);
            gameManager.GameOver();
        }
    }
    void gameplayManual() {
        if (Input.GetKeyDown("space") && accelerate)//if "space" when accelerating, it brakes
        {
            accelerate = false;
            brakeLights.SetActive(true);
        }//increase brake power in update first lines

        if (fSpeed < 0)//brakes may cause negative speed, we fix it
        {
            fSpeed = 0;
        }
        if ((Input.GetKeyDown("left shift")|| Input.GetKeyDown("right shift")) && !accelerate)//if "shift" when decelerating, return to accelerate mode
        {
            accelerate = true;
            deceleration = 1;
            brakeLights.SetActive(false);
        }
        float speedUI = fSpeed * 1.5f;
        gameManager.showSpeedText(speedUI);
        if (fSpeed > 0) //only move when speed is positive, in order to not reverse
        {
            transform.Translate(Vector3.forward * forwardInput * fSpeed * Time.deltaTime); //translation instead of forces because forces with band results in crazy movements.

        }
        if (IsOnGround() && accelerate)//enable changing direction and speed increase
        {
            horizontalInput = Input.GetAxis("Horizontal"); 
            transform.Rotate(Vector3.up, horizontalInput * turnSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            if (elapsed >= 1f) //each x seconds, call function
            {
                elapsed = elapsed % 1f;
                increaseSpeed();
            }
        }
        else if (IsOnGround() && !accelerate)
        {//deceleration
            horizontalInput = Input.GetAxis("Horizontal");
            transform.Rotate(Vector3.up, horizontalInput * turnSpeed * Time.deltaTime);
            if (fSpeed > 0)//no generate negative speeds
            {
                elapsed += Time.deltaTime;
                if (elapsed >= 1f) //each x seconds, call function
                {
                    elapsed = elapsed % 1f;
                    decreaseSpeed();
                }
            }
        }
        else if (!IsOnGround())
        {//no acceleration in air
            elapsed += Time.deltaTime;
            if (elapsed >= 1f) //each x seconds, call function
            {
                elapsed = elapsed % 1f;
                decreaseSpeed();
            }
        }
        if (transform.position.y < gameManager.getDeathHeight())
        {
            gameManager.GameOver();
        }


    }
    void increaseSpeed() {
        fSpeed += acceleration;
    }
    void decreaseSpeed()
    {
        fSpeed -= deceleration;
    }

    bool IsOnGround()
    {
        wheelsOnGround = 0;
        foreach (WheelCollider wheel in allWheelsColliders)
        {

            if (wheel.isGrounded)
            {
                wheelsOnGround++;
            }
        }
        if (wheelsOnGround != 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
  
    // Compute the angle of rotation clockwise about the forward axis relative to the provided zero roll direction.
    // As the armband is rotated about the forward axis this value will change, regardless of which way the
    // forward vector of the Myo is pointing. The returned value will be between -180 and 180 degrees.

    float rollFromZero(Vector3 zeroRoll, Vector3 forward, Vector3 up)
    {
        // The cosine of the angle between the up vector and the zero roll vector. Since both are
        // orthogonal to the forward vector, this tells us how far the Myo has been turned around the
        // forward axis relative to the zero roll vector, but we need to determine separately whether the
        // Myo has been rolled clockwise or counterclockwise.
        float cosine = Vector3.Dot(up, zeroRoll);

        // To determine the sign of the roll, we take the cross product of the up vector and the zero
        // roll vector. This cross product will either be the same or opposite direction as the forward
        // vector depending on whether up is clockwise or counter-clockwise from zero roll.
        // Thus the sign of the dot product of forward and it yields the sign of our roll value.
        Vector3 cp = Vector3.Cross(up, zeroRoll);
        float directionCosine = Vector3.Dot(forward, cp);
        float sign = directionCosine < 0.0f ? 1.0f : -1.0f;

        // Return the angle of roll (in degrees) from the cosine and the sign.
        return sign * Mathf.Rad2Deg * Mathf.Acos(cosine);
    }

    // Compute a vector that points perpendicular to the forward direction,
    // minimizing angular distance from world up (positive Y axis).
    // This represents the direction of no rotation about its forward axis.
    Vector3 computeZeroRollVector(Vector3 forward)
    {
        Vector3 antigravity = Vector3.up;
        Vector3 m = Vector3.Cross(myo.transform.forward, antigravity);
        Vector3 roll = Vector3.Cross(m, myo.transform.forward);

        return roll.normalized;
    }

    // Adjust the provided angle to be within a -180 to 180.
    float normalizeAngle(float angle)
    {
        if (angle > 180.0f)
        {
            return angle - 360.0f;
        }
        if (angle < -180.0f)
        {
            return angle + 360.0f;
        }
        return angle;
    }

    // Extend the unlock if ThalmcHub's locking policy is standard, and notifies the given myo that a user action was
    // recognized.


    void ExtendUnlockAndNotifyUserAction(ThalmicMyo myo)
    {
        ThalmicHub hub = ThalmicHub.instance;

        if (hub.lockingPolicy == LockingPolicy.Standard)
        {
            myo.Unlock(UnlockType.Timed);
        }

        myo.NotifyUserAction();
    }
}
