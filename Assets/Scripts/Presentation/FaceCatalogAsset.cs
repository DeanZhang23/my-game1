using System;
using UnityEngine;

namespace Echoes.Presentation
{
    [Serializable]
    public sealed class FaceEntry
    {
        public string id;
        public string nameKey;
        public string descriptionKey;
    }

    [CreateAssetMenu(menuName = "Echoes/Face Catalog")]
    public sealed class FaceCatalogAsset : ScriptableObject
    {
        public FaceEntry[] faces;
    }
}
