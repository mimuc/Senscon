﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NBackTask : MonoBehaviour
{
    enum STATES { start, end, wait, move, baseline };
    public BlockDesigner blockDesigner;

    STATES state = STATES.start;


    public DataLoggerSimple loggerSimple;

    public GameObject feedbackCorrect;
    public GameObject feedbackWrong;
    private Coroutine disableFeedback;

    public GameObject feedbackStats;
    public int feedbackStatsPresentValue = 20;
    private int feedbackStatsCounter = 0;
    private Coroutine feedbackStatCoroutineDisable;

    public GameObject pilar;
    private GameObject sphere = null;

    public int counterBalls = 0;
    public bool isLastCorrect = false;

    public Material[] materials;
    private List<int> colorList = new List<int>();
    public int nBackNumber = 1;

    private int lastColor = 0;
    private int nBackColor = 0;

    //private double lastTimeStamp = 0;

    public bool showFeedback = true;

    //public DataLogger logger;

    // Start is called before the first frame update
    void Start() //initializationstep 
    {

  /*      if (logger == null)
        {
            Debug.LogError("Logger not set");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        } */

        if (pilar == null)
        {
            Debug.LogError("Pilar not set");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }

        if (materials.Length < 2)
        {
            Debug.LogError("Not enough colors");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }

        
        feedbackCorrect.SetActive(false);
        feedbackWrong.SetActive(false);
        feedbackStats.SetActive(false);

    }


    // Update is called once per frame
    void Update()
    {
        double timestamp = UnixTime.GetTime();

        if (Input.GetKeyDown("s") && STATES.start == state) //Start the task
        {

            state = STATES.wait;
            int nextBlock = blockDesigner.getNextBlock();
            Debug.Log("nextBlock: " + nextBlock);
            if (nextBlock == -1) {
                Debug.LogError("Wrong !");
            }
            else if (nextBlock == -2)
            {
                state = STATES.end;
                //loggerSimple.writeState(timestamp, "end", nextBlock, -1);
                loggerSimple.writeState(timestamp, "end", "");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }

            if (state == STATES.wait) {
                feedbackCorrect.SetActive(false);
                feedbackWrong.SetActive(false);
                counterBalls = 0;
                feedbackStatsCounter = 0;
                colorList.Clear();
                generateSpheres();
                blockDesigner.startRecoding();
            }

            loggerSimple.writeState(timestamp, "start", "");


        }
        else if (Input.GetKeyDown("left") && STATES.wait == state)
        {
            //print("left key was pressed");
            presentFeedback(timestamp, "red");
            state = STATES.wait;
            counterBalls++  ;


        }
        else if (Input.GetKeyDown("right") && STATES.wait == state)
        {
            //print("right key was pressed");
            presentFeedback(timestamp, "green");
            state = STATES.wait;
            counterBalls++;
        }

        if (blockDesigner.isDone && STATES.start != state)
        {
            //loggerSimple.writeState(timestamp, "end", -1, -1);
            loggerSimple.writeState(timestamp, "end", "");
            state = STATES.start;
            if (sphere != null)
            {
                Destroy(sphere);
            }
        }

    }

    public void collision(double timestamp, string pickedTrash)
    {
        presentFeedback(timestamp, pickedTrash);
        state = STATES.wait;
        counterBalls++;
        generateSpheres();
        //lastTimeStamp = timestamp;

        // Count the numbers of correct feedbacks for the stats feedback
        if (isLastCorrect == true)
        {
            feedbackStatsCounter++;
        }

        // Present stats after X balls.
        if (counterBalls % feedbackStatsPresentValue == 0)
        {

            float accuracy = (float)feedbackStatsCounter / (float)feedbackStatsPresentValue * 100.0f;
            TextMeshPro tmp = feedbackStats.GetComponent<TextMeshPro>() as TextMeshPro;
            tmp.SetText(Math.Round(accuracy) + "% Accuracy");
            feedbackStats.SetActive(true);
            feedbackStatsCounter = 0;

            if (feedbackStatCoroutineDisable != null)
            {
                StopCoroutine(feedbackStatCoroutineDisable);
            }

            feedbackStatCoroutineDisable = StartCoroutine(waitFeedbackStatsCoroutine());
        }
    }

    public void generateSpheres()
    {
        if (sphere != null)
        {
            Destroy(sphere);
        }

        //Debug.Log("Sound Played");
        this.GetComponent<AudioSource>().Play();

        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.AddComponent<CollisionDone>();
        sphere.AddComponent<Teleporter>();

        sphere.transform.position = pilar.transform.position + new Vector3(0, 1.357f, 0);
        sphere.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        SphereCollider sc = sphere.AddComponent(typeof(SphereCollider)) as SphereCollider;
        Rigidbody sphereRigidBody = sphere.AddComponent<Rigidbody>();
        sphereRigidBody.mass = 0.1f;
        sphereRigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        int randomColorId = UnityEngine.Random.Range(0, materials.Length);
        colorList.Add(randomColorId);
        sphere.GetComponent<Renderer>().material = materials[randomColorId];

        DestroyandPlay4s d = sphere.AddComponent<DestroyandPlay4s>() as DestroyandPlay4s;
        d.task = this;

        sphere.transform.parent = this.transform;
        sphere.name = "NbackBall";
        sphere.tag = "nbacktask";
    }

    public void presentFeedback(double timestamp, string pickedTrash)
    {
        if (colorList.Count - nBackNumber - 1 >= 0) //Here we start presenting the feedback after 2 initial trials
        {
            if (disableFeedback != null)
            {
                StopCoroutine(disableFeedback);
            }

            feedbackWrong.SetActive(false);  //remove wrong feedbackinfo
            feedbackCorrect.SetActive(false); //remove correct feedbackinfo

            lastColor = colorList[colorList.Count - 1];  //Last color of the ball defined as the last element of the color list
            nBackColor = colorList[colorList.Count - nBackNumber - 1];  //nbackColor defined as colorList element minus the NBack number (1) - 1, 


            if (lastColor == nBackColor && "green" == pickedTrash)   //If the color of the last ball presented matches the Nback Color & it's put in the green trash, feedback is correct
            {
                Debug.Log("correct");
                isLastCorrect = true;
                if (showFeedback) { 
                    feedbackCorrect.SetActive(true);
                }
            }

            else if (lastColor != nBackColor && "red" == pickedTrash)  //If the color of the last ball presented does not match the Nback Color & it's put in the red trash, feedback is correct
            {
                Debug.Log("correct");
                isLastCorrect = true;
                if (showFeedback)
                {
                    feedbackCorrect.SetActive(true);
                }
            }
            else  //Otherwise if the Last color matches the Nback color and it's put in the red trashcan OR Last color does not match the Nback color and it's put in the green trashcan: Feedback is wrong
            {
                Debug.Log("wrong");
                isLastCorrect = false;
                if (showFeedback)
                {
                    feedbackWrong.SetActive(true);
                }
            }

            loggerSimple.writeScore(timestamp, nBackColor, lastColor, pickedTrash, isLastCorrect, nBackNumber);
            disableFeedback = StartCoroutine(myWaitCoroutine());
        }
    }

    IEnumerator myWaitCoroutine()
    {
        yield return new WaitForSeconds(1f); // Wait for one second
        feedbackWrong.SetActive(false);
        feedbackCorrect.SetActive(false);
    }

    IEnumerator waitFeedbackStatsCoroutine()
    {
        yield return new WaitForSeconds(4f); // Wait for one second
        feedbackStats.SetActive(false);
    }
}




