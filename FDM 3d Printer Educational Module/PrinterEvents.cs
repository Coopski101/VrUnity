using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.Extras;
using Valve.VR;
using Obi;
using Valve.VR.InteractionSystem;

/* event script to replace settinsmanagerscript - should be attached to onbuttondown on a hoverbutton
*/
public class PrinterEvents : MonoBehaviour
{
    GameObject emitter;
    GameObject pipeContainer;
    ObiEmitter fluid;
    ObiParticleRenderer particles;
    Dictionary<float, Color> colors = new Dictionary<float, Color>();
    public float index = 0f;

    public string currentButtonName;

    public AudioSource audioSource;
    public AudioSource audioMotorSource;
    public List<GeometryScript> geometries;
    public static GeometryScript currentGeom;
    bool buttonLock;

    void Start()
    {
        geometries = new List<GeometryScript>();
        geometries.Add(GameObject.Find("Obi Solver").transform.GetChild(0).gameObject.GetComponent<Circle>());//must add via script as initialization wipes the list out
        geometries.Add(GameObject.Find("Obi Solver").transform.GetChild(1).gameObject.GetComponent<DogboneTensile>());

        pipeContainer = GameObject.Find("Pipe");
        emitter = GameObject.Find("Emitter");
        fluid = emitter.GetComponent<ObiEmitter>();
        particles = emitter.GetComponent<ObiParticleRenderer>();
        colors.Add(1f, new Color(0.0f, 175.0f, 0.0f, 0.9f)); //Green
        colors.Add(0f, new Color(175.0f, 0.0f, 0.0f, 0.9f)); //Red
        colors.Add(2f, new Color(0.0f, 0.0f, 175.0f, 0.9f)); //Blue
        colors.Add(3f, new Color(0.0f, 175.0f, 175.0f, 0.9f)); //teal
        colors.Add(4f, new Color(255.0f, 0.0f, 150.0f, 0.9f)); //pink 255.0f, 0.0f, 150.0f, 0.9f
        colors.Add(5f, new Color(255.0f, 230.0f, 0.0f, 0.9f)); //yellow
        colors.Add(6f, new Color(0.0f, 0.0f, 0.0f, 0.9f)); //black
        colors.Add(7f, new Color(255.0f, 255.0f, 255.0f, 0.9f)); //white
        fluid.enabled = false;

        currentButtonName = gameObject.name ; //gives name the object it's currently attached to name

        particles.particleColor = colors[(Mathf.Abs(index)) % 8f];//no ugly start

        currentGeom = geometries[0];
        currentGeom.canPrint = true;
        buttonLock = false;

    }
    public void OnPress(Hand hand)
    {
        Debug.Log("Button pressed: "+currentButtonName);//debug

        audioSource.Play();//audio button sound

        if (currentButtonName == "IncreaseSpeed")
        {
            currentGeom.speed += 0.005f;
        }
        if (currentButtonName == "DecreaseSpeed")
        {
            currentGeom.speed -= 0.005f;
        }
        if (currentButtonName == "NextColor")
        {
            index += 1f;
            particles.particleColor = colors[(Mathf.Abs(index)) %8f];
            Debug.Log("ColorDebug: index--" + index.ToString()+"math--"+ ((Mathf.Abs(index)) % 10f).ToString()+"colorreturn--" +colors[(Mathf.Abs(index)) % 10f].ToString() + "color--"+particles.particleColor.ToString());//debug

        }
        if (currentButtonName == "PreviousColor")
        {
            index -= 1f;
            particles.particleColor = colors[(Mathf.Abs(index)) % 8f];
            Debug.Log("ColorDebug: index--" + index.ToString() + "math--" + ((Mathf.Abs(index)) % 10f).ToString() + "colorreturn--" + colors[(Mathf.Abs(index)) % 10f].ToString() + "color--" + particles.particleColor.ToString());//debug
        }
        if (currentButtonName == "StartCircle")//where circle is the shape of the button (dont ask me why)
        {
            index = 0f;
            currentGeom.enabled = true;
            if (currentGeom.canPrint)
            {
                fluid.speed = 1.5f;
                audioMotorSource.Play();
            }
        }
        if (currentButtonName == "PauseCircle")
        {
            fluid.speed = 0f;
            currentGeom.enabled = false;
            audioMotorSource.Pause();
        }
        if (currentButtonName == "QuitModule")
        {
            Debug.Log("Application quit");
            Application.Quit();
        }
        if (currentButtonName == "NextGeometryCircle")
        {
            if (currentGeom.canPrint == true)
                currentGeom = geometries[(geometries.IndexOf(currentGeom) + 1) % geometries.Count];
            else
                print("Must remove obj from bed before printing more");
        }

        if (currentGeom.name.Equals("Circle"))//invisible walls that trap particles
            pipeContainer.SetActive(true);
        else
            pipeContainer.SetActive(false);

    }




}
