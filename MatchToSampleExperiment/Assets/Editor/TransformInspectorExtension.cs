#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Transform))]
public class TransformInspectorExtension : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector for the Transform component.

        if (GUILayout.Button("Save Transform"))
        {
            SaveTransform();
        }

        if (GUILayout.Button("Load Transform"))
        {
            LoadTransform();
        }
    }

    void SaveTransform()
    {
        Transform t = (Transform)target;
        PlayerPrefs.SetFloat("SavedPositionX", t.position.x);
        PlayerPrefs.SetFloat("SavedPositionY", t.position.y);
        PlayerPrefs.SetFloat("SavedPositionZ", t.position.z);
        PlayerPrefs.SetFloat("SavedRotationX", t.rotation.eulerAngles.x);
        PlayerPrefs.SetFloat("SavedRotationY", t.rotation.eulerAngles.y);
        PlayerPrefs.SetFloat("SavedRotationZ", t.rotation.eulerAngles.z);
        PlayerPrefs.SetFloat("SavedScaleX", t.localScale.x);
        PlayerPrefs.SetFloat("SavedScaleY", t.localScale.y);
        PlayerPrefs.SetFloat("SavedScaleZ", t.localScale.z);
    }

    void LoadTransform()
    {
        if (!PlayerPrefs.HasKey("SavedPositionX") || !PlayerPrefs.HasKey("SavedPositionY") || !PlayerPrefs.HasKey("SavedPositionZ")
        || !PlayerPrefs.HasKey("SavedRotationX") || !PlayerPrefs.HasKey("SavedRotationY") || !PlayerPrefs.HasKey("SavedRotationZ")
        || !PlayerPrefs.HasKey("SavedScaleX") || !PlayerPrefs.HasKey("SavedScaleY") || !PlayerPrefs.HasKey("SavedScaleZ"))
        {
            return; // Don't try to load if no saved data.
        }

        Transform t = (Transform)target;
        Vector3 savedPosition = new Vector3(
            PlayerPrefs.GetFloat("SavedPositionX"),
            PlayerPrefs.GetFloat("SavedPositionY"),
            PlayerPrefs.GetFloat("SavedPositionZ")
        );
        Vector3 savedRotation = new Vector3(
            PlayerPrefs.GetFloat("SavedRotationX"),
            PlayerPrefs.GetFloat("SavedRotationY"),
            PlayerPrefs.GetFloat("SavedRotationZ")
        );
        Vector3 savedScale = new Vector3(
            PlayerPrefs.GetFloat("SavedScaleX"),
            PlayerPrefs.GetFloat("SavedScaleY"),
            PlayerPrefs.GetFloat("SavedScaleZ")
        );

        t.position = savedPosition;
        t.rotation = Quaternion.Euler(savedRotation);
        t.localScale = savedScale;
    }
}
#endif
