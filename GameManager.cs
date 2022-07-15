using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Thalmic.Myo;
using System;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI gameOverText;
    public bool isGameActive;
    public Button restartButton;
    public Button menuButton;
    public GameObject titleScreen; 
    public Camera carSky, overSky;
    public GameObject carMyoPrefab;
    private Boolean isCarCreated = false;
    private readonly int countdownSeconds=3;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI speedometerText;
    private int deathHeight;

    public Button modeButton;
    public TextMeshProUGUI modeText;
    public bool myoMode = true;

    public AudioSource countdownAudio;
    //TIMER
    public int tiempoinicial;
    [Tooltip("Escala del Tiempo del Reloj")]
    [Range(-10.0f, 10.0f)]
    public float escalaDeTiempo = 1;

    public TextMeshProUGUI clockText;
    private float TiempoFrameConTiempoScale = 0f;
    private float showTimeInSeconds = 0F;
    private float escalaDeTiempoPausar, escalaDeTiempoInicial;
    private bool isClockPaused = true;
    private GameObject instantiatedCar;
    private int gameLevel;//for knowing which to load in restartlevel
    //TIMER
    public bool getMyoMode() {
        return myoMode;
    }
    public Boolean getIsCarCreated() {
        return isCarCreated;
    }
    public GameObject getInstantiatedCar()
    {
        return instantiatedCar;
    }
    public int getDeathHeight() {
        return deathHeight;
    }
    void Start()
    {
        //Escala de Tiempo Original para el timer
        escalaDeTiempoInicial = escalaDeTiempo;
        showTimeInSeconds = tiempoinicial;
        clockText.gameObject.SetActive(false);
        updateClock(tiempoinicial);
        //first camera stablish
        carSky.gameObject.SetActive(false);
        overSky.gameObject.SetActive(true);

    }
    public void StartGame(int level)
    {
        gameLevel = level;
        //camera switch and audio listener switch

        overSky.gameObject.SetActive(false);
        carSky.gameObject.SetActive(true);
        overSky.GetComponent<AudioListener>().enabled = false;
        carSky.GetComponent<AudioListener>().enabled = true;


        titleScreen.gameObject.SetActive(false);
        if (level == 0)
        {
            instantiatedCar = Instantiate(carMyoPrefab, new UnityEngine.Vector3(-370, 1417, 40), transform.rotation * UnityEngine.Quaternion.Euler(0, 0f, 0f));//,UnityEngine.Quaternion.identity
            isCarCreated = true;
            deathHeight = 1407;

            StartCoroutine(Countdown(countdownSeconds));
        }
        if (level == 1) {
             instantiatedCar=Instantiate(carMyoPrefab, new UnityEngine.Vector3(11, 5, 32), transform.rotation * UnityEngine.Quaternion.Euler(0, -12f, 0f));//,UnityEngine.Quaternion.identity
            isCarCreated = true;
            deathHeight = -5;
             StartCoroutine(Countdown(countdownSeconds));
        }
        if (level == 2)
        {
            instantiatedCar = Instantiate(carMyoPrefab, new UnityEngine.Vector3(-1157, 720, 167), transform.rotation * UnityEngine.Quaternion.Euler(0, -50, 0f));//,UnityEngine.Quaternion.identity
            isCarCreated = true;
            deathHeight = 710;
            StartCoroutine(Countdown(countdownSeconds));
        }

    }
    IEnumerator Countdown(int seconds)
    {
        countdownText.SetText("");//delete "GO" that was written previously
        countdownText.gameObject.SetActive(true);
        int count = seconds;
        yield return new WaitForSeconds(1);
        countdownText.SetText("Good luck!");
        yield return new WaitForSeconds(2);

        countdownAudio.Play();
        while (count > 0)
        {
            countdownText.SetText(count.ToString());
            yield return new WaitForSeconds(1);
            count--;
        }

        // count down is finished... 
        countdownText.SetText("Go!");
        clockText.SetText("");////delete previous time that was written previously
        clockText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1);
        countdownText.gameObject.SetActive(false);

        isGameActive = true;//game starts, player moves
        isClockPaused = false;//timer starts
        speedometerText.gameObject.SetActive(true);

    }

    // Update for timer
    void Update()
    {
        if (!isClockPaused)
        {
            TiempoFrameConTiempoScale = Time.deltaTime * escalaDeTiempo;
            showTimeInSeconds += TiempoFrameConTiempoScale;
            updateClock(showTimeInSeconds);
        }
        if (myoMode)
        {
            modeText.color = new Color32(221, 26, 46, 255);
            modeText.SetText("Myo Armband mode");
        }
        else
        {
            modeText.color = new Color32(171, 31, 87, 255);
            modeText.SetText("Keyboard mode");
        }
    }
    public void updateClock(float timeInSeconds)
    {
        int minutes;
        int seconds;
        string clockText;

        if (timeInSeconds < 0) timeInSeconds = 0;

        minutes = (int)timeInSeconds / 60;
        seconds = (int)timeInSeconds % 60;

        clockText = minutes.ToString("00") + ":" + seconds.ToString("00");
        this.clockText.text = clockText;
    }

    public void GameOver() {
        gameOverText.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true);
        menuButton.gameObject.SetActive(true);
        isClockPaused = true;
        isGameActive = false;
        
    }
    public void Win()
    {
        //WinText.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true);
        menuButton.gameObject.SetActive(true);
        clockText.gameObject.SetActive(false);
        speedometerText.gameObject.SetActive(false);
        winText.gameObject.SetActive(true);
        winText.SetText("Congratulations for beating level " + gameLevel+"!!!!\nFinal time: "+ clockText.text);

        isClockPaused = true;
        isGameActive = false;
    }
    public void BackToMenu() {//error if dontdeleteonload
        clockText.gameObject.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void RestartLevel()
    {
        clockText.gameObject.SetActive(false);
        showTimeInSeconds = 0f;
        isClockPaused = true;//for resetting clock
        gameOverText.gameObject.SetActive(false);
        winText.gameObject.SetActive(false);
        speedometerText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
        menuButton.gameObject.SetActive(false);
        Destroy(instantiatedCar);//destroy old car
        isCarCreated = false;//for camera re-adjustment
        StartGame(gameLevel);
    }
    public void showSpeedText(float speedUI) {
        speedometerText.SetText("Speed: " + speedUI + "km/h");

    }
    public void changeMode() {
        myoMode = !myoMode;
    }

}
