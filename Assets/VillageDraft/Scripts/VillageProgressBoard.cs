using System;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Tracks village objectives and keeps the central board readable as the village grows.</summary>
public sealed class VillageProgressBoard : MonoBehaviour
{
    [Serializable]
    public sealed class Objective
    {
        public string id;
        public string label;
        [Min(1)] public int stepsRequired = 1;
        [NonSerialized] public int stepsCompleted;
        public Renderer indicator;
    }

    [Header("Village tasks")]
    public Objective[] objectives = Array.Empty<Objective>();
    public TextMesh boardText;
    public string heading = "VILLAGE TASKS";

    [Header("Status lamps")]
    public Color pendingColor = new Color(0.95f, 0.57f, 0.18f);
    public Color completeColor = new Color(0.28f, 0.9f, 0.56f);

    [Header("Events")]
    public UnityEvent onProgressChanged;
    public UnityEvent onVillageComplete;

    MaterialPropertyBlock propertyBlock;
    bool allCompleteInvoked;

    public int CompletedObjectiveCount
    {
        get
        {
            var count = 0;
            foreach (var objective in objectives)
                if (objective != null && objective.stepsCompleted >= Mathf.Max(1, objective.stepsRequired))
                    count++;
            return count;
        }
    }

    public bool IsVillageComplete => objectives.Length > 0 && CompletedObjectiveCount == objectives.Length;

    public int CompletedStepCount
    {
        get
        {
            var count = 0;
            foreach (var objective in objectives)
                if (objective != null)
                    count += Mathf.Clamp(objective.stepsCompleted, 0, Mathf.Max(1, objective.stepsRequired));
            return count;
        }
    }

    public int RequiredStepCount
    {
        get
        {
            var count = 0;
            foreach (var objective in objectives)
                if (objective != null)
                    count += Mathf.Max(1, objective.stepsRequired);
            return count;
        }
    }

    void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        RefreshBoard();
    }

    /// <summary>
    /// Advance one step. A requiredCurrentStep of -1 accepts any unfinished step;
    /// 0, 1, etc. enforce an order within an objective.
    /// </summary>
    public bool TryAdvanceObjective(string objectiveId, int requiredCurrentStep = -1)
    {
        if (string.IsNullOrWhiteSpace(objectiveId))
            return false;

        foreach (var objective in objectives)
        {
            if (objective == null || objective.id != objectiveId)
                continue;

            if (objective.stepsCompleted >= Mathf.Max(1, objective.stepsRequired) ||
                (requiredCurrentStep >= 0 && objective.stepsCompleted != requiredCurrentStep))
                return false;

            objective.stepsCompleted++;
            RefreshBoard();
            onProgressChanged?.Invoke();

            if (IsVillageComplete && !allCompleteInvoked)
            {
                allCompleteInvoked = true;
                onVillageComplete?.Invoke();
            }
            return true;
        }

        Debug.LogWarning($"Village objective '{objectiveId}' is missing from the progress board.", this);
        return false;
    }

    public int GetCompletedSteps(string objectiveId)
    {
        foreach (var objective in objectives)
            if (objective != null && objective.id == objectiveId)
                return objective.stepsCompleted;
        return -1;
    }

    public void ResetProgress()
    {
        foreach (var objective in objectives)
            if (objective != null)
                objective.stepsCompleted = 0;
        allCompleteInvoked = false;
        RefreshBoard();
        onProgressChanged?.Invoke();
    }

    public void RefreshBoard()
    {
        if (boardText != null)
        {
            var lines = new StringBuilder(128);
            lines.AppendLine(heading);
            lines.AppendLine();
            if (objectives.Length > 5)
            {
                lines.AppendLine($"{CompletedObjectiveCount} OF {objectives.Length} PLACES READY");
                lines.AppendLine();
                if (IsVillageComplete)
                {
                    lines.AppendLine("THE FESTIVAL IS READY!");
                    lines.Append("CELEBRATE AT THE SQUARE");
                }
                else
                {
                    foreach (var objective in objectives)
                    {
                        if (objective == null || objective.stepsCompleted >= Mathf.Max(1, objective.stepsRequired))
                            continue;
                        lines.AppendLine("SUGGESTED: " + objective.id.ToUpperInvariant());
                        lines.Append(objective.id == "market"
                            ? "FOLLOW CORAL TRAIL"
                            : "OR VISIT ANY PLACE");
                        break;
                    }
                }
            }
            else
            {
                foreach (var objective in objectives)
                {
                    if (objective == null)
                        continue;
                    var required = Mathf.Max(1, objective.stepsRequired);
                    var completed = Mathf.Clamp(objective.stepsCompleted, 0, required);
                    lines.Append(completed >= required ? "[DONE] " : $"[{completed}/{required}] ");
                    lines.AppendLine(string.IsNullOrEmpty(objective.label) ? objective.id : objective.label);
                }
                lines.AppendLine();
                lines.Append($"{CompletedObjectiveCount}/{objectives.Length} places complete");
            }
            boardText.text = lines.ToString();
        }

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
        foreach (var objective in objectives)
        {
            if (objective?.indicator == null)
                continue;
            var material = objective.indicator.sharedMaterial;
            if (material == null)
                continue;
            var color = objective.stepsCompleted >= Mathf.Max(1, objective.stepsRequired) ? completeColor : pendingColor;
            objective.indicator.GetPropertyBlock(propertyBlock);
            if (material.HasProperty("_BaseColor"))
                propertyBlock.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                propertyBlock.SetColor("_Color", color);
            objective.indicator.SetPropertyBlock(propertyBlock);
            propertyBlock.Clear();
        }
    }
}
