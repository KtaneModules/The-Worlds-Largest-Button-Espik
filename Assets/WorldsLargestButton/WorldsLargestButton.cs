using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class WorldsLargestButton : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable Button;

    public Material[] ColorMaterials;
    public Material[] StateMaterials;
    public Material[] AlertMaterials;

    public Renderer[] ButtonModel;
    public Renderer[] ButtonTextModel;
    public TextMesh[] ButtonText;

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved = false;

    // Solving info
    private readonly string[] COLORS = { "Blue", "Yellow", "Magenta", "Purple", "Cyan", "White", "Gray", "Brown" };
    private readonly string[] LABELS = { "Hold", "Press", "Tap", "Release", "Abort", "Detonate", "Run", "Large", "Button", "Big", "Listen", "Thick",
                                        "Literally\nBlank", "Explode", "Strike", "Solve", " ", "Blank", "Something", "World" };

    private readonly int[] primeNumbers = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59 };
    private readonly string[] firstHalfAlphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M" };

    private int serialDigitSum;
    private int stagesCompleted = 0;
    private int colorIndex = 0;
    private string buttonText = "World's\nLargest\nButton";

    private int sound = 0;
    private bool willStrike = false;
    private bool isUnicorn = false;
    private bool canHoldButton = true;
    private bool isEasySolution = false;

    private bool alertModeGood = true;
    private bool canCycleFlash = false;
    private bool wasAlertMode = false;

    private string newColor = "";
    private string newColor2 = "";
    private int newColorIndex = 0;
    private int newColor2Index = 0;
    private bool twoColorsFlash = false;
    private bool color2Flashing = false;

    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;

        // Delegation
        Button.OnInteract += delegate () { HoldButton(); return false; };
        Button.OnInteractEnded += delegate () { ReleaseButton(); };
    }

    // Sets up the initial button
    private void Start () {
	    serialDigitSum = Bomb.GetSerialNumberNumbers().Sum();
        GenerateColor(true);

        buttonText = LABELS[UnityEngine.Random.Range(0, LABELS.Length)];
        ButtonText[0].text = buttonText;
        ButtonText[1].text = buttonText;

        ButtonModel[1].enabled = false;
        ButtonTextModel[1].enabled = false;

        // Fixes the line break for logging
        if (buttonText == "Literally\nBlank")
            buttonText = "Literally Blank";

        Debug.LogFormat("[The World's Largest Button #{0}] The button is {1} and says \"{2}\".", moduleId, COLORS[colorIndex], buttonText);
        Debug.LogFormat("[The World's Largest Button #{0}] Rule {1} from Step 1 applies.", moduleId, LogFirstStageRules());

        sound = UnityEngine.Random.Range(0, 10);
    }

    
    // Setting materials
    private void SetMaterial(Material material) {
        ButtonModel[0].material = material;
        ButtonModel[1].material = material;
    }

    // Sets the button state
    private void SetButtonState(bool state) {
        if (state == true) {
            ButtonModel[0].enabled = false;
            ButtonModel[1].enabled = true;
            ButtonTextModel[0].enabled = false;
            ButtonTextModel[1].enabled = true;
        }

        else {
            ButtonModel[0].enabled = true;
            ButtonModel[1].enabled = false;
            ButtonTextModel[0].enabled = true;
            ButtonTextModel[1].enabled = false;
        }
    }


    // Holding the button
    private void HoldButton() {
        if (ButtonModel[0].enabled == false) return; // Cannot double hold

        Button.AddInteractionPunch(2.5f);
        PlayButtonSound();
        SetButtonState(true);

        if (moduleSolved == false) {
            if (stagesCompleted == 0)
                willStrike = !FirstStageRules();

            else if ((int)Bomb.GetTime() % 10 == sound)
                isEasySolution = true;

            else
                willStrike = !SecondStageRules();

            if (canHoldButton == false)
                willStrike = true;

            if (willStrike == true)
                Debug.LogFormat("[The World's Largest Button #{0}] The button was held at an invalid time. The module will strike upon releasing.", moduleId);

            if (isUnicorn == false)
                GenerateHoldButtonColor();
        }
    }

    // Releasing the button
    private void ReleaseButton() {
        if (ButtonModel[0].enabled == true) return; // Cannot double release

        Button.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, gameObject.transform);
        SetButtonState(false);

        if (moduleSolved == false) {
            if (willStrike == false) {
                // Alert mode
                if (wasAlertMode == true) {
                    willStrike = !alertModeGood;
                }

                else {
                    willStrike = !ReleaseRules(newColorIndex);

                    if (twoColorsFlash == true)
                        willStrike = !ReleaseRules(newColor2Index);
                }

                if (willStrike == true)
                    Debug.LogFormat("[The World's Largest Button #{0}] The button was released at an invalid time.", moduleId);
            }

            if (willStrike == true)
                StartCoroutine(Strike());

            else {
                stagesCompleted++;
                Debug.LogFormat("[The World's Largest Button #{0}] The button was held and released successfully.", moduleId);

                if (isUnicorn == true || isEasySolution == true || stagesCompleted > 3)
                    Solve();

                // Go to next stage
                else {
                    if (stagesCompleted == 1)
                        Debug.LogFormat("[The World's Largest Button #{0}] The sound the button made was \"{1}\".", moduleId, LogButtonSound());

                    if (wasAlertMode == true)
                        GenerateColor(false);

                    else
                        ChooseColor();

                    Debug.LogFormat("[The World's Largest Button #{0}] If you didn't hear the sound, rule {1} from Step 2 applies for right now.", moduleId, LogSecondStageRules());
                }
            }

            newColor2 = "";
            newColor2Index = 0;
            color2Flashing = false;
            alertModeGood = true;
            twoColorsFlash = false;
            isEasySolution = false;
            canCycleFlash = false;
            willStrike = false;
            wasAlertMode = false;
        }
    }


    // Plays the sound
    private void PlayButtonSound() {
        switch (sound) {
        case 1: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, gameObject.transform); break;
        case 2: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BinderDrop, gameObject.transform); break;
        case 3: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BriefcaseOpen, gameObject.transform); break;
        case 4: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform); break;
        case 5: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CapacitorPop, gameObject.transform); break;
        case 6: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.MenuDrop, gameObject.transform); break;
        case 7: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Stamp, gameObject.transform); break;
        case 8: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Switch, gameObject.transform); break;
        case 9: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, gameObject.transform); break;
        default: Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, gameObject.transform); break;
        }
    }


    // Generate new button color
    private void GenerateColor(bool firstTime) {
        colorIndex = UnityEngine.Random.Range(0, COLORS.Length);
        SetMaterial(ColorMaterials[colorIndex]);

        if (firstTime == false)
            Debug.LogFormat("[The World's Largest Button #{0}] The button's color is now {1}.", moduleId, COLORS[colorIndex]);
    }

    // Chooses the button color based on what was held
    private void ChooseColor() {
        if (color2Flashing == false)
            colorIndex = newColorIndex;

        else
            colorIndex = newColor2Index;

        Debug.LogFormat("[The World's Largest Button #{0}] The button's color remains as {1}.", moduleId, COLORS[colorIndex]);
    }

    // Generates the button color when held
    private void GenerateHoldButtonColor() {
        int random = UnityEngine.Random.Range(0, 10);

        // Single color
        if (random < 6) {
            newColorIndex = UnityEngine.Random.Range(0, COLORS.Length);
            newColor = COLORS[newColorIndex];
            SetMaterial(ColorMaterials[newColorIndex]);
            Debug.LogFormat("[The World's Largest Button #{0}] The held button is now {1}.", moduleId, COLORS[newColorIndex]);
        }

        // Two colors
        else if (random < 9) {
            newColorIndex = UnityEngine.Random.Range(0, COLORS.Length);
            newColor2Index = UnityEngine.Random.Range(0, COLORS.Length);
            newColor = COLORS[newColorIndex];
            newColor2 = COLORS[newColor2Index];
            SetMaterial(ColorMaterials[newColorIndex]);

            // If the two selected colors are the same
            if (newColor == newColor2)
                Debug.LogFormat("[The World's Largest Button #{0}] The held button is now {1}.", moduleId, COLORS[newColorIndex]);

            else {
                twoColorsFlash = true;
                canCycleFlash = true;
                Debug.LogFormat("[The World's Largest Button #{0}] The held button is now cycling between {1} and {2}.", moduleId, COLORS[newColorIndex], COLORS[newColor2Index]);
                StartCoroutine(CycleTwoColors());
            }
        }

        // Alert mode
        else {
            wasAlertMode = true;
            alertModeGood = true;
            canCycleFlash = true;
            newColorIndex = 8;
            Debug.LogFormat("[The World's Largest Button #{0}] The button is cycling between more than 2 colors. You have 10 seconds to release the button.", moduleId);
            StartCoroutine(CycleAlert());
            StartCoroutine(StartAlertCountdown());
        }
    }


    // Module strikes
    private IEnumerator Strike() {
        Debug.LogFormat("[The World's Largest Button #{0}] Strike!", moduleId);
        GetComponent<KMBombModule>().HandleStrike();
        canHoldButton = false;
        SetMaterial(StateMaterials[0]);

        yield return new WaitForSeconds(1.0f);

        GenerateColor(false);
        canHoldButton = true;

        if (stagesCompleted == 0)
            Debug.LogFormat("[The World's Largest Button #{0}] Rule {1} from Step 1 applies.", moduleId, LogFirstStageRules());

        else
            Debug.LogFormat("[The World's Largest Button #{0}] If you didn't hear the sound, rule {1} from Step 2 applies for right now.", moduleId, LogSecondStageRules());
    }

    // Module solves
    private void Solve() {
        Debug.LogFormat("[The World's Largest Button #{0}] Module solved!", moduleId);
        GetComponent<KMBombModule>().HandlePass();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
        moduleSolved = true;
        SetMaterial(StateMaterials[1]);
    }


    // Button cycles between two colors
    private IEnumerator CycleTwoColors() {
        if (canCycleFlash == true) {
            SetMaterial(ColorMaterials[newColorIndex]);
            color2Flashing = false;
            yield return new WaitForSeconds(0.3f);
        }

        if (canCycleFlash == true) {
            SetMaterial(ColorMaterials[newColor2Index]);
            color2Flashing = true;
            yield return new WaitForSeconds(0.3f);
        }

        if (canCycleFlash == true)
            StartCoroutine(CycleTwoColors());
    }

    // Button alert mode
    private IEnumerator CycleAlert() {
        if (canCycleFlash == true) {
            SetMaterial(AlertMaterials[0]);
            yield return new WaitForSeconds(0.1f);
        }

        if (canCycleFlash == true) {
            SetMaterial(AlertMaterials[1]);
            yield return new WaitForSeconds(0.1f);
        }

        if (canCycleFlash == true) {
            SetMaterial(AlertMaterials[2]);
            yield return new WaitForSeconds(0.1f);
        }

        if (canCycleFlash == true) {
            SetMaterial(AlertMaterials[3]);
            yield return new WaitForSeconds(0.1f);
        }

        if (canCycleFlash == true) {
            SetMaterial(AlertMaterials[4]);
            yield return new WaitForSeconds(0.1f);
        }

        if (canCycleFlash == true)
            StartCoroutine(CycleAlert());
    }

    // Sets a countdown for the alert
    private IEnumerator StartAlertCountdown() {
        alertModeGood = true;
        yield return new WaitForSeconds(10.0f);
        alertModeGood = false;
    }


    // Checks Step 1 rules
    private bool FirstStageRules() {
        if (COLORS[colorIndex] == "Blue" && buttonText == "Button") {
            isUnicorn = true;
            return true;
        }

        else if (COLORS[colorIndex] == "White")
            return true;

        else if (buttonText.Length > 6) {
            if ((int)Bomb.GetTime() % 60 == serialDigitSum)
                return true;

            return false;
        }

        else if (Bomb.GetBatteryCount() >= 3) {
            for (int i = 0; i < primeNumbers.Length; i++) {
                if ((int)Bomb.GetTime() % 60 == primeNumbers[i])
                    return true;
            }

            return false;
        }

        else if (buttonText == " ") {
            if ((int)Bomb.GetTime() % 60 == 0 || (int)Bomb.GetTime() % 60 == 11 || (int)Bomb.GetTime() % 60 == 22 ||
                (int)Bomb.GetTime() % 60 == 33 || (int)Bomb.GetTime() % 60 == 44 || (int)Bomb.GetTime() % 60 == 55)
                return true;

            return false;
        }

        else if (COLORS[colorIndex] == "Yellow" || COLORS[colorIndex] == "Cyan" || COLORS[colorIndex] == "Magenta")
            return true;

        else if (buttonText == "Strike") {
            if ((int)Bomb.GetTime() % 10 == Bomb.GetStrikes() % 10)
                return true;

            return false;
        }

        else if (buttonText == "Solve") {
            if ((int)Bomb.GetTime() % 60 == Bomb.GetSolvedModuleNames().Count() % 60)
                return true;

            return false;
        }

        for (int i = 0; i < firstHalfAlphabet.Length; i++) {
            if (buttonText.Substring(0, 1) == firstHalfAlphabet[i]) {
                if ((int)Bomb.GetTime() % 2 == 0)
                    return true;

                return false;
            }
        }

        return true;
    }

    // Checks release rules
    private bool ReleaseRules(int index) {
        /* 0 = Blue
         * 1 = Yellow
         * 2 = Magenta
         * 3 = Purple
         * 4 = Cyan
         * 5 = White
         * 6 = Gray
         * 7 = Brown
         * 8 = Alert
         */

        if (COLORS[colorIndex] == newColor && twoColorsFlash == false)
            return true;

        switch (index) {
        case 1: if (Bomb.GetFormattedTime().Count(x => x == '5') > 0) return true; break;
        case 2: if (Bomb.GetFormattedTime().Count(x => x == '2') > 0) return true; break;
        case 3: if ((int)Bomb.GetTime() % 10 == 6) return true; break;
        case 4: if ((int)Bomb.GetTime() % 10 + (int)Bomb.GetTime() % 60 / 10 == 7) return true; break;
        case 5: if ((int)Bomb.GetTime() % 2 == 0 || (int)Bomb.GetTime() % 60 / 10 % 2 == 0) return true; break;
        case 6: if ((int)Bomb.GetTime() % 2 == 1 || (int)Bomb.GetTime() % 60 / 10 % 2 == 1) return true; break;
        case 7: if ((int)Bomb.GetTime() % 120 < 60) return true; break;
        case 8: return alertModeGood;
        default: if (Bomb.GetFormattedTime().Count(x => x == '4') > 0) return true; break;
        }

        return false;
    }

    // Checks Step 2 Rules
    private bool SecondStageRules() {
        if (COLORS[colorIndex] == "Cyan") {
            if ((int)Bomb.GetTime() % 7 == 0)
                return true;

            return false;
        }

        else if (serialDigitSum > 20) {
            if ((int)Bomb.GetTime() % 60 == serialDigitSum)
                return true;

            return false;
        }

        else if (Bomb.GetBatteryCount() == 0) {
            if ((int)Bomb.GetTime() % 10 == Bomb.GetSerialNumberNumbers().Last())
                return true;

            return false;
        }

        else if ((int)Bomb.GetTime() / 60 == Bomb.GetSolvedModuleNames().Count()) {
            if ((int)Bomb.GetTime() % 60 < 15)
                return true;

            return false;
        }

        else if (Bomb.GetStrikes() == 1) {
            if ((int)Bomb.GetTime() % 10 == 2)
                return true;

            return false;
        }

        else if (Bomb.GetBatteryHolderCount() + Bomb.GetIndicators().Count() + Bomb.GetPortPlateCount() > 5) {
            if ((int)Bomb.GetTime() % 10 == 1 || (int)Bomb.GetTime() % 10 == 4 || (int)Bomb.GetTime() % 10 == 6 ||
                (int)Bomb.GetTime() % 10 == 8 || (int)Bomb.GetTime() % 10 == 9)
                return true;

            return false;
        }

        else if (Bomb.GetSolvableModuleNames().Count(x => x.Contains("The World's Largest Button")) == Bomb.GetModuleNames().Count())
            return true;

        else if (Bomb.IsIndicatorOff("SND")) {
            if ((int)Bomb.GetTime() % 10 == 0)
                return true;

            return false;
        }

        else if (COLORS[colorIndex] == "White" || COLORS[colorIndex] == "Gray" || COLORS[colorIndex] == "Brown") {
            if ((int)Bomb.GetTime() % 10 == 8)
                return true;

            return false;
        }

        else if (serialDigitSum == 0)
            return true;

        else {
            if ((int)Bomb.GetTime() % serialDigitSum == 0)
                return true;

            return false;
        }
    }


    // Logs the button sound
    private string LogButtonSound() {
        switch (sound) {
        case 1: return "Big Button Release";
        case 2: return "Binder Drop";
        case 3: return "Briefcase Open";
        case 4: return "Button Press";
        case 5: return "Capacitor Pop";
        case 6: return "Menu Drop";
        case 7: return "Stamp";
        case 8: return "Switch";
        case 9: return "Wire Snip";
        default: return "Big Button Press";
        }
    }

    // Logs the first step rules
    private int LogFirstStageRules() {
        if (COLORS[colorIndex] == "Blue" && buttonText == "Button")
            return 1;

        else if (COLORS[colorIndex] == "White")
            return 2;

        else if (buttonText.Length > 6)
            return 3;

        else if (Bomb.GetBatteryCount() >= 3)
            return 4;

        else if (buttonText == " ")
            return 5;

        else if (COLORS[colorIndex] == "Yellow" || COLORS[colorIndex] == "Cyan" || COLORS[colorIndex] == "Magenta")
            return 6;

        else if (buttonText == "Strike")
            return 7;

        else if (buttonText == "Solve")
            return 8;

        for (int i = 0; i < firstHalfAlphabet.Length; i++) {
            if (buttonText.Substring(0, 1) == firstHalfAlphabet[i])
                return 9;
        }

        return 10;
    }

    // Logs the second step rules
    private int LogSecondStageRules() {
        if (COLORS[colorIndex] == "Cyan")
            return 1;

        else if (serialDigitSum > 20)
            return 2;

        else if (Bomb.GetBatteryCount() == 0)
            return 3;

        else if ((int)Bomb.GetTime() / 60 == Bomb.GetSolvedModuleNames().Count())
            return 4;

        else if (Bomb.GetStrikes() == 1)
            return 5;

        else if (Bomb.GetBatteryHolderCount() + Bomb.GetIndicators().Count() + Bomb.GetPortPlateCount() > 5)
            return 6;

        else if (Bomb.GetSolvableModuleNames().Count(x => x.Contains("The World's Largest Button")) == Bomb.GetModuleNames().Count())
            return 7;

        else if (Bomb.IsIndicatorOff("SND"))
            return 8;

        else if ((COLORS[colorIndex] == "White" || COLORS[colorIndex] == "Gray" || COLORS[colorIndex] == "Brown"))
            return 9;

        else
            return 10;
    }



    // Twitch Plays support - made by eXish


    private bool timeIsShort(string yes)
    {
        if(yes.Length != 1)
        {
            return false;
        }
        int temp = 0;
        bool check = int.TryParse(yes, out temp);
        if (check)
        {
            if (temp > -1 && temp < 10)
            {
                return true;
            }
        }
        return false;
    }

    private bool timeIsNotAsShort(string yes)
    {
        if (yes.Length != 2)
        {
            return false;
        }
        int temp = 0;
        bool check = int.TryParse(yes, out temp);
        if (check)
        {
            if (temp > -1 && temp < 60)
            {
                return true;
            }
        }
        return false;
    }

    private bool timeIsLonger(string yes)
    {
        if (yes.Length != 4)
        {
            return false;
        }
        if (Regex.IsMatch(yes, @"^\s*[0-9]:[0-5][0-9]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return true;
        }
        return false;
    }

    private bool timeIsLongest(string yes)
    {
        if (yes.Length != 5)
        {
            return false;
        }
        if (Regex.IsMatch(yes, @"^\s*[0-5][0-9]:[0-5][0-9]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return true;
        }
        return false;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} hold/release XX:XX/X:XX/XX/X [Holds or releases the button at the specified time (XX:XX/#X:XX/##:XX/##:#X)]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*hold\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (ButtonModel[0].enabled == false)
            {
                yield return "sendtochaterror The button is already being held!";
                yield break;
            }   
            if (parameters.Length == 1)
            {
                Button.OnInteract();
            }
            else if(parameters.Length == 2)
            {
                if (timeIsShort(parameters[1]))
                {
                    int temp = 0;
                    int.TryParse(parameters[1], out temp);
                    yield return "sendtochat Holding the button at \"##:#"+parameters[1]+"\"!";
                    while (((int)Bomb.GetTime() % 10) != temp)
                    {
                        yield return "trycancel Waiting to hold the button has been halted due to a request to cancel!";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (timeIsNotAsShort(parameters[1]))
                {
                    int temp = 0;
                    int.TryParse(parameters[1], out temp);
                    yield return "sendtochat Holding the button at \"##:"+parameters[1]+"\"!";
                    while (((int)Bomb.GetTime() % 60) != temp)
                    {
                        yield return "trycancel Waiting to hold the button has been halted due to a request to cancel!";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (timeIsLonger(parameters[1]))
                {
                    int temp = 0;
                    int temp2 = 0;
                    int.TryParse(parameters[1].Substring(0, 1), out temp);
                    int.TryParse(parameters[1].Substring(2), out temp2);
                    yield return "sendtochat Holding the button at \"#"+parameters[1]+"\"!";
                    while (((int)Bomb.GetTime() % 600) != (temp2+(temp*60)))
                    {
                        yield return "trycancel Waiting to hold the button has been halted due to a request to cancel!";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (timeIsLongest(parameters[1]))
                {
                    int temp = 0;
                    int temp2 = 0;
                    int.TryParse(parameters[1].Substring(0, 2), out temp);
                    int.TryParse(parameters[1].Substring(3), out temp2);
                    yield return "sendtochat Holding the button at \""+parameters[1]+"\"!";
                    while (((int)Bomb.GetTime() % 3600) != (temp2 + (temp * 60)))
                    {
                        yield return "trycancel Waiting to hold the button has been halted due to a request to cancel!";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else
                {
                    yield return "sendtochaterror The specified time to hold the button at \"" + parameters[1] + "\" is invalid!";
                    yield break;
                }
                Button.OnInteract();
            }
            else
            {
                yield return "sendtochaterror Too many parameters!";
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*release\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (ButtonModel[0].enabled == true)
            {
                yield return "sendtochaterror The button is not being held!";
                yield break;
            }
            if (parameters.Length == 1)
            {
                Button.OnInteractEnded();
            }
            else if (parameters.Length == 2)
            {
                if (timeIsShort(parameters[1]))
                {
                    int temp = 0;
                    int.TryParse(parameters[1], out temp);
                    yield return "sendtochat Releasing the button at \"##:#" + parameters[1] + "\"!";
                    while (((int)Bomb.GetTime() % 10) != temp)
                    {
                        yield return "trycancel Waiting to release the button has been halted due to a request to cancel!";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (timeIsNotAsShort(parameters[1]))
                {
                    int temp = 0;
                    int.TryParse(parameters[1], out temp);
                    yield return "sendtochat Releasing the button at \"##:" + parameters[1] + "\"!";
                    while (((int)Bomb.GetTime() % 60) != temp)
                    {
                        yield return "trycancel Waiting to release the button has been halted due to a request to cancel!";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (timeIsLonger(parameters[1]))
                {
                    int temp = 0;
                    int temp2 = 0;
                    int.TryParse(parameters[1].Substring(0, 1), out temp);
                    int.TryParse(parameters[1].Substring(2), out temp2);
                    yield return "sendtochat Releasing the button at \"#" + parameters[1] + "\"!";
                    while (((int)Bomb.GetTime() % 600) != (temp2 + (temp * 60)))
                    {
                        yield return "trycancel Waiting to release the button has been halted due to a request to cancel!";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (timeIsLongest(parameters[1]))
                {
                    int temp = 0;
                    int temp2 = 0;
                    int.TryParse(parameters[1].Substring(0, 2), out temp);
                    int.TryParse(parameters[1].Substring(3), out temp2);
                    yield return "sendtochat Releasing the button at \"" + parameters[1] + "\"!";
                    while (((int)Bomb.GetTime() % 3600) != (temp2 + (temp * 60)))
                    {
                        yield return "trycancel Waiting to release the button has been halted due to a request to cancel!";
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else
                {
                    yield return "sendtochaterror The specified time to release the button at \""+parameters[1]+"\" is invalid!";
                    yield break;
                }
                Button.OnInteractEnded();
            }
            else
            {
                yield return "sendtochaterror Too many parameters!";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        // If the button is on stage 1
        if(stagesCompleted == 0)
        {
            // Wait for proper holding time
            int rule = LogFirstStageRules();
            if (rule == 3)
            {
                while (((int)Bomb.GetTime() % 60) != serialDigitSum)
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (rule == 4)
            {
                while (!primeNumbers.Contains(((int)Bomb.GetTime() % 60)))
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (rule == 5)
            {
                while (((int)Bomb.GetTime() % 60) != 55 && ((int)Bomb.GetTime() % 60) != 44 && ((int)Bomb.GetTime() % 60) != 33 && ((int)Bomb.GetTime() % 60) != 22 && ((int)Bomb.GetTime() % 60) != 11 && ((int)Bomb.GetTime() % 60) != 0)
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (rule == 7)
            {
                while (((int)Bomb.GetTime() % 10) != (Bomb.GetStrikes() % 10))
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (rule == 8)
            {
                while (((int)Bomb.GetTime() % 60) != (Bomb.GetSolvedModuleNames().Count() % 60))
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (rule == 9)
            {
                while (((int)Bomb.GetTime() % 10) != 0 && ((int)Bomb.GetTime() % 10) != 2 && ((int)Bomb.GetTime() % 10) != 4 && ((int)Bomb.GetTime() % 10) != 6 && ((int)Bomb.GetTime() % 10) != 8)
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            Button.OnInteract();
            yield return new WaitForSeconds(0.1f);
            // Wait for proper release time
            if (twoColorsFlash)
            {
                while (!ReleaseRules(newColorIndex) || !ReleaseRules(newColor2Index))
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                while (!ReleaseRules(newColorIndex))
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            Button.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
            // If unicorn don't continue
            if (rule == 1)
                yield break;
        }
        // Choose whether stage 2 and beyond should be the easy or hard way (50/50)
        int rando = 0; // set to alawys Easy Way - change to UnityEngine.Random.Range(0, 2) to get either
        if (rando == 0)
        {
            // Easy Way (Sound)
            // Wait for proper holding time
            while (((int)Bomb.GetTime() % 10) != sound)
            {
                yield return true;
                yield return new WaitForSeconds(0.1f);
            }
            Button.OnInteract();
            yield return new WaitForSeconds(0.1f);
            // Wait for proper release time
            if (twoColorsFlash)
            {
                while (!ReleaseRules(newColorIndex) || !ReleaseRules(newColor2Index))
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                while (!ReleaseRules(newColorIndex))
                {
                    yield return true;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            Button.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            // Hard Way (More Timed Presses)
            while (!moduleSolved)
            {
                // Wait for proper holding time
                int rule = LogSecondStageRules();
                if (rule == 1)
                {
                    while ((int)Bomb.GetTime() % 7 != 0)
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (rule == 2)
                {
                    while (((int)Bomb.GetTime() % 60) != serialDigitSum)
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (rule == 3)
                {
                    while (((int)Bomb.GetTime() % 10) != Bomb.GetSerialNumberNumbers().Last())
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (rule == 4)
                {
                    while (((int)Bomb.GetTime() % 60) > 14)
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (rule == 5)
                {
                    while (((int)Bomb.GetTime() % 10) != 2)
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (rule == 6)
                {
                    while (((int)Bomb.GetTime() % 10) != 1 && ((int)Bomb.GetTime() % 10) != 4 && ((int)Bomb.GetTime() % 10) != 6 && ((int)Bomb.GetTime() % 10) != 8 && ((int)Bomb.GetTime() % 10) != 9)
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (rule == 8)
                {
                    while (((int)Bomb.GetTime() % 10) != 0)
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if (rule == 9)
                {
                    while (((int)Bomb.GetTime() % 10) != 8)
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else if ((rule == 10) && (serialDigitSum != 0))
                {
                    while (((int)Bomb.GetTime() % serialDigitSum) != 0)
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                Button.OnInteract();
                yield return new WaitForSeconds(0.1f);
                // Wait for proper release time
                if (twoColorsFlash)
                {
                    while (!ReleaseRules(newColorIndex) || !ReleaseRules(newColor2Index))
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else
                {
                    while (!ReleaseRules(newColorIndex))
                    {
                        yield return true;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                Button.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}