namespace SWeetJelly.Bootstrapper.Framework.Unity.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(PackageBootstrapperProfileSo))]
    public sealed class PackageBootstrapperProfileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}