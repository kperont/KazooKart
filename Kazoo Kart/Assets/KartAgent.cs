using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class KartAgent : Agent
{
    // Initialize Environment variables
    Rigidbody rb;
    public Transform Target;
    public GameObject spawn; //Ben

    // Initialize Agent speeds
    public float turnSpeed = 1f;
    public float moveSpeed = 1f;

    private Stack DEAD_FLAGS = new Stack();
    private int num_flags = 0;
    private int num_flags_collected = 0;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawn = GameObject.FindGameObjectWithTag("Spawn"); //Ben
    }

    // Actions at beginning of each episode
    public override void OnEpisodeBegin()
    {

        // Reset number of flags and number of collected flags
        num_flags = 0;
        num_flags_collected = 0;

        // Reset Agent Velocity and Position
        this.rb.angularVelocity = Vector3.zero;
        this.rb.velocity = Vector3.zero;
        this.transform.localPosition = spawn.transform.position; //Ben
        this.transform.localRotation = spawn.transform.rotation; //Ben

        // Reset Flag States
        while(DEAD_FLAGS.Count > 0){
            GameObject f = (GameObject)DEAD_FLAGS.Pop();
            f.SetActive(true);
        }

        // Reinitialize number of flags
        GameObject[] flags = GameObject.FindGameObjectsWithTag("Flag");
        if(flags.Length > 0){
            num_flags = flags.Length;
        }
        
    }

    // Collect observations
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent location
        sensor.AddObservation(transform.localPosition);

        // Agent velocity
        var localVelocity = transform.InverseTransformDirection(rb.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;

        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        dirToGo += transform.right * right;
        rotateDir = -transform.up * rotate;

        rb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        if (rb.velocity.sqrMagnitude > 25f) // slow it down
        {
            rb.velocity *= 0.95f;
        }

    }

    // Actions with each action received
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        if (this.transform.localPosition.y < 0)
        {
            SetReward(-1.0f);
            EndEpisode();
        }

        MoveAgent(actionBuffers);
    }

    void FixedUpdate(){
        AddReward(-0.001f);
    }

    void OnTriggerEnter(Collider other)
    {
        // Punish if Kazoo hits wall
        if(other.gameObject.CompareTag("Wall")){
            SetReward(-1.0f);
            EndEpisode();
        }
        // Reward Kazoo if it finishes
        else if(other.gameObject.CompareTag("FinishFlag")){
            SetReward(100.0f);
            EndEpisode();
        }
    }

    void OnTriggerExit(Collider other){
        // Disable Trigger on flag if Kazoo has passed through
        if(other.gameObject.CompareTag("Flag")){

            num_flags_collected++;
            AddReward(1.5f);

            DEAD_FLAGS.Push(other.gameObject);
            other.gameObject.SetActive(false);
            Debug.Log("BUTT PASSED THROUGH");
        }
    }

    void OnCollisionEnter(Collision collision){
        if(collision.gameObject.CompareTag("ForbiddenGround")){
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Wall")){
            SetReward(-1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = 0;
        continuousActionsOut[1] = 0;
        continuousActionsOut[2] = 0;
        if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[2] = -1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            continuousActionsOut[0] = -1;
        }
    }

}
