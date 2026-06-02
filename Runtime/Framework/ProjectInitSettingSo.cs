namespace SWeetJelly.Initialiser.Framework
{
    using UnityEngine;
    using System.Collections.Generic;

    [CreateAssetMenu(
        fileName = "Project Init Setting",
        menuName = "SWeetJelly/Settings/Project Init Setting")]
    public class ProjectInitSettingSo : ScriptableObject
    {
        [SerializeField] InitModuleSo[] modules;
        public IReadOnlyList<InitModuleSo> Modules => modules;
    }
}