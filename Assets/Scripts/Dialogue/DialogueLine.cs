using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName = "";
    public Sprite portrait;
    [TextArea(2, 5)]
    public string text = "";
    public float typingSpeed = 0.04f;
    public bool autoAdvance = false;
    public float autoAdvanceDelay = 1f;
}