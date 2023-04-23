#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class MyPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gameManager = (GameManager)target;

        if (GUILayout.Button("Generate solar system"))
        {
            gameManager.GenerateSolarSystem();
        }

        if (GUILayout.Button("Generate rand solar system"))
        {
            gameManager.GenerateRandSolarSystem();
        }
    }
}
#endif