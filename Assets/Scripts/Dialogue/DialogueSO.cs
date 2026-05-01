using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Zero-Te/Dialogue")]
public class DialogueSO : ScriptableObject
{
    public List<DialogueLine> lines = new();
}