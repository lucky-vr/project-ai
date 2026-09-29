using UnityEngine;
using System.Text;

/// <summary>World-space instruction UI showing the next action at one village venue.</summary>
public sealed class VillageVenueTaskGuide : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public string objectiveId;
    public string venueTitle;
    public string[] instructions;
    public bool[] pressSteps;
    public TextMesh promptText;

    int lastStep = -1;
    float hintUntil;
    string hintHeading;

    void OnEnable()
    {
        if (progressBoard != null)
            progressBoard.onProgressChanged.AddListener(Refresh);
        Refresh();
    }

    void OnDisable()
    {
        if (progressBoard != null)
            progressBoard.onProgressChanged.RemoveListener(Refresh);
    }

    public void Refresh()
    {
        if (promptText == null || progressBoard == null || instructions == null)
            return;
        var step = progressBoard.GetCompletedSteps(objectiveId);
        if (lastStep >= 0 && step > lastStep && step < instructions.Length)
        {
            hintHeading = "DONE  •  NEXT";
            hintUntil = Time.time + 2f;
        }
        else if (step < lastStep || step >= instructions.Length)
        {
            hintHeading = null;
            hintUntil = 0;
        }
        lastStep = step;
        WritePrompt();
    }

    /// <summary>Briefly points a player back to the active task after a rejected XR attempt.</summary>
    public void ShowRetryHint()
    {
        if (progressBoard == null || instructions == null ||
            progressBoard.GetCompletedSteps(objectiveId) >= instructions.Length)
            return;
        hintHeading = "TRY THIS NEXT";
        hintUntil = Time.time + 2.5f;
        WritePrompt();
    }

    void Update()
    {
        if (hintUntil <= 0 || Time.time < hintUntil) return;
        hintUntil = 0;
        hintHeading = null;
        WritePrompt();
    }

    void WritePrompt()
    {
        if (promptText == null || progressBoard == null || instructions == null) return;
        int step = progressBoard.GetCompletedSteps(objectiveId);
        if (hintHeading == null || step < 0 || step >= instructions.Length)
        {
            promptText.text = FormatPrompt(venueTitle, step, instructions, pressSteps);
            return;
        }
        promptText.text = hintHeading + "\n" + Wrap(instructions[step], 22) +
                          "\n" + ControlHint(step, pressSteps);
    }

    public static string FormatPrompt(string title, int step, string[] actions, bool[] pressSteps = null)
    {
        if (actions == null || actions.Length == 0) return title + "\nNO TASKS";
        if (step < 0)
            return title + "\nTASK DATA MISSING";
        if (step >= actions.Length)
            return title + " READY!\nExplore another place.\nWrist menu cues items.";
        return title + "  " + (step + 1) + "/" + actions.Length +
               "\n" + Wrap(actions[step], 22) +
               "\n" + ControlHint(step, pressSteps);
    }

    static string ControlHint(int step, bool[] pressSteps) =>
        pressSteps != null && step < pressSteps.Length && pressSteps[step]
            ? "POINT + TRIGGER"
            : "GRIP • RELEASE AT TARGET";

    static string Wrap(string message, int maxLine)
    {
        var result = new StringBuilder();
        int line = 0;
        foreach (var word in message.Split(' '))
        {
            if (line > 0 && line + word.Length + 1 > maxLine)
            {
                result.Append('\n');
                line = 0;
            }
            else if (line > 0)
            {
                result.Append(' ');
                line++;
            }
            result.Append(word);
            line += word.Length;
        }
        return result.ToString();
    }
}
