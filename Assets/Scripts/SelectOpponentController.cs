using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectOpponentController : MonoBehaviour
{
    private bool playAgainstAI = false;
    private bool playAgainstHuman = false;
    private bool playAsWhite = false;
    private bool playAsBlack = false;
    private int playLength = 0;
    private GameObject aiButton;
    private GameObject humanButton;
    private GameObject whiteButton;
    private GameObject blackButton;
    private GameObject oneMinuteButton;
    private GameObject threeMinuteButton;
    private GameObject fiveMinuteButton;
    private GameObject tenMinuteButton;
    private GameObject playButton;


    public void Awake()
    {
        aiButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("OpponentPanel").Find("AIButton").gameObject;
        humanButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("OpponentPanel").Find("HumanButton").gameObject;
        whiteButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("ChooseSidePanel").Find("WhiteButton").gameObject;
        blackButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("ChooseSidePanel").Find("BlackButton").gameObject;
        oneMinuteButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("ChooseTimePanel").Find("OneMinuteButton").gameObject;
        threeMinuteButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("ChooseTimePanel").Find("ThreeMinuteButton").gameObject;
        fiveMinuteButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("ChooseTimePanel").Find("FiveMinuteButton").gameObject;
        tenMinuteButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("ChooseTimePanel").Find("TenMinuteButton").gameObject;
        playButton = GameObject.FindGameObjectWithTag("GamePlayUIParent").transform.Find("PlayButton").gameObject;
    }

    public void SelectButton(GameObject buttonObject)
    {
        buttonObject.GetComponent<Button>().GetComponent<Image>().color = Color.black;
        buttonObject.GetComponentInChildren<Text>().color = Color.white;
    }

    public void UnSelectButton(GameObject buttonObject)
    {
        buttonObject.GetComponent<Button>().GetComponent<Image>().color = Color.white;
        buttonObject.GetComponentInChildren<Text>().color = Color.black;
    }

    public void CheckAndSetPlayButtonActivity()
    {
        bool shouldBeActive = (playAgainstAI ^ playAgainstHuman) && (playAsWhite ^ playAsBlack) && playLength > 0;

        if(shouldBeActive)
        {
            playButton.SetActive(true);
        }
        else
        {
            playButton.SetActive(false);
        }
    }

    public void SetPlayLengthButtons(bool enable)
    {
        if (enable)
        {
            oneMinuteButton.SetActive(true);
            threeMinuteButton.SetActive(true);
            fiveMinuteButton.SetActive(true);
            tenMinuteButton.SetActive(true);
            UnSelectButton(oneMinuteButton);
            UnSelectButton(threeMinuteButton);
            UnSelectButton(fiveMinuteButton);
            UnSelectButton(tenMinuteButton);
        }
        else
        {
            oneMinuteButton.SetActive(false);
            threeMinuteButton.SetActive(false);
            fiveMinuteButton.SetActive(false);
            tenMinuteButton.SetActive(false);
            UnSelectButton(oneMinuteButton);
            UnSelectButton(threeMinuteButton);
            UnSelectButton(fiveMinuteButton);
            UnSelectButton(tenMinuteButton);
        }
    }

    public void SetChooseSideButtons(bool enable)
    {
        if (enable)
        {
            whiteButton.SetActive(true);
            blackButton.SetActive(true);
            UnSelectButton(whiteButton);
            UnSelectButton(blackButton);
        }
        else
        {
            whiteButton.SetActive(false);
            blackButton.SetActive(false);
            UnSelectButton(whiteButton);
            UnSelectButton(blackButton);
        }
    }

    public void PlayAgainstAI()
    {
        playAgainstAI = !playAgainstAI;
        playAgainstHuman = false;
        playLength = 0;

        if (playAgainstAI)
        {
            SelectButton(aiButton);
            UnSelectButton(humanButton);

            SetChooseSideButtons(true);
            SetPlayLengthButtons(false);
        }
        else
        {
            UnSelectButton(aiButton);
            UnSelectButton(humanButton);

            SetChooseSideButtons(false);
            SetPlayLengthButtons(false);
        }

        CheckAndSetPlayButtonActivity();
    }

    public void PlayAgainstHuman()
    {
        playAgainstHuman = !playAgainstHuman;
        playAgainstAI = false;
        playAsBlack = false;
        playLength = 0;

        if (playAgainstHuman)
        {
            playAsWhite = true;
            UnSelectButton(aiButton);
            SelectButton(humanButton);

            SetChooseSideButtons(false);
            SetPlayLengthButtons(true);
        }
        else
        {
            playAsWhite = false;
            UnSelectButton(aiButton);
            UnSelectButton(humanButton);

            SetChooseSideButtons(false);
            SetPlayLengthButtons(false);
        }

        CheckAndSetPlayButtonActivity();
    }
    public void PlayAsWhite()
    {
        playAsWhite = !playAsWhite;
        playAsBlack = false;
        playLength = 0;

        if (playAsWhite)
        {
            SelectButton(whiteButton);
            UnSelectButton(blackButton);
        }
        else
        {
            UnSelectButton(whiteButton);
            UnSelectButton(blackButton);
        }

        if(playAsWhite && (playAgainstAI ^ playAgainstHuman))
        {
            SetPlayLengthButtons(true);
        }
        else
        {
            SetPlayLengthButtons(false);
        }

        CheckAndSetPlayButtonActivity();
    }
    public void PlayAsBlack()
    {
        playAsBlack = !playAsBlack;
        playAsWhite = false;
        playLength = 0;

        if (playAsBlack)
        {
            UnSelectButton(whiteButton);
            SelectButton(blackButton);
        }
        else
        {
            UnSelectButton(whiteButton);
            UnSelectButton(blackButton);
        }

        if (playAsBlack && (playAgainstAI ^ playAgainstHuman))
        {
            SetPlayLengthButtons(true);
        }
        else
        {
            SetPlayLengthButtons(false);
        }

        CheckAndSetPlayButtonActivity();
    }
    public void PlayOneMinute()
    {
        playLength = playLength == 1 ? 0 : 1;

        if(playLength == 1)
        {
            SelectButton(oneMinuteButton);
        }
        else
        {
            UnSelectButton(oneMinuteButton);
        }

        UnSelectButton(threeMinuteButton);
        UnSelectButton(fiveMinuteButton);
        UnSelectButton(tenMinuteButton);

        CheckAndSetPlayButtonActivity();
    }
    public void PlayThreeMinute()
    {
        playLength = playLength == 3 ? 0 : 3;

        if (playLength == 3)
        {
            SelectButton(threeMinuteButton);
        }
        else
        {
            UnSelectButton(threeMinuteButton);
        }

        UnSelectButton(fiveMinuteButton);
        UnSelectButton(tenMinuteButton); 
        UnSelectButton(oneMinuteButton);


        CheckAndSetPlayButtonActivity();
    }
    public void PlayFiveMinute()
    {
        playLength = playLength == 5 ? 0 : 5;

        if (playLength == 5)
        {
            SelectButton(fiveMinuteButton);
        }
        else
        {
            UnSelectButton(fiveMinuteButton);
        }

        UnSelectButton(threeMinuteButton);
        UnSelectButton(tenMinuteButton);
        UnSelectButton(oneMinuteButton);

        CheckAndSetPlayButtonActivity();
    }
    public void PlayTenMinute()
    {
        playLength = playLength == 10 ? 0 : 10;

        if (playLength == 10)
        {
            SelectButton(tenMinuteButton);
        }
        else
        {
            UnSelectButton(tenMinuteButton);
        }

        UnSelectButton(threeMinuteButton);
        UnSelectButton(fiveMinuteButton);
        UnSelectButton(oneMinuteButton);

        CheckAndSetPlayButtonActivity();
    }
    public void Play()
    {
        if (playAgainstHuman)
            playAsWhite = true;
        GameManager.instance.isOpponentHuman = playAgainstHuman;
        GameManager.instance.isWhiteSelected = playAsWhite;
        GameManager.instance.playTimeAmount = playLength * 60;
        Debug.Log("Opponent is: " + (playAgainstHuman ? "Human" : "AI"));
        Debug.Log("Play as: " + (playAsWhite ? "White" : "Black"));
        SceneManager.LoadScene("GamePlay");
    }

    public void GoBack()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
