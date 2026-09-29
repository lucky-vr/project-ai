using UnityEngine;

/// <summary>
/// A venue-specific, visible result of the ten player interactions. All dependencies are
/// serialized scene references, so rebuilding the village does not rely on Editor listeners.
/// </summary>
public sealed class VillageVenueExperience : MonoBehaviour
{
    public enum Activity
    {
        Market, Cafe, Workshop, Greenhouse, Pool,
        Bakery, Post, Clinic, Library, Music,
        Garage, Observatory, Harbor, Art, Recycling
    }

    public VillageProgressBoard progressBoard;
    public string objectiveId;
    public Activity activity;
    public Transform focus;
    public Transform secondary;
    public Renderer[] milestones;
    public GameObject[] revealPieces;
    public TextMesh outcomeText;
    public AudioSource musicSource;

    Vector3 focusPosition, focusScale, secondaryPosition, secondaryScale;
    Quaternion focusRotation, secondaryRotation;
    MaterialPropertyBlock block;
    int lastStep = -1;
    float clock;
    bool remoteCue;

    public int LastDisplayedStep => lastStep;
    public bool RemoteCueActive => remoteCue;

    /// <summary>The wrist menu operates this venue's distinct visible festival mechanism.</summary>
    public void ToggleRemoteCue()
    {
        remoteCue = !remoteCue;
        Refresh();
        if (activity == Activity.Music && remoteCue)
            PlayNote(Mathf.Max(1, lastStep + 1));
    }

    public void ResetRemoteCue()
    {
        if (!remoteCue) return;
        remoteCue = false;
        Refresh();
    }

    void Awake()
    {
        if (focus != null)
        {
            focusPosition = focus.localPosition;
            focusScale = focus.localScale;
            focusRotation = focus.localRotation;
        }
        if (secondary != null)
        {
            secondaryPosition = secondary.localPosition;
            secondaryScale = secondary.localScale;
            secondaryRotation = secondary.localRotation;
        }
        block = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        if (progressBoard != null && progressBoard.onProgressChanged != null)
            progressBoard.onProgressChanged.AddListener(Refresh);
        Refresh();
    }

    void OnDisable()
    {
        if (progressBoard != null && progressBoard.onProgressChanged != null)
            progressBoard.onProgressChanged.RemoveListener(Refresh);
    }

    public void Refresh()
    {
        if (progressBoard == null) return;
        int step = Mathf.Clamp(progressBoard.GetCompletedSteps(objectiveId), 0, 10);
        if (step < lastStep) remoteCue = false;
        float progress = step / 10f;
        if (outcomeText != null)
            outcomeText.text = activity.ToString().ToUpperInvariant() + "  " + step + "/10\n" + Phase(step);
        if (milestones != null)
        {
            for (int i = 0; i < milestones.Length; i++)
            {
                var renderer = milestones[i];
                if (renderer == null) continue;
                var color = i < step ? new Color(.23f, .93f, .58f) : new Color(.32f, .36f, .42f);
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
                block.Clear();
            }
        }
        if (revealPieces != null)
            for (int i = 0; i < revealPieces.Length; i++)
                if (revealPieces[i] != null) revealPieces[i].SetActive(i < step);

        if (focus != null)
        {
            focus.localPosition = focusPosition;
            focus.localScale = focusScale;
            focus.localRotation = focusRotation;
        }
        if (secondary != null)
        {
            secondary.localPosition = secondaryPosition;
            secondary.localScale = secondaryScale;
            secondary.localRotation = secondaryRotation;
        }
        ApplyActivity(progress, step);
        if (remoteCue) ApplyRemoteCue();
        if (activity == Activity.Music && step > lastStep && lastStep >= 0)
            PlayNote(step);
        lastStep = step;
    }

    void ApplyRemoteCue()
    {
        if (focus == null || secondary == null) return;
        switch (activity)
        {
            case Activity.Market: focus.localPosition += Vector3.right * .22f; break;
            case Activity.Cafe: secondary.localRotation *= Quaternion.Euler(0, 0, -28); break;
            case Activity.Workshop: focus.localRotation *= Quaternion.Euler(0, 0, 100); break;
            case Activity.Greenhouse: secondary.localRotation *= Quaternion.Euler(0, 75, 0); break;
            case Activity.Pool: secondary.localRotation *= Quaternion.Euler(0, 0, 70); break;
            case Activity.Bakery: secondary.localRotation *= Quaternion.Euler(0, -55, 0); break;
            case Activity.Post: secondary.localPosition += Vector3.down * .24f; break;
            case Activity.Clinic: secondary.localRotation *= Quaternion.Euler(0, 0, -40); break;
            case Activity.Library: secondary.localRotation *= Quaternion.Euler(0, 0, -28); break;
            case Activity.Music: secondary.localRotation *= Quaternion.Euler(0, 0, -45); break;
            case Activity.Garage: focus.localPosition += Vector3.up * .26f; break;
            case Activity.Observatory: focus.localRotation *= Quaternion.Euler(-22, 45, 0); break;
            case Activity.Harbor: focus.localRotation *= Quaternion.Euler(0, -35, 0); break;
            case Activity.Art: focus.localRotation *= Quaternion.Euler(0, 65, 0); break;
            case Activity.Recycling: secondary.localPosition += Vector3.down * .26f; break;
        }
    }

    void ApplyActivity(float progress, int step)
    {
        if (focus == null || secondary == null) return;
        switch (activity)
        {
            case Activity.Market:
                focus.localPosition += Vector3.right * (.48f * progress); // cash drawer
                secondary.localScale = secondaryScale * (1f + .48f * progress); // filled basket
                break;
            case Activity.Cafe:
                focus.localPosition += Vector3.up * (.30f * progress); // coffee level
                secondary.localRotation *= Quaternion.Euler(0, 0, -42f * progress); // steam wand
                break;
            case Activity.Workshop:
                focus.localRotation *= Quaternion.Euler(0, 0, 360f * progress); // gear train
                secondary.localPosition += Vector3.up * (.40f * progress); // power column
                break;
            case Activity.Greenhouse:
                focus.localScale = new Vector3(focusScale.x * (1f + progress),
                    focusScale.y * (1f + 2.5f * progress), focusScale.z * (1f + progress));
                secondary.localRotation *= Quaternion.Euler(0, 0, -55f * progress); // sprinkler
                break;
            case Activity.Pool:
                focus.localPosition += Vector3.up * (.22f * progress); // floating marker
                secondary.localRotation *= Quaternion.Euler(0, 0, 180f * progress); // timer needle
                break;
            case Activity.Bakery:
                focus.localScale = focusScale * (1f + .65f * progress); // dough rising
                secondary.localRotation *= Quaternion.Euler(0, -75f * Mathf.Clamp01((step - 5f) / 2f), 0); // oven
                break;
            case Activity.Post:
                focus.localPosition += Vector3.right * (1.05f * progress); // sorting belt parcel
                secondary.localRotation *= Quaternion.Euler(-65f * Mathf.Clamp01((step - 2f) / 2f), 0, 0); // stamp
                break;
            case Activity.Clinic:
                focus.localScale = new Vector3(focusScale.x * (1f + .6f * progress), focusScale.y, focusScale.z);
                secondary.localRotation *= Quaternion.Euler(0, 0, -70f * Mathf.Clamp01((step - 5f) / 2f)); // scanner
                break;
            case Activity.Library:
                focus.localScale = new Vector3(focusScale.x, focusScale.y * (1f + 1.8f * progress), focusScale.z); // books
                secondary.localRotation *= Quaternion.Euler(0, 0, -38f * progress); // open page
                break;
            case Activity.Music:
                focus.localScale = new Vector3(focusScale.x, focusScale.y * (1f + progress), focusScale.z); // equalizer
                secondary.localRotation *= Quaternion.Euler(0, 0, -40f * progress); // spotlight
                break;
            case Activity.Garage:
                focus.localPosition += Vector3.up * (.95f * progress); // vehicle lift
                secondary.localRotation *= Quaternion.Euler(0, 0, 270f * progress); // wheel
                break;
            case Activity.Observatory:
                focus.localRotation *= Quaternion.Euler(-35f * progress, 120f * progress, 0); // telescope
                secondary.localRotation *= Quaternion.Euler(0, 0, 220f * progress); // star dial
                break;
            case Activity.Harbor:
                focus.localRotation *= Quaternion.Euler(0, -75f * progress, 0); // crane boom
                secondary.localPosition += Vector3.up * (.9f * progress); // cargo hook
                break;
            case Activity.Art:
                focus.localRotation *= Quaternion.Euler(0, 280f * progress, 0); // sculpture turntable
                secondary.localScale = secondaryScale * (1f + .15f * progress); // exhibition light
                break;
            case Activity.Recycling:
                focus.localPosition += Vector3.right * (.75f * progress); // conveyor
                secondary.localPosition += Vector3.down * (.65f * Mathf.Clamp01((step - 7f) / 2f)); // compactor
                break;
        }
    }

    void Update()
    {
        if (lastStep < 0) return;
        if (activity == Activity.Clinic && focus != null && lastStep >= 3)
        {
            clock += Time.deltaTime;
            focus.localScale = new Vector3(focusScale.x * (1f + .6f * lastStep / 10f),
                focusScale.y * (1f + .25f * Mathf.Sin(clock * 8f)), focusScale.z);
        }
        else if (activity == Activity.Music && focus != null && lastStep >= 3)
        {
            clock += Time.deltaTime;
            focus.localScale = new Vector3(focusScale.x,
                focusScale.y * (1f + lastStep / 10f + .12f * Mathf.Sin(clock * 5f)), focusScale.z);
        }
    }

    void PlayNote(int step)
    {
        if (musicSource == null) return;
        int sampleRate = 22050;
        int count = sampleRate / 5;
        var samples = new float[count];
        var notes = new[] { 262f, 294f, 330f, 349f, 392f, 440f, 494f, 523f, 587f, 659f };
        float frequency = notes[Mathf.Clamp(step - 1, 0, notes.Length - 1)];
        for (int i = 0; i < count; i++)
        {
            float envelope = Mathf.Sin(Mathf.PI * i / count);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * .16f;
        }
        var clip = AudioClip.Create("Village note " + step, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        musicSource.PlayOneShot(clip);
        Destroy(clip, .4f);
    }

    string Phase(int step)
    {
        if (step >= 10) return "COMPLETE - VENUE TRANSFORMED";
        switch (activity)
        {
            case Activity.Market: return step < 3 ? "SHOPPING" : step < 7 ? "STOCK AND PRICE" : "PAYMENT";
            case Activity.Cafe: return step < 3 ? "BREWING" : step < 7 ? "PREPARE THE ORDER" : "SERVING";
            case Activity.Workshop: return step < 3 ? "POWER CIRCUITS" : step < 7 ? "REPAIR MACHINE" : "INSPECTION";
            case Activity.Greenhouse: return step < 3 ? "SEEDS AND SOIL" : step < 7 ? "GROW AND WATER" : "HARVEST";
            case Activity.Pool: return step < 3 ? "RESCUE TRAINING" : step < 7 ? "POOL SETUP" : "RACE FINISH";
            case Activity.Bakery: return step < 3 ? "MIX THE DOUGH" : step < 7 ? "PROOF AND BAKE" : "COOL AND PACK";
            case Activity.Post: return step < 3 ? "RECEIVE MAIL" : step < 7 ? "SORT AND STAMP" : "DISPATCH";
            case Activity.Clinic: return step < 3 ? "CHECK IN" : step < 7 ? "DIAGNOSE AND TREAT" : "DISCHARGE";
            case Activity.Library: return step < 3 ? "RETURNS" : step < 7 ? "CATALOG AND SHELVE" : "CHECKOUT";
            case Activity.Music: return step < 3 ? "SET THE STAGE" : step < 7 ? "SOUND CHECK" : "SHOWTIME";
            case Activity.Garage: return step < 3 ? "INSPECT VEHICLE" : step < 7 ? "REPAIR" : "TEST AND RELEASE";
            case Activity.Observatory: return step < 3 ? "ASSEMBLE SCOPE" : step < 7 ? "ALIGN STARS" : "CAPTURE SKY";
            case Activity.Harbor: return step < 3 ? "SECURE THE DOCK" : step < 7 ? "LOAD CARGO" : "SAIL";
            case Activity.Art: return step < 3 ? "PREPARE CANVAS" : step < 7 ? "CREATE" : "EXHIBIT";
            case Activity.Recycling: return step < 3 ? "SORT" : step < 7 ? "WASH AND PROCESS" : "COMPACT";
            default: return "EXPLORE";
        }
    }
}
